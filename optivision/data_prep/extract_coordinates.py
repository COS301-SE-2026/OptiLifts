from pathlib import Path

import cv2
import mediapipe as mp
import numpy as np
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
from tqdm import tqdm

RAW_VIDEOS_DIR = Path("../data/raw_videos")
PROCESSED_TENSOR_DIR = Path("../data/processed_tensors")
MODEL_PATH = "pose_landmarker.task"


def process_vid(vid_path, output_dir):
    base_options = python.BaseOptions(model_asset_path=MODEL_PATH)

    # video mode for better tracking
    options = vision.PoseLandmarkerOptions(
        base_options=base_options,
        running_mode=vision.RunningMode.VIDEO,
        num_poses=1,
        min_pose_detection_confidence=0.5,
        min_pose_presence_confidence=0.5,
        min_tracking_confidence=0.5,
    )

    output_dir.parent.mkdir(parents=True, exist_ok=True)
    vid_cap = cv2.VideoCapture(str(vid_path))

    vid_landmarks = []

    with vision.PoseLandmarker.create_from_options(options) as landmarker:
        while vid_cap.isOpened():
            ret, frame = vid_cap.read()
            if not ret:
                break

            # BGR to RGB
            frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=frame_rgb)

            # provide exact timestamp for video tracking mode
            timestamp_ms = int(vid_cap.get(cv2.CAP_PROP_POS_MSEC))

            if len(vid_landmarks) > 0 and timestamp_ms <= 0:
                timestamp_ms = len(vid_landmarks) * 33

            detection_result = landmarker.detect_for_video(mp_image, timestamp_ms)

            # found body landmarks
            if detection_result.pose_landmarks:
                frame_landmarks = []
                for lm in detection_result.pose_landmarks[0]:
                    frame_landmarks.append([lm.x, lm.y, lm.z])

                # add frame coords to list for whole video
                vid_landmarks.append(frame_landmarks)
            else:
                # no body found so add empty frame so tensor shape is consistent
                vid_landmarks.append(np.zeros((33, 3)).tolist())

    vid_cap.release()

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
