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
CLEARANCE_THRESHOLD = 0.45
LOCAL_SMOOTHING_RADIUS = 60  # 2.0 seconds at 30fps
FLAW_THRESHOLDS: Dict[str, float] = {}

TRAJECTORY_FLAWS = {"shallow_depth", "no_chest_touch"}
IDLE_DISPLACEMENT_THRESHOLD = 0.08

LABELS: Dict[str, List[str]] = {
    "squat": ["shallow_depth", "excessive_forward_lean"],
    "bench_press": [
        "excessive_elbow_flare",
        "no_chest_touch",
        "incorrect_bar_path",
        "bad_arch",
    ],
    "deadlift": ["lumbar_flexion", "bad_hip_movement", "bar_drifting", "knees_forward"],
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
        raise FileNotFoundError(
            f"Model weights not found for '{normalised}' at {model_file}"
        )

    logger.info(
        "Loading TorchScript model: %s onto %s.", model_file.name, target_device
    )
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


def _check_bench_press_posture(med_angle: float) -> Optional[str]:
    # check angle less than 35 degrees
    if med_angle > 35.0:
        return f"Expected Bench Press (horizontal posture), but lifter is upright (torso angle {med_angle:.1f}°)."
    return None


def _check_squat_posture(
    med_angle: float, has_wrists: bool, mean_wrist_to_sh: float, max_wrist_to_hip: float
) -> Optional[str]:
    # upright
    if med_angle < 15.0:
        return f"Expected Squat (standing posture), but lifter is lying flat on a bench (median torso angle {med_angle:.1f}°)."

    # 2. hands above the shoulders
    if has_wrists and mean_wrist_to_sh > 0.14 and max_wrist_to_hip > 0.08:
        return "Expected Squat (hands held at shoulders on barbell), but lifter's hands are hanging down below hips (Deadlift detected)."

    return None


def _check_deadlift_posture(
    med_angle: float,
    hip_disp: float,
    has_wrists: bool,
    mean_wrist_to_sh: float,
    max_wrist_to_hip: float,
) -> Optional[str]:
    # 1. lots of hip movement
    if med_angle < 12.0 and hip_disp < 0.05:
        return f"Expected Deadlift (standing posture), but lifter is lying flat on a bench (median torso angle {med_angle:.1f}°)."

    # hands below shoulders
    if has_wrists and (mean_wrist_to_sh < 0.12 or max_wrist_to_hip < 0.05):
        return "Expected Deadlift (hands pulling from floor), but lifter's hands remain held at shoulder level (Squat detected)."

    return None


def detect_posture_mismatch(
    exercise: str, model_input: torch.Tensor
) -> Optional[Dict[str, Any]]:
    total_frames = model_input.shape[2]
    if total_frames < 5:
        return None

    # shoulders: L= 11, R= 12 (channels 33, 34, 36 and 37)
    # Hips: L= 23, R= 24 (channels 69, 70, 72 and 73)
    sh_x = (model_input[0, 33, :] + model_input[0, 36, :]) / 2.0
    sh_y = (model_input[0, 34, :] + model_input[0, 37, :]) / 2.0
    hip_x = (model_input[0, 69, :] + model_input[0, 72, :]) / 2.0
    hip_y = (model_input[0, 70, :] + model_input[0, 73, :]) / 2.0
    wrist_y = (model_input[0, 46, :] + model_input[0, 49, :]) / 2.0

    valid_mask = (sh_x != 0) | (sh_y != 0)
    if valid_mask.sum() < 5:
        return None

    dx = torch.abs(sh_x[valid_mask] - hip_x[valid_mask])
    dy = torch.abs(sh_y[valid_mask] - hip_y[valid_mask])

    angles = torch.rad2deg(torch.atan2(dy, dx + 1e-6))
    med_angle = float(torch.median(angles))
    hip_disp = float(torch.max(hip_y[valid_mask]) - torch.min(hip_y[valid_mask]))

    valid_wrists = valid_mask & (wrist_y != 0)
    has_wrists = bool(valid_wrists.sum() >= 5)
    mean_wrist_to_sh = (
        float(torch.mean(wrist_y[valid_wrists] - sh_y[valid_wrists]))
        if has_wrists
        else 0.0
    )
    max_wrist_to_hip = (
        float(torch.max(wrist_y[valid_wrists] - hip_y[valid_wrists]))
        if has_wrists
        else 0.0
    )

    norm_exercise = EXERCISE_ALIASES.get(exercise.lower(), exercise.lower())

    msg = None
    if norm_exercise == "bench_press":
        msg = _check_bench_press_posture(med_angle)
    elif norm_exercise == "squat":
        msg = _check_squat_posture(
            med_angle, has_wrists, mean_wrist_to_sh, max_wrist_to_hip
        )
    elif norm_exercise == "deadlift":
        msg = _check_deadlift_posture(
            med_angle, hip_disp, has_wrists, mean_wrist_to_sh, max_wrist_to_hip
        )

    if msg:
        logger.warning("Posture mismatch for %s: %s", norm_exercise, msg)
        return {
            "error": "posture_mismatch",
            "severity": 1.0,
            "start_frame": 0,
            "end_frame": total_frames,
            "message": msg,
        }

    return None


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

            window_records.append(
                {
                    "is_idle": idle,
                    "probs": probs,
                    "start_frame": start_idx,
                    "end_frame": end_idx,
                }
            )

    return window_records


def _is_window_locally_smoothed(
    w: Dict[str, Any], flaw_idx: int, windows: List[Dict[str, Any]]
) -> bool:
    w_start = w.get("start_frame", 0)
    for nbr in windows:
        if abs(nbr.get("start_frame", 0) - w_start) <= LOCAL_SMOOTHING_RADIUS:
            if float(nbr["probs"][flaw_idx]) < CLEARANCE_THRESHOLD:
                return True
    return False


def _evaluate_trajectory_flaw(
    flaw: str, flaw_idx: int, threshold: float, target_windows: List[Dict[str, Any]]
) -> Optional[Dict[str, Any]]:
    # new local temporal smoothing so we can check multiple reps
    unsmoothed_windows = []
    for w in target_windows:
        prob = float(w["probs"][flaw_idx])
        if prob >= threshold and not _is_window_locally_smoothed(
            w, flaw_idx, target_windows
        ):
            unsmoothed_windows.append((prob, w))

    if not unsmoothed_windows:
        return None

    best_prob, best_w = max(unsmoothed_windows, key=lambda item: item[0])
    return {
        "error": flaw,
        "severity": round(best_prob, 3),
        "start_frame": best_w.get("start_frame", 0),
        "end_frame": best_w.get("end_frame", 0),
    }


def _evaluate_standard_flaw(
    flaw: str, flaw_idx: int, threshold: float, target_windows: List[Dict[str, Any]]
) -> Optional[Dict[str, Any]]:
    # peak evaluation for non-trajectory flaws
    peak_prob = 0.0
    peak_window = None
    for w in target_windows:
        prob = float(w["probs"][flaw_idx])
        if prob > peak_prob:
            peak_prob = prob
            peak_window = w

    if peak_prob >= threshold and peak_window is not None:
        return {
            "error": flaw,
            "severity": round(peak_prob, 3),
            "start_frame": peak_window.get("start_frame", 0),
            "end_frame": peak_window.get("end_frame", 0),
        }
    return None


def _filter_detected_anomalies(
    class_labels: List[str],
    window_records: List[Dict[str, Any]],
) -> List[Dict[str, Any]]:
    active_windows = [w for w in window_records if not w["is_idle"]]
    target_windows = active_windows if active_windows else window_records
    detected = []

    for flaw_idx, flaw in enumerate(class_labels):
        threshold = FLAW_THRESHOLDS.get(flaw, THRESHOLD)

        if flaw in TRAJECTORY_FLAWS:
            anomaly = _evaluate_trajectory_flaw(
                flaw, flaw_idx, threshold, target_windows
            )
        else:
            anomaly = _evaluate_standard_flaw(flaw, flaw_idx, threshold, target_windows)

        if anomaly:
            detected.append(anomaly)

    detected.sort(key=lambda x: x["severity"], reverse=True)
    return detected


def run_sliding_window_inference(
    exercise: str,
    coordinates_data: Dict[str, Any],
    device: Optional[torch.device] = None,
    weights_dir: Optional[Path] = None,
) -> List[Dict[str, Any]]:
    target_device = device or DEVICE
    normalised_exercise, model = get_model(
        exercise, device=target_device, weights_dir=weights_dir
    )
    class_labels = LABELS.get(normalised_exercise, LABELS["squat"])

    model_input = parse_frames_to_tensor(coordinates_data, device=target_device)
    model_input = _pad_input_tensor(model_input, WINDOW_SIZE)
    logger.info(
        "Analysing %d frames for '%s'.", model_input.shape[2], normalised_exercise
    )

    mismatch = detect_posture_mismatch(normalised_exercise, model_input)
    if mismatch:
        logger.info(
            "Inference intercepted by posture gatekeeper: %s", mismatch["error"]
        )
        return [mismatch]

    window_records = _evaluate_windows(
        model, model_input, normalised_exercise, class_labels
    )
    detected_anomalies = _filter_detected_anomalies(class_labels, window_records)

    logger.info(
        "Inference complete: %d anomalies detected over threshold (%s).",
        len(detected_anomalies),
        detected_anomalies,
    )
    return detected_anomalies
