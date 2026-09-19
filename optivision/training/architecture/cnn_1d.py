import torch
import torch.nn as nn


class ExerciseCNN1D(nn.Module):
    def __init__(self, num_classes: int):
        super(ExerciseCNN1D, self).__init__()

        # feature extraction
        self.features = nn.Sequential(
            nn.Conv1d(in_channels=99, out_channels=128, kernel_size=3, padding=1),
            nn.BatchNorm1d(128),
            # reLU means can learn complex curves instead of linear patterns
            nn.ReLU(),
            nn.MaxPool1d(kernel_size=2),
            nn.Conv1d(in_channels=128, out_channels=256, kernel_size=3, padding=1),
            nn.BatchNorm1d(256),
            nn.ReLU(),
            nn.MaxPool1d(kernel_size=2),
            nn.Conv1d(in_channels=256, out_channels=512, kernel_size=3, padding=1),
            nn.BatchNorm1d(512),
            nn.ReLU(),
        )

        # standardise the time dimension to 1
        self.global_pool = nn.AdaptiveMaxPool1d(1)

        # classification
        self.classifier = nn.Sequential(
            nn.Linear(512, 256),
            nn.ReLU(),
            nn.Dropout(0.5),
            nn.Linear(256, num_classes),
            nn.Sigmoid(),
        )

    def forward(self, x):
        x = self.features(x)
        x = self.global_pool(x)
        x = torch.flatten(x, 1)
        x = self.classifier(x)
        return x
