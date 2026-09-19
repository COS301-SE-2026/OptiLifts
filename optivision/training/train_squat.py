from pathlib import Path

import torch.nn.functional as F
import torch
from torch.utils.data import DataLoader, random_split

from datasets.exercise_dataset import ExerciseDataset
from architecture.cnn_1d import ExerciseCNN1D

import random
from torch.utils.data import Subset
from collections import defaultdict

def pad_batch(batch):
    max_frames = max([item[0].shape[1] for item in batch])
        
    padded_tensors = []
    labels_list = []
    
    for tensor, labels in batch:
        pad_size = max_frames - tensor.shape[1]
        padded_tensor = F.pad(tensor, (0, pad_size))
        
        padded_tensors.append(padded_tensor)
        labels_list.append(labels)
        
    batch_tensors = torch.stack(padded_tensors)
    batch_labels = torch.stack(labels_list)
    
    return batch_tensors, batch_labels

def main(): 
    # params
    TENSOR_DIR = Path("data/processed_tensors")
    WEIGHTS_DIR = Path("weights")
    WEIGHTS_DIR.mkdir(exist_ok=True)

    EPOCHS = 50
    BATCH_SIZE = 16
    LEARNING_RATE = 0.001
    NUM_CLASSES = 3

    best_val_loss = float('inf')

    device = torch.device("cuda")

    # load dataset

    full_dataset = ExerciseDataset(TENSOR_DIR, "squat", num_classes=NUM_CLASSES)

    # get core names so can keep video and augmented versions in same split
    core_to_indices = defaultdict(list)
    for i, path in enumerate(full_dataset.file_paths):
        core_name = path.name.replace("noisy_", "").replace("scaled_", "").replace("mirrored_", "")
        core_to_indices[core_name].append(i)
        
    # shuffle core vids
    unique_cores = list(core_to_indices.keys())
    random.shuffle(unique_cores) #NOSONAR
    
    train_core_count = int(0.8 * len(unique_cores))
    train_cores = unique_cores[:train_core_count]
    val_cores = unique_cores[train_core_count:]
    
    train_indices = []
    for core in train_cores:
        train_indices.extend(core_to_indices[core])
        
    val_indices = []
    for core in val_cores:
        val_indices.extend(core_to_indices[core])
        
    # create datasets
    train_dataset = Subset(full_dataset, train_indices)
    val_dataset = Subset(full_dataset, val_indices)

    train_size = len(train_dataset)
    val_size = len(val_dataset)

    train_loader = DataLoader(train_dataset, batch_size=BATCH_SIZE, shuffle=True, num_workers=4, collate_fn=pad_batch)
    val_loader = DataLoader(val_dataset, batch_size=BATCH_SIZE, shuffle=False, num_workers=4, collate_fn=pad_batch)

    print(f"Training size: {train_size}, Validation size: {val_size}")

    # model and optimizer

    model = ExerciseCNN1D(num_classes=NUM_CLASSES).to(device)
    criterion = torch.nn.BCELoss()
    #  wow 314 stuff
    optimizer = torch.optim.Adam(model.parameters(), lr=LEARNING_RATE, weight_decay=1e-4)

    # training loop
    for epoch in range(EPOCHS):
        model.train()
        train_loss = 0.0

        for tensors, labels in train_loader:
            tensors, labels = tensors.to(device), labels.to(device)

            optimizer.zero_grad()
            outputs = model(tensors)
            loss = criterion(outputs, labels)
            loss.backward()
            optimizer.step()

            train_loss += loss.item() 

        avg_train_loss = train_loss / len(train_loader)

        model.eval()
        val_loss = 0.0

        with torch.no_grad():
            for tensors, labels in val_loader:
                tensors, labels = tensors.to(device), labels.to(device)

                outputs = model(tensors)
                loss = criterion(outputs, labels)

                val_loss += loss.item()

        avg_val_loss = val_loss / len(val_loader)

        print(f"Epoch [{epoch+1}/{EPOCHS}] | Train Loss: {avg_train_loss:.4f} | Val Loss: {avg_val_loss:.4f}")


        # save best model
        if avg_val_loss < best_val_loss:
            best_val_loss = avg_val_loss
            
            model = model.to("cpu")
            scripted_model = torch.jit.script(model)
            scripted_model.save(WEIGHTS_DIR / "squat_side.pt")
            model = model.to(device) 

if __name__ == "__main__":
    main()