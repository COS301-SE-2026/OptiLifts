from pathlib import Path
import numpy as np
import shutil

EXERCISES = {
    "squat": {
        "classes": 2,
        "lacking_indices": [],  # Heels raised was dropped
    },
    "bench_press": {
        "classes": 5,
        "lacking_indices": [0],  # glutes_raised
    },
    "deadlift": {
        "classes": 5,
        "lacking_indices": [1],  # hips_early_rise
    },
}


def apply_noise(tensor, noise_level=0.01):
    noisy_tensor = tensor.copy()
    valid_mask = np.any(tensor != 0, axis=-1)

    # small random noise
    noise = np.random.normal(0, noise_level, noisy_tensor[valid_mask].shape)  # NOSONAR
    noisy_tensor[valid_mask] += noise
    return noisy_tensor


def apply_scale(tensor, scale_min=0.9, scale_max=1.1):
    scaled_tensor = tensor.copy()
    valid_mask = np.any(tensor != 0, axis=-1)

    # random scale factor for height
    scale_factor = np.random.uniform(scale_min, scale_max)  # NOSONAR
    scaled_tensor[valid_mask] *= scale_factor
    return scaled_tensor


def is_lacking(t_file_name, num_classes, lacking_indices):
    # get labels to see if it's a lacking area
    name = t_file_name.replace(".npy", "")
    parts = name.split("_")
    labels = [float(x) for x in parts[-num_classes:]]
    return any(labels[i] > 0 for i in lacking_indices)


def process_tensor_file(t_file, augmented_dir, num_classes, lacking_indices):
    tensor = np.load(t_file)

    lacking = is_lacking(t_file.name, num_classes, lacking_indices)

    # lacking ones get 10
    num_copies = 10 if lacking else 1

    for i in range(num_copies):
        suffix = f"_{i}" if lacking else ""

        # create and save noisy
        noisy = apply_noise(tensor)
        out_noisy = augmented_dir / f"noisy{suffix}_{t_file.name}"
        np.save(out_noisy, noisy)

        # create and save scaled
        scaled = apply_scale(tensor)
        out_scaled = augmented_dir / f"scaled{suffix}_{t_file.name}"
        np.save(out_scaled, scaled)


def process_exercise_directory(exercise_dir, ex_config):
    ex_name = exercise_dir.name
    num_classes = ex_config["classes"]
    lacking_indices = ex_config["lacking_indices"]

    augmented_dir = exercise_dir / "augmented_dataset"

    if augmented_dir.exists():
        shutil.rmtree(augmented_dir)
    augmented_dir.mkdir(exist_ok=True)

    # get base files
    npy_files = []
    for f in exercise_dir.rglob("*.npy"):
        if "augmented_dataset" not in f.parts:
            npy_files.append(f)

    for t_file in npy_files:
        process_tensor_file(t_file, augmented_dir, num_classes, lacking_indices)

    print(f"{ex_name}: finished augmenting")


def main():
    TENSORS_DIR = Path("data/processed_tensors")

    for exercise_dir in TENSORS_DIR.iterdir():
        if not exercise_dir.is_dir() or exercise_dir.name not in EXERCISES:
            continue

        ex_config = EXERCISES[exercise_dir.name]
        process_exercise_directory(exercise_dir, ex_config)


if __name__ == "__main__":
    main()
