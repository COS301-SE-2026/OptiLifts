from pathlib import Path

import torch.nn.functional as F
import torch
from torch.utils.data import DataLoader

from datasets.exercise_dataset import ExerciseDataset
from architecture.cnn_1d import ExerciseCNN1D
from torchmetrics.classification import MultilabelF1Score

import random
from torch.utils.data import Subset
from collections import defaultdict

import argparse

import re


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


def parse_arguments():
    parser = argparse.ArgumentParser(description="OptiVision Model Trainer")
    parser.add_argument(
        "--exercise",
        type=str,
        required=True,
        choices=["squat", "bench_press", "deadlift"],
        help="Name of the exercise folder",
    )
    parser.add_argument(
        "--classes", type=int, required=True, help="Number of labels for this exercise"
    )
    return parser.parse_args()


def get_core_mappings(full_dataset, num_classes):
    # get core names so can keep video and augmented versions in same split
    core_to_indices = defaultdict(list)
    core_to_labels = {}
    for i, path in enumerate(full_dataset.file_paths):
        core_name = re.sub(r"^(noisy|scaled)(_\d+)?_", "", path.name)
        core_name = core_name.replace("mirrored_", "")

        core_to_indices[core_name].append(i)

        name_no_ext = path.name.replace(".npy", "")
        parts = name_no_ext.split("_")
        labels = [float(x) for x in parts[-num_classes:]]
        core_to_labels[core_name] = labels

    return core_to_indices, core_to_labels


def calculate_split_counts(unique_cores, train_c, core_to_labels, num_classes):
    train_flaw_counts = [0] * num_classes
    total_flaw_counts = [0] * num_classes

    for i in unique_cores:
        labels = core_to_labels[i]
        for j in range(num_classes):
            if labels[j] > 0:
                total_flaw_counts[j] += 1
                if i in train_c:
                    train_flaw_counts[j] += 1

    return train_flaw_counts, total_flaw_counts


def get_smallest_ratio(train_flaw_counts, total_flaw_counts, num_classes):
    min_ratio = 1.0
    for i in range(num_classes):
        if total_flaw_counts[i] > 0:
            ratio = train_flaw_counts[i] / total_flaw_counts[i]
            if ratio < min_ratio:
                min_ratio = ratio
    return min_ratio


def stratified_split(core_to_indices, core_to_labels, num_classes):
    unique_cores = list(core_to_indices.keys())
    train_core_count = int(0.8 * len(unique_cores))

    # stratified split, atleast 75% of each class is in the training set
    best_split = None
    best_min_ratio = -1.0

    iteration = 0
    max_iterations = 1000

    while best_min_ratio < 0.75 and iteration < max_iterations:
        random.shuffle(unique_cores)  # NOSONAR
        train_c = unique_cores[:train_core_count]

        train_flaw_counts, total_flaw_counts = calculate_split_counts(
            unique_cores, train_c, core_to_labels, num_classes
        )
        min_ratio = get_smallest_ratio(
            train_flaw_counts, total_flaw_counts, num_classes
        )

        if min_ratio > best_min_ratio:
            best_min_ratio = min_ratio
            best_split = (train_c, unique_cores[train_core_count:])

        iteration += 1

    print(f"Stratified Split: {best_min_ratio* 100}%")
    return best_split


def calculate_dynamic_weights(train_indices, full_dataset, num_classes, device):
    # adjusted weights w/ cost-sensitive learning
    pos_counts = torch.zeros(num_classes)
    for idx in train_indices:
        _, labels = full_dataset[idx]
        pos_counts += labels

    total_train = len(train_indices)
    neg_counts = total_train - pos_counts
    pos_counts = torch.clamp(pos_counts, min=1.0)

    # inverse ratio
    dynamic_weights = (neg_counts / pos_counts).to(device)
    dynamic_weights = torch.clamp(dynamic_weights, max=15.0)
    print(f"Dynamic Class Penalties: {dynamic_weights.cpu().numpy()}")

    return dynamic_weights


def train_model(
    model,
    train_loader,
    val_loader,
    criterion_train,
    criterion_eval,
    optimizer,
    scheduler,
    epochs,
    weights_dir,
    model_save_name,
    device,
    val_meta,
):
    best_val_loss = float("inf")
    best_val_f1 = 0.0
    best_epoch = 1

    # training loop
    for epoch in range(epochs):
        model.train()
        train_loss = 0.0

        for tensors, labels in train_loader:
            tensors, labels = tensors.to(device), labels.to(device)

            optimizer.zero_grad()
            outputs = model(tensors)
            loss = criterion_train(outputs, labels)
            loss.backward()
            optimizer.step()

            train_loss += loss.item()

        avg_train_loss = train_loss / len(train_loader)

        model.eval()
        val_loss = 0.0

        all_val_probs = []
        all_val_targets = []

        with torch.no_grad():
            for tensors, labels in val_loader:
                tensors, labels = tensors.to(device), labels.to(device)
                outputs = model(tensors)
                loss = criterion_eval(outputs, labels)
                val_loss += loss.item()

                all_val_probs.append(torch.sigmoid(outputs).cpu())
                all_val_targets.append(labels.cpu())

        avg_val_loss = val_loss / len(val_loader)

        # video level F1
        # get all batches together
        all_val_probs = torch.cat(all_val_probs, dim=0)
        all_val_targets = torch.cat(all_val_targets, dim=0)

        # map windows to rows in concatenated tensor
        idx_to_row = {orig_idx: row for row, orig_idx in enumerate(val_meta["indices"])}

        video_preds = []
        video_targets = []

        # get window preds for each vid
        for i in val_meta["cores"]:
            row_pos = [idx_to_row[idx] for idx in val_meta["mapping"][i]]
            vid_wndw_probs = all_val_probs[row_pos]
            vid_max_probs = torch.max(vid_wndw_probs, dim=0)[0]

            video_preds.append(vid_max_probs)
            video_targets.append(
                all_val_targets[row_pos[0]]
            )  # answer key to check against

        preds_tensor = torch.stack(video_preds).to(device)
        targets_tensor = torch.stack(video_targets).to(device)

        metric = MultilabelF1Score(
            num_labels=val_meta["num_classes"], threshold=0.5, average="macro"
        ).to(device)
        val_f1 = metric(preds_tensor, targets_tensor.long()).item()

        print(
            f"Epoch [{epoch+1}/{epochs}] | Train Loss: {avg_train_loss:.4f} | Val Loss: {avg_val_loss:.4f} | Video Val F1: {val_f1*100:.1f}%"
        )

        # save best model
        is_best = (val_f1 > best_val_f1 + 1e-4) or (
            abs(val_f1 - best_val_f1) <= 1e-4 and avg_val_loss < best_val_loss
        )

        if is_best:
            best_val_f1 = val_f1
            best_val_loss = avg_val_loss
            best_epoch = epoch + 1

            model = model.to("cpu")
            scripted_model = torch.jit.script(model)
            scripted_model.save(weights_dir / model_save_name)
            model = model.to(device)

        scheduler.step(avg_val_loss)

    print(
        f"\nTraining complete: Epoch {best_epoch} with Video Val F1: {best_val_f1*100:.1f}% (Val Loss: {best_val_loss:.4f})"
    )


def main():
    args = parse_arguments()

    # fix variance with seed
    random.seed(81)
    torch.manual_seed(81)

    # params
    TENSOR_DIR = Path("data/processed_tensors")
    WEIGHTS_DIR = Path("training/weights")
    WEIGHTS_DIR.mkdir(exist_ok=True, parents=True)

    EPOCHS = 50
    BATCH_SIZE = 16
    LEARNING_RATE = 0.001

    NUM_CLASSES = args.classes
    EXERCISE_NAME = args.exercise
    MODEL_SAVE_NAME = f"{EXERCISE_NAME}_side.pt"

    device = torch.device("cuda")

    # load dataset
    full_dataset = ExerciseDataset(TENSOR_DIR, EXERCISE_NAME, num_classes=NUM_CLASSES)

    core_to_indices, core_to_labels = get_core_mappings(full_dataset, NUM_CLASSES)

    train_cores, val_cores = stratified_split(
        core_to_indices, core_to_labels, NUM_CLASSES
    )

    train_indices = []
    for i in train_cores:
        train_indices.extend(core_to_indices[i])

    val_indices = []
    for i in val_cores:
        val_indices.extend(core_to_indices[i])

    # create datasets
    train_dataset = Subset(full_dataset, train_indices)
    val_dataset = Subset(full_dataset, val_indices)

    train_size = len(train_dataset)
    val_size = len(val_dataset)

    train_loader = DataLoader(
        train_dataset,
        batch_size=BATCH_SIZE,
        shuffle=True,
        num_workers=4,
        collate_fn=pad_batch,
    )
    val_loader = DataLoader(
        val_dataset,
        batch_size=BATCH_SIZE,
        shuffle=False,
        num_workers=4,
        collate_fn=pad_batch,
    )

    print(f"Training size: {train_size}, Validation size: {val_size}")

    # model and optimizer
    model = ExerciseCNN1D(num_classes=NUM_CLASSES).to(device)

    dynamic_weights = calculate_dynamic_weights(
        train_indices, full_dataset, NUM_CLASSES, device
    )

    criterion_train = torch.nn.BCEWithLogitsLoss(pos_weight=dynamic_weights)
    criterion_eval = torch.nn.BCEWithLogitsLoss()

    #  wow 314 stuff
    optimizer = torch.optim.Adam(
        model.parameters(), lr=LEARNING_RATE, weight_decay=1e-4
    )
    scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(
        optimizer, mode="min", factor=0.1, patience=3
    )

    val_meta = {
        "cores": val_cores,
        "indices": val_indices,
        "mapping": core_to_indices,
        "num_classes": NUM_CLASSES,
    }

    train_model(
        model=model,
        train_loader=train_loader,
        val_loader=val_loader,
        criterion_train=criterion_train,
        criterion_eval=criterion_eval,
        optimizer=optimizer,
        scheduler=scheduler,
        epochs=EPOCHS,
        weights_dir=WEIGHTS_DIR,
        model_save_name=MODEL_SAVE_NAME,
        device=device,
        val_meta=val_meta,
    )


if __name__ == "__main__":
    main()
