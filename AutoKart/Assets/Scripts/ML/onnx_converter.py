from ultralytics import YOLO
from pathlib import Path
import sys
import shutil

def export_for_unity(run_path: str):
    run_dir = Path(run_path)
    weights_dir = run_dir / "weights"
    model_path = weights_dir / "best.pt"
    onnx_path = weights_dir / "best.onnx"
    dest_path = Path(__file__).parent / "best.onnx"

    assert model_path.exists(), f"Model not found at {model_path}"

    print(f"📦 Loading model: {model_path}")
    model = YOLO(str(model_path))

    print("🔄 Exporting to ONNX...")
    model.export(format="onnx", dynamic=True, simplify=True)

    if not onnx_path.exists():
        raise FileNotFoundError(f"ONNX file not found at {onnx_path}")

    shutil.copy(onnx_path, dest_path)
    print(f"✅ Copied ONNX to: {dest_path}")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python onnx_converter.py <run_dir>")
        sys.exit(1)
    export_for_unity(sys.argv[1])
