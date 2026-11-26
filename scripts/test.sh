#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export DOTNET_CLI_HOME="$ROOT/.tools/home" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
DOTNET="${DOTNET:-$ROOT/.tools/dotnet/dotnet}"
if [[ ! -x "$DOTNET" ]]; then DOTNET="$(command -v dotnet || true)"; fi
[[ -x "$DOTNET" ]] || { echo 'A .NET 8 SDK is required for tests.' >&2; exit 1; }
SDK_LINE="$("$DOTNET" --list-sdks | tail -1)"
SDK_VERSION="${SDK_LINE%% *}"
SDK_BASE="$(printf '%s' "$SDK_LINE" | sed -n 's/.*\[\(.*\)\]/\1/p')"
SDKROOT="$(dirname "$SDK_BASE")"
CSC="$SDK_BASE/$SDK_VERSION/Roslyn/bincore/csc.dll"
REFDIR="$(find "$SDKROOT/packs/Microsoft.NETCore.App.Ref" -type d -path '*/ref/net8.0' | sort | tail -1)"
[[ -n "$REFDIR" ]] || { echo 'Install a .NET 8 SDK for test reference assemblies.' >&2; exit 1; }
mkdir -p build
REFS=()
for ref in "$REFDIR"/*.dll; do REFS+=("-r:$ref"); done
SOURCES=()
for name in AircraftAutopilotController AircraftTelemetry PIDController MovingAverage Maths NumericField CallbackOwnership ConflictDetector; do SOURCES+=("Source/AircraftAutopilotContinued/$name.cs"); done
"$DOTNET" "$CSC" -nologo -noconfig -nostdlib+ -langversion:latest -target:exe "${REFS[@]}" -out:build/Tests.dll "${SOURCES[@]}" tests/*.cs tests/fixtures/*.cs
cat > build/Tests.runtimeconfig.json <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}
JSON
"$DOTNET" build/Tests.dll "$ROOT"
python3 scripts/verify.py
