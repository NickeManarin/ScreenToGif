#!/usr/bin/env bash
set -euo pipefail

project_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
scratch=$(mktemp -d /tmp/screentogif-desktop-XXXXXX)
evidence=${1:-$scratch/evidence}
mkdir -p "$scratch/config" "$evidence"
evidence=$(cd "$evidence" && pwd)
trap 'rm -rf "$scratch/config" "$scratch/webcam-config"' EXIT

if [[ ${XDG_SESSION_TYPE:-} == wayland ]]; then
    lock_state=$(gdbus call --session --dest org.gnome.ScreenSaver \
        --object-path /org/gnome/ScreenSaver --method org.gnome.ScreenSaver.GetActive)
    if [[ $lock_state != '(false,)' ]]; then
        echo "The desktop must be unlocked for native-window verification." >&2
        exit 1
    fi
fi

echo "Desktop evidence: $evidence"
cd "$project_root"
XDG_CONFIG_HOME="$scratch/config" \
SCREENTOGIF_DESKTOP_SMOKE=1 \
SCREENTOGIF_DESKTOP_SCREENSHOT_DIR="$evidence" \
dotnet test ScreenToGif.Linux.Tests/ScreenToGif.Linux.Tests.csproj --no-restore \
    --filter FullyQualifiedName~DesktopWindowSmokeTests \
    --logger 'trx;LogFileName=desktop.trx' --results-directory "$evidence"

python3 ScreenToGif.Linux/scripts/check-desktop-entrypoints.py \
    "$project_root/ScreenToGif.Linux/bin/Debug/net9.0/ScreenToGif.Linux" "$evidence"

mkdir -p "$scratch/webcam-config" "$evidence/webcam-controlled"
XDG_CONFIG_HOME="$scratch/webcam-config" \
SCREENTOGIF_WEBCAM_SMOKE=1 \
SCREENTOGIF_WEBCAM_SCREENSHOT_DIR="$evidence/webcam-controlled" \
dotnet test ScreenToGif.Linux.Tests/ScreenToGif.Linux.Tests.csproj --no-build --no-restore \
    --filter FullyQualifiedName~WebcamWindowSmokeTests \
    --logger 'trx;LogFileName=webcam.trx' --results-directory "$evidence"
