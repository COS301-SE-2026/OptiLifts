from pathlib import Path

import cv2
import mediapipe as mp
import numpy as np
from tqdm import tqdm

RAW_VIDEOS_DIR = Path("..data/raw_videos")
PROCESSED_TENSOR_DIR = Path("..data/processed_tensors")

mp_pose = mp.solutions.pose


def process_vid(vid_path, output_dir):

    pose = mp_pose.Pose(
        static_image_mode=False,
        model_complexity=2,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5,
    )

    output_dir.parent.mkdir(parents=True, exist_ok=True)
    vid_cap = cv2.VideoCapture(str(vid_path))

    vid_landmarks = []

    while vid_cap.isOpened():
        ret, frame = vid_cap.read()
        if not ret:
            break

        # BGR to RGB
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        results = pose.process(frame_rgb)

        # found body landmarks
        if results.pose_landmarks:
            frame_landmarks = []
            for lm in results.pose_landmarks.landmark:
                frame_landmarks.append([lm.x, lm.y, lm.z])

            # add frame coords to to list for whole video
            vid_landmarks.append(frame_landmarks)
        else:
            # no body found so add empty frame so tensor shape is consistent
            vid_landmarks.append(np.zeros((33, 3)).tolist())

    vid_cap.release()
    pose.close()

    # convert to optimal 32 bit float numpy array - thanks Keith :)
    # minimal accuracy loss but helps a lot with memory usage and speed
    tensor = np.array(vid_landmarks, dtype=np.float32)
    np.save(str(output_dir), tensor)


def main():
    vid_files = list(RAW_VIDEOS_DIR.rglob("*.mp4"))

    if not vid_files:
        print("No videos found in the raw_videos directory")
        return

    print(f"Processing {len(vid_files)} videos")

    for vid_file in tqdm(vid_files, desc="Processing videos"):
        path = vid_file.relative_to(RAW_VIDEOS_DIR)
        output_file = path.with_suffix(".npy")
        output_path = PROCESSED_TENSOR_DIR / output_file

        if output_path.exists():
            continue

        process_vid(vid_file, output_path)

    print("Videos have been processed into tensors")


# only runs if script executed directly
if __name__ == "__main__":
    main()
