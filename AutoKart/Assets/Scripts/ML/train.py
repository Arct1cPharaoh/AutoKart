from collections import defaultdict
from ultralytics import YOLO
from pathlib import Path
import shutil
import random
from tqdm import tqdm
import yaml

# === Paths ===
image_dir = Path("dataset_yolo/images")
label_dir = Path("dataset_yolo/labels")
split_base = Path("dataset_yolo_split")
split_base.mkdir(parents=True, exist_ok=True)

# === Get all image files ===
print("📊 Gathering labeled image paths...")
image_files = sorted([
    p for p in image_dir.glob("*")
    if p.suffix.lower() in [".jpg", ".jpeg", ".png"]
    and (label_dir / f"{p.name}.txt").exists()
])

print(f"\n📷 Total labeled images: {len(image_files)}")

# === Class distribution ===
print("📈 Class distribution:")
class_counts = defaultdict(int)
for img_path in tqdm(image_files, desc="Parsing labels"):
    label_path = label_dir / f"{img_path.name}.txt"
    with open(label_path, "r") as f:
        for line in f:
            class_id = int(line.split()[0])
            class_counts[class_id] += 1
for cls_id, count in sorted(class_counts.items()):
    print(f"  Class {cls_id}: {count}")

# === Shuffle and split ===
print("\n🔀 Shuffling and splitting dataset...")
random.seed(42)
random.shuffle(image_files)
n = len(image_files)
splits = {
    "train": image_files[:int(n * 0.8)],
    "val": image_files[int(n * 0.8):int(n * 0.9)],
    "test": image_files[int(n * 0.9):]
}

# === Copy files and rename labels ===
for split, files in splits.items():
    img_out = split_base / split / "images"
    lbl_out = split_base / split / "labels"
    img_out.mkdir(parents=True, exist_ok=True)
    lbl_out.mkdir(parents=True, exist_ok=True)
    
    print(f"\n📂 Copying {split} files...")
    for img_path in tqdm(files, desc=f"{split} set"):
        shutil.copy(img_path, img_out / img_path.name)

        src_label = label_dir / f"{img_path.name}.txt"
        dst_label = lbl_out / f"{img_path.stem}.txt"
        if src_label.exists():
            shutil.copy(src_label, dst_label)

# === Write data.yaml ===
yaml_path = split_base / "data.yaml"
print("\n📝 Writing data.yaml...")
yaml_content = {
    "train": str((split_base / "train").resolve()),
    "val": str((split_base / "val").resolve()),
    "test": str((split_base / "test").resolve()),
    "nc": 5,
    "names": [
        "yellow_cone",
        "blue_cone",
        "orange_cone",
        "large_orange_cone",
        "unknown_cone"
    ]
}
with open(yaml_path, "w") as f:
    yaml.dump(yaml_content, f)

# === Train ===
print("\n🚀 Starting YOLO training...")
model = YOLO("yolov8n.pt")
model.train(
    imgsz=640,
    epochs=50,
    batch=16,
    data=str(yaml_path),
    name="train_run",
)

# === Evaluate ===
print("\n📏 Evaluating on test set...")
metrics = model.val(data=str(yaml_path), split="test")
print("\n📊 Evaluation metrics:")
print(metrics)

# === Save example predictions ===
print("\n🖼️ Saving test set predictions...")
pred_dir = split_base / "test_predictions"
pred_dir.mkdir(exist_ok=True)
model.predict(
    source=str(split_base / "test" / "images"),
    save=True,
    save_txt=True,
    project=str(pred_dir.parent),
    name=pred_dir.name,
    imgsz=640,
    conf=0.25
)
