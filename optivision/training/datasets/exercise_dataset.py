from pathlib import Path
    
import numpy as np
import torch
from torch.utils.data import Dataset


class ExerciseDataset(Dataset):
    def __init__(self, tensor_dir: Path, exercise_name: str, num_classes: int):
        self.num_classes = num_classes
        
        exercise_path = tensor_dir / exercise_name
        self.file_paths = list(exercise_path.rglob("*.npy"))
        
        if len(self.file_paths) == 0:
            raise ValueError(f"No npy files found in {exercise_path}")

    def __len__(self):
        return len(self.file_paths)

    def _extract_labels(self, filename: str) -> list[float]:
        name_no_ext = filename.replace(".npy", "")
        parts = name_no_ext.split("_")
        
        labels = parts[-self.num_classes:]
        
        return [float(x) for x in labels]

    def __getitem__(self, idx):
        file_path = self.file_paths[idx]
        
        tensor_np = np.load(file_path)   
        labels_np = np.array(self._extract_labels(file_path.name), dtype=np.float32)
        
        # 33 landmarks x 3 coords = 99 
        frames = tensor_np.shape[0]
        tensor_np = tensor_np.reshape((frames, 99))
        
        tensor = torch.from_numpy(tensor_np)
        labels = torch.from_numpy(labels_np)
        
        # convid needs channels then seq length
        tensor = tensor.transpose(0, 1)
        
        return tensor, labels
