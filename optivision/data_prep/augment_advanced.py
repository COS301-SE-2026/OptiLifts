from pathlib import Path
import numpy as np

def apply_noise(tensor, noise_level=0.01):
    noisy_tensor = tensor.copy()
    valid_mask = np.any(tensor != 0, axis=-1)
    
    # small random noise
    noise = np.random.normal(0, noise_level, noisy_tensor[valid_mask].shape) #NOSONAR
    noisy_tensor[valid_mask] += noise
    return noisy_tensor

def apply_scale(tensor, scale_min=0.9, scale_max=1.1):
    scaled_tensor = tensor.copy()
    valid_mask = np.any(tensor != 0, axis=-1)
    
    # random scale factor for height
    scale_factor = np.random.uniform(scale_min, scale_max) #NOSONAR
    scaled_tensor[valid_mask] *= scale_factor
    return scaled_tensor

def main():
    TENSORS_DIR = Path("data/processed_tensors")
    
    for exercise_dir in TENSORS_DIR.iterdir():
        if not exercise_dir.is_dir():
            continue
        
        augmented_dir = exercise_dir / "augmented_dataset"
        augmented_dir.mkdir(exist_ok=True)
        
        # get base files
        npy_files = []
        for f in exercise_dir.rglob("*.npy"):
            if "augmented_dataset" not in f.parts:
                npy_files.append(f)
                
        for t_file in npy_files:
            tensor = np.load(t_file)
            
            # create and save noisy
            noisy = apply_noise(tensor)
            out_noisy = augmented_dir / f"noisy_{t_file.name}"
            np.save(out_noisy, noisy)
            
            # create and save scaled
            scaled = apply_scale(tensor)
            out_scaled = augmented_dir / f"scaled_{t_file.name}"
            np.save(out_scaled, scaled)

if __name__ == "__main__":
    main()
