#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TARGET="$ROOT/Source/KSP_osx/GameData/AircraftAutopilotContinued"
if [[ "${1:-}" == --uninstall ]]; then
    # This fixed path is exclusively this mod's directory. No unrelated GameData files.
    python3 - "$TARGET" <<'PY'
from pathlib import Path
import shutil,sys
p=Path(sys.argv[1])
assert p.name=='AircraftAutopilotContinued' and p.parent.name=='GameData'
if p.is_dir(): shutil.rmtree(p)
print('Uninstalled Aircraft Autopilot Continued')
PY
    exit 0
fi
[[ $# -eq 0 ]] || { echo 'Usage: install.sh [--uninstall]' >&2; exit 1; }
[[ -f "$ROOT/dist/GameData/AircraftAutopilotContinued/Plugins/AircraftAutopilotContinued.dll" ]] || { echo 'Run scripts/build.sh first' >&2; exit 1; }
mkdir -p "$TARGET"
cp -R "$ROOT/dist/GameData/AircraftAutopilotContinued/" "$TARGET/"
printf 'Installed: %s/Plugins/AircraftAutopilotContinued.dll\n' "$TARGET"

python3 "$ROOT/scripts/verify.py" --installed
