import os

import pandas as pd

RAW_DIR = "/mnt/directory/"
OUT_DIR = "/mnt/directory/output/"
PREFIX = "od"


def process_dataset():
    os.makedirs(OUT_DIR, exist_ok=True)
    csv_path = os.path.join(RAW_DIR, "dataset_tracker.csv")

    if not os.path.exists(csv_path):
        print(f"Error: Could not find {csv_path}")
        return

    print(f"Loading tracking data from {csv_path}")

    # Read CSV using semicolon
    try:
        df = pd.read_csv(csv_path, sep=";")
    except pd.errors.ParserError as e:
        print(f"Error reading CSV: {e}")
        df = pd.read_csv(csv_path)

    df.columns = df.columns.str.strip().str.lower()

    if "raw_filename" not in df.columns:
        print("Error: Could not find 'raw_filename' column.")
        return

    for index, row in df.iterrows():
        raw_file = str(row["raw_filename"]).strip()
        start = row["start_time"]
        end = row["end_time"]

        # pulling using the exact column names in CSV
        l_flex = int(row.get("lumbar_flexion", 0))
        h_rise = int(row.get("bad_hip_movement", 0))
        b_drift = int(row.get("bar_drifting", 0))
        k_fwd = int(row.get("knees_forward", 0))
        s_depth = int(row.get("shallow_depth", 0))

        final_filename = (
            f"{PREFIX}_{index}_{l_flex}_{h_rise}_{b_drift}_{k_fwd}_{s_depth}.mp4"
        )
        input_path = os.path.join(RAW_DIR, raw_file)
        output_path = os.path.join(OUT_DIR, final_filename)

        if not os.path.exists(input_path):
            print(f"Warning: {raw_file} not found in {RAW_DIR}")
            continue

        ffmpeg_cmd = (
            f'ffmpeg -y -hide_banner -loglevel error -i "{input_path}" '
            f"-ss {start} -to {end} "
            f"-c:v libx264 -pix_fmt yuv420p "
            f'"{output_path}"'
        )

        print(f"Processing {raw_file} -> {final_filename}")
        os.system(ffmpeg_cmd)

    print("\n All videos processed successfully!")


if __name__ == "__main__":
    process_dataset()
