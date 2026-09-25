import logging
import os
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

import numpy as np
import torch

logger = logging.getLogger("OptiVisionWorker.SlidingWindow")
_DEFAULT_WEIGHTS_DIR = Path(__file__).resolve().parent.parent / "training" / "weights"
if not _DEFAULT_WEIGHTS_DIR.exists() and Path("/app/training/weights").exists():
    _DEFAULT_WEIGHTS_DIR = Path("/app/training/weights")

WEIGHTS_DIR = Path(os.getenv("WEIGHTS_DIR", str(_DEFAULT_WEIGHTS_DIR)))
WINDOW_SIZE = 90
STRIDE = 30
THRESHOLD = 0.80
FLAW_THRESHOLDS: Dict[str, float] = {
    "heels_raised": 0.92,
}

TRAJECTORY_FLAWS = {"shallow_depth", "no_chest_touch"}
IDLE_DISPLACEMENT_THRESHOLD = 0.08

LABELS: Dict[str, List[str]] = {
    "squat": ["shallow_depth", "excessive_forward_lean", "heels_raised"],
    "bench_press": ["glutes_raised", "excessive_elbow_flare", "no_chest_touch", "incorrect_bar_path", "bad_arch"],
    "deadlift": ["lumbar_flexion", "hips_early_rise", "bar_drifting", "knees_forward", "shallow_depth"],
}

EXERCISE_ALIASES: Dict[str, str] = {
    "bench": "bench_press",
    "benchpress": "bench_press",
    "bench_press": "bench_press",
    "squat": "squat",
    "squats": "squat",
    "deadlift": "deadlift",
    "deadlifts": "deadlift",
}

DEVICE: torch.device = torch.device("cpu")
LOADED_MODELS: Dict[str, Any] = {}


def set_device(device: torch.device) -> None:
    global DEVICE
    DEVICE = device


def get_device() -> torch.device:
    return DEVICE


def get_model(
    exercise: str,
    device: Optional[torch.device] = None,
    weights_dir: Optional[Path] = None,
) -> Tuple[str, Any]:
    target_device = device or DEVICE
    target_weights_dir = weights_dir or WEIGHTS_DIR

    normalised = EXERCISE_ALIASES.get(exercise.lower(), exercise.lower())
    cache_key = f"{normalised}_{target_device}"
    if cache_key in LOADED_MODELS:
        return normalised, LOADED_MODELS[cache_key]

    model_file = target_weights_dir / f"{normalised}_side.pt"
    if not model_file.exists():
        raise FileNotFoundError(f"Model weights not found for '{normalised}' at {model_file}")

    logger.info("Loading TorchScript model: %s onto %s.", model_file.name, target_device)
    model = torch.jit.load(str(model_file), map_location=target_device)
    model.eval()
    LOADED_MODELS[cache_key] = model
    return normalised, model


def parse_frames_to_tensor(
    coordinates_data: Dict[str, Any],
    device: Optional[torch.device] = None,
) -> torch.Tensor:
    target_device = device or DEVICE
    frames = coordinates_data.get("frames", [])
    if not frames:
        raise ValueError("No frames found in coordinate payload.")

    frame_matrix = []
    for f in frames:
        raw_lm = f.get("landmarks", [])
        frame_lms = []
        for lm in raw_lm:
            if isinstance(lm, dict):
                frame_lms.append([lm.get("x", 0.0), lm.get("y", 0.0), lm.get("z", 0.0)])
            elif isinstance(lm, (list, tuple)) and len(lm) >= 3:
                frame_lms.append([lm[0], lm[1], lm[2]])
            else:
                frame_lms.append([0.0, 0.0, 0.0])
        while len(frame_lms) < 33:
            frame_lms.append([0.0, 0.0, 0.0])
        frame_matrix.append(frame_lms[:33])

    tensor_np = np.array(frame_matrix, dtype=np.float32)
    flattened = tensor_np.reshape(len(frames), -1)
    transposed = flattened.T
    batch_tensor = torch.from_numpy(transposed).unsqueeze(0).to(target_device)
    return batch_tensor


def is_window_idle(exercise: str, window_tensor: torch.Tensor) -> bool:
    if exercise in ("squat", "deadlift"):
        left_hip_y = window_tensor[0, 70, :]
        right_hip_y = window_tensor[0, 73, :]
        disp_left = float(left_hip_y.max() - left_hip_y.min())
        disp_right = float(right_hip_y.max() - right_hip_y.min())
        return max(disp_left, disp_right) < IDLE_DISPLACEMENT_THRESHOLD

    if exercise in ("bench", "bench_press"):
        left_wrist_y = window_tensor[0, 46, :]
        right_wrist_y = window_tensor[0, 49, :]
        disp_left = float(left_wrist_y.max() - left_wrist_y.min())
        disp_right = float(right_wrist_y.max() - right_wrist_y.min())
        return max(disp_left, disp_right) < IDLE_DISPLACEMENT_THRESHOLD

    return False


def _pad_input_tensor(model_input: torch.Tensor, window_size: int) -> torch.Tensor:
    total_frames = model_input.shape[2]
    if total_frames >= window_size:
        return model_input
    pad_size = window_size - total_frames
    last_frame = model_input[:, :, -1:].repeat(1, 1, pad_size)
    return torch.cat([model_input, last_frame], dim=2)


def _evaluate_windows(
    model: Any,
    model_input: torch.Tensor,
    exercise: str,
    class_labels: List[str],
) -> List[Dict[str, Any]]:
    total_frames = model_input.shape[2]
    window_records: List[Dict[str, Any]] = []

    with torch.no_grad():
        for start_idx in range(0, total_frames - WINDOW_SIZE + 1, STRIDE):
            end_idx = start_idx + WINDOW_SIZE
            window_tensor = model_input[:, :, start_idx:end_idx]
            idle = is_window_idle(exercise, window_tensor)

            if not idle:
                outputs = model(window_tensor)
                probs = torch.sigmoid(outputs)[0].cpu().numpy()
            else:
                probs = np.zeros(len(class_labels), dtype=np.float32)

            window_records.append({"is_idle": idle, "probs": probs})

    return window_records


def _aggregate_peak_scores(
    class_labels: List[str],
    window_records: List[Dict[str, Any]],
) -> Dict[str, float]:
    active_windows = [w for w in window_records if not w["is_idle"]]
    target_windows = active_windows if active_windows else window_records
    peak_scores = dict.fromkeys(class_labels, 0.0)

    for w in target_windows:
        for i, prob in enumerate(w["probs"]):
            flaw = class_labels[i]
            if float(prob) > peak_scores[flaw]:
                peak_scores[flaw] = float(prob)

    return peak_scores


def _find_suppressed_flaws(
    exercise: str,
    class_labels: List[str],
    window_records: List[Dict[str, Any]],
    peak_scores: Dict[str, float],
) -> set:
    suppressed: set = set()
    active_windows = [w for w in window_records if not w["is_idle"]]

    for flaw in TRAJECTORY_FLAWS:
        if flaw in class_labels:
            flaw_idx = class_labels.index(flaw)
            threshold = FLAW_THRESHOLDS.get(flaw, THRESHOLD)
            if any(w["probs"][flaw_idx] < threshold for w in active_windows):
                suppressed.add(flaw)

    if exercise == "deadlift":
        hips_threshold = FLAW_THRESHOLDS.get("hips_early_rise", THRESHOLD)
        if peak_scores.get("hips_early_rise", 0.0) >= hips_threshold:
            suppressed.add("lumbar_flexion")

    return suppressed


def _filter_detected_anomalies(
    peak_scores: Dict[str, float],
    suppressed_flaws: set,
) -> List[Dict[str, Any]]:
    detected = []
    for flaw, score in peak_scores.items():
        threshold = FLAW_THRESHOLDS.get(flaw, THRESHOLD)
        if score >= threshold and flaw not in suppressed_flaws:
            detected.append({"error": flaw, "severity": round(score, 3)})

    detected.sort(key=lambda x: x["severity"], reverse=True)
    return detected


def run_sliding_window_inference(
    exercise: str,
    coordinates_data: Dict[str, Any],
    device: Optional[torch.device] = None,
    weights_dir: Optional[Path] = None,
) -> List[Dict[str, Any]]:
    target_device = device or DEVICE
    normalised_exercise, model = get_model(exercise, device=target_device, weights_dir=weights_dir)
    class_labels = LABELS.get(normalised_exercise, LABELS["squat"])

    model_input = parse_frames_to_tensor(coordinates_data, device=target_device)
    model_input = _pad_input_tensor(model_input, WINDOW_SIZE)
    logger.info("Analysing %d frames for '%s'.", model_input.shape[2], normalised_exercise)

    window_records = _evaluate_windows(model, model_input, normalised_exercise, class_labels)
    peak_scores = _aggregate_peak_scores(class_labels, window_records)
    suppressed_flaws = _find_suppressed_flaws(normalised_exercise, class_labels, window_records, peak_scores)
    detected_anomalies = _filter_detected_anomalies(peak_scores, suppressed_flaws)

    logger.info(
        "Inference complete: %d anomalies detected over threshold (%s).",
        len(detected_anomalies),
        detected_anomalies,
    )
    return detected_anomalies