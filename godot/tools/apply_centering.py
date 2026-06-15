#!/usr/bin/env python3
"""Set `nodes/import_script/path` on every *.fbx.import to point at the
centering post-import script. Idempotent — safe to re-run.
"""

import re
import sys
from pathlib import Path

MODELS_DIR = Path(__file__).resolve().parent.parent / "models"
SCRIPT_PATH = "res://tools/center_fbx.gd"

LINE_RE = re.compile(r'^import_script/path=".*"$', re.MULTILINE)


def patch_one(import_file: Path) -> str:
    text = import_file.read_text()
    new_line = f'import_script/path="{SCRIPT_PATH}"'
    if LINE_RE.search(text):
        if new_line in text:
            return "already-set"
        text = LINE_RE.sub(new_line, text)
    else:
        return "no-import-script-field"
    import_file.write_text(text)
    return "patched"


def main():
    targets = sorted(MODELS_DIR.rglob("*.fbx.import"))
    if not targets:
        print(f"No *.fbx.import files under {MODELS_DIR}. Open the Godot "
              f"editor at least once so it generates them, then re-run.",
              file=sys.stderr)
        sys.exit(1)
    counts = {"patched": 0, "already-set": 0, "no-import-script-field": 0}
    for t in targets:
        status = patch_one(t)
        counts[status] += 1
        print(f"  {status:24s} {t.relative_to(MODELS_DIR.parent)}")
    print()
    print(f"Patched: {counts['patched']}, "
          f"Already set: {counts['already-set']}, "
          f"No field: {counts['no-import-script-field']}")
    if counts["patched"] or counts["already-set"]:
        print("\nNow in the Godot editor: right-click the `models/` folder in "
              "the FileSystem dock → Reimport. The script will run on each FBX.")


if __name__ == "__main__":
    main()
