import argparse
import importlib.util
import json
import os
import sys

base_dir = os.path.dirname(__file__)
search_paths = [
    os.path.abspath(os.path.join(base_dir, "..", "..", "..", "engine")),
    os.path.abspath(os.path.join(base_dir, "..")),
    os.path.abspath(os.path.join(base_dir, "..", "..")),
    os.path.abspath(os.path.join(base_dir, "..", "..", "..")),
]
for p in search_paths:
    if p not in sys.path:
        sys.path.insert(0, p)


def load_marc_module():
    candidate = os.path.abspath(os.path.join(base_dir, "..", "..", "..", "engine", "marc_toplu_sorgula.py"))
    if not os.path.isfile(candidate):
        raise FileNotFoundError(f"marc_toplu_sorgula.py not found at: {candidate}")
    spec = importlib.util.spec_from_file_location("marc_toplu_sorgula", candidate)
    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load marc_toplu_sorgula module spec")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--cancel-flag", required=False)
    parser.add_argument("--cache-path", required=False, help="ISBN önbelleği SQLite dosyası (verilmezse önbellek kapalı)")
    args = parser.parse_args()
    marc_toplu_sorgula = load_marc_module()
    cancel_state = {"requested": False}
    # Ücretlendirme yalnızca bulunan ISBN'ler üzerinden yapılır; süreç iptalde öldürülebildiği için
    # bulunan sayısı son satırı beklemeden her ilerleme satırında da bildirilir.
    found_state = {"count": 0}

    def progress_callback(idx, total, _isbn, marc, message):
        if isinstance(marc, dict) and marc.get("status") == "found":
            found_state["count"] += 1
        payload = {
            "event": "progress",
            "processedCount": int(idx or 0),
            "foundCount": found_state["count"],
            "totalCount": int(total or 0),
            "message": message or "",
        }
        print(json.dumps(payload, ensure_ascii=True), flush=True)

    def cancel_check():
        if not args.cancel_flag:
            return False
        if os.path.exists(args.cancel_flag):
            cancel_state["requested"] = True
            return True
        return False

    try:
        results, output_file = marc_toplu_sorgula.run_batch(
            input_path=args.input,
            output_path=args.output,
            progress_callback=progress_callback,
            cancel_check=cancel_check,
            cache_path=args.cache_path or None,
        )

        payload = {
            "success": output_file is not None,
            "processedCount": len(results or []),
            "foundCount": sum(1 for r in (results or []) if r.get("status") == "found"),
            "outputPath": output_file or "",
            "error": None if output_file else ("Canceled" if cancel_state["requested"] else "No output file generated"),
            "canceled": bool(cancel_state["requested"]),
        }
        print(json.dumps(payload, ensure_ascii=True), flush=True)
        return 0
    except Exception as exc:
        payload = {
            "success": False,
            "processedCount": 0,
            "foundCount": 0,
            "outputPath": "",
            "error": str(exc),
            "canceled": bool(cancel_state["requested"]),
        }
        print(json.dumps(payload, ensure_ascii=True), flush=True)
        return 1


if __name__ == "__main__":
    sys.exit(main())
