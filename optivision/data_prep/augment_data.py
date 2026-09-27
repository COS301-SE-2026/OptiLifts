from pathlib import Path

import numpy as np
from tqdm import tqdm

PROCESSED_TENSOR_DIR = Path("../data/processed_tensors")


def main():
    npy_files = list(PROCESSED_TENSOR_DIR.rglob("*.npy"))
    original = [f for f in npy_files if not f.name.startswith("mirrored_")]

    if not original:
        print("No tensors to augment")
        return

    print(f"Augmenting {len(original)} tensors")

    for t_file in tqdm(original, desc="mirroring coords"):
        tensor = np.load(t_file)
        mirrored_t = tensor.copy()

        x_coords = tensor[:, :, 0]
        mirrored_t[:, :, 0] = np.where(x_coords != 0, 1.0 - x_coords, 0.0)

        output_file = f"mirrored_{t_file.name}"
        output_path = t_file.parent / output_file

        np.save(str(output_path), mirrored_t)

    print("Augmented tensors for side views")


if __name__ == "__main__":
    main()
