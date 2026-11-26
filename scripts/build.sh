#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export DOTNET_CLI_HOME="$ROOT/.tools/home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET="${DOTNET:-$ROOT/.tools/dotnet/dotnet}"
if [[ ! -x "$DOTNET" ]]; then DOTNET="$(command -v dotnet || true)"; fi
[[ -x "$DOTNET" ]] || { echo 'Install a .NET SDK or set DOTNET to its executable.' >&2; exit 1; }
SDK_LINE="$("$DOTNET" --list-sdks | tail -1)"
SDK_VERSION="${SDK_LINE%% *}"
SDK_BASE="$(printf '%s' "$SDK_LINE" | sed -n 's/.*\[\(.*\)\]/\1/p')"
CSC="$SDK_BASE/$SDK_VERSION/Roslyn/bincore/csc.dll"
[[ -f "$CSC" ]] || { echo 'Cannot locate SDK compiler.' >&2; exit 1; }
MANAGED="$ROOT/Source/KSP_osx/KSP.app/Contents/Resources/Data/Managed"
mkdir -p build dist/GameData/AircraftAutopilotContinued/Plugins dist/GameData/AircraftAutopilotContinued/Icons
REFS=()
for name in mscorlib System System.Core Assembly-CSharp Assembly-CSharp-firstpass UnityEngine UnityEngine.CoreModule UnityEngine.IMGUIModule UnityEngine.InputLegacyModule UnityEngine.ImageConversionModule UnityEngine.UIModule UnityEngine.UI UnityEngine.AnimationModule; do
  [[ ! -f "$MANAGED/$name.dll" ]] || REFS+=("-r:$MANAGED/$name.dll")
done
"$DOTNET" "$CSC" -nologo -noconfig -nostdlib+ -langversion:7.3 -target:library -optimize+ "${REFS[@]}" -out:build/AircraftAutopilotContinued.dll Source/AircraftAutopilotContinued/*.cs
cp build/AircraftAutopilotContinued.dll dist/GameData/AircraftAutopilotContinued/Plugins/
cp assets/AppLauncherIcon.png dist/GameData/AircraftAutopilotContinued/Icons/
cp LICENSE THIRD_PARTY_NOTICES.md README.md dist/GameData/AircraftAutopilotContinued/
cp -R docs dist/GameData/AircraftAutopilotContinued/
python3 scripts/package.py
printf 'Built and packaged AircraftAutopilotContinued.dll\n'
