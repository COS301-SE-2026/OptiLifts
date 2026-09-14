import os
import pandas as pd

RAW_DIR = "/mnt/c/Users/jn390/Downloads/deadliftvideos/"
OUT_DIR = "/mnt/c/Users/jn390/Downloads/processed_deadlifts/"
PREFIX = "jd"

def process_dataset():
    os.makedirs(OUT_DIR, exist_ok=True)
    csv_path = os.path.join(RAW_DIR, "dataset_tracker.csv")
    
    if not os.path.exists(csv_path):
        print(f"Error: Could not find {csv_path}")
        return

    print(f"Loading tracking data from {csv_path}...")
    
    # Read CSV using semicolon
    try:
        df = pd.read_csv(csv_path, sep=';')
    except Exception as e:
        print(f"Error reading CSV: {e}")
        df = pd.read_csv(csv_path)

    df.columns = df.columns.str.strip().str.lower()
    
    if 'raw_filename' not in df.columns:
        print("Error: Could not find 'raw_filename' column.")
        return

    for index, row in df.iterrows():
        raw_file = str(row['raw_filename']).strip()
        start = row['start_time']
        end = row['end_time']
        
        # pulling using the exact column names in CSV
        l_flex = int(row.get('lumbar_flexion', 0))
        h_rise = int(row.get('hips_early_rise', 0))
        b_drift = int(row.get('bar_drifting', 0))
        k_fwd = int(row.get('knees_forward', 0))
        l_hyper = int(row.get('lockout_hyperextension', 0))
        
        final_filename = f"{PREFIX}_{index}_{l_flex}_{h_rise}_{b_drift}_{k_fwd}_{l_hyper}.mp4"
        input_path = os.path.join(RAW_DIR, raw_file)
        output_path = os.path.join(OUT_DIR, final_filename)
        
        if not os.path.exists(input_path):
            print(f"Warning: {raw_file} not found in {RAW_DIR}. Skipping...")
            continue
            
        ffmpeg_cmd = (
            f'ffmpeg -y -hide_banner -loglevel error -i "{input_path}" '
            f'-ss {start} -to {end} '
            f'-vf "crop=iw/2:ih:iw/2:0" '
            f'"{output_path}"'
        )
        
        print(f"Processing {raw_file} -> {final_filename}...")
        os.system(ffmpeg_cmd)

    print("\n All videos processed successfully!")

if __name__ == "__main__":
    process_dataset()
