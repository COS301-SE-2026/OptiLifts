import os
import pandas as pd

RAW_DIR = "/mnt/c/Users/jn390/Downloads/squatvideos/"
OUT_DIR = "/mnt/c/Users/jn390/Downloads/processed_squats/"
PREFIX = "js"

def process_dataset():
    os.makedirs(OUT_DIR, exist_ok=True)
    csv_path = os.path.join(RAW_DIR, "dataset_tracker.csv")
    
    if not os.path.exists(csv_path):
        print(f"error: Could not find {csv_path}")
        return

    print(f"loading data from {csv_path}")
    
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
        d_err = int(row.get('shallow_depth', 0))
        l_err = int(row.get('excessive_forward_lean', 0))
        h_err = int(row.get('heels_raised', 0))
        r_err = int(row.get('rounded_back', 0))  

        # adjust depending on how many errors there are
        final_filename = f"{PREFIX}_{index}_{d_err}_{l_err}_{h_err}_{r_err}.mp4"
        input_path = os.path.join(RAW_DIR, raw_file)
        output_path = os.path.join(OUT_DIR, final_filename)
        
        if not os.path.exists(input_path):
            print(f"Warning: {raw_file} not found in {RAW_DIR}")
            continue
            
        ffmpeg_cmd = (
            f'ffmpeg -y -hide_banner -loglevel error -i "{input_path}" '
            f'-ss {start} -to {end} '
            f'-vf "crop=iw/2:ih:iw/2:0" '
            f'"{output_path}"'
        )
        
        print(f"Processing {raw_file} -> {final_filename}...")
        os.system(ffmpeg_cmd)

    print("\n all videos processed successfully")

if __name__ == "__main__":
    process_dataset()
