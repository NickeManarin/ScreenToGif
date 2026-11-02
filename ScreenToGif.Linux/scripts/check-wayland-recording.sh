#!/usr/bin/env bash
set -euo pipefail

project_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
evidence=${1:?Usage: check-wayland-recording.sh /tmp/evidence-directory}
mkdir -p "$evidence"
evidence=$(cd "$evidence" && pwd)
[[ ${XDG_SESSION_TYPE:-} == wayland ]] || { echo "A native Wayland desktop is required." >&2; exit 1; }
lock_state=$(gdbus call --session --dest org.gnome.ScreenSaver \
    --object-path /org/gnome/ScreenSaver --method org.gnome.ScreenSaver.GetActive)
[[ $lock_state == '(false,)' ]] || { echo "Unlock the GNOME desktop before this check." >&2; exit 1; }
config=$(mktemp -d /tmp/screentogif-wayland-config-XXXXXX)
trap 'rm -rf "$config"' EXIT

if [[ ${SCREENTOGIF_WAYLAND_REUSE:-} == 1 ]]; then
    echo 'Cancel the first administrator dialog; approve the second. Share the test source once, then do not approve another picker. After Forget, cancel selection; approve administrator cleanup if requested.'
elif [[ ${SCREENTOGIF_WAYLAND_LIFECYCLE:-} == 1 ]]; then
    echo 'Cancel the FIRST sharing dialog. The next closes automatically. Then share only ScreenToGif Wayland test source twice.'
else
    echo 'In the desktop sharing dialog choose only the window: ScreenToGif Wayland test source'
fi
if [[ ${SCREENTOGIF_WAYLAND_CURSOR_PROBE:-} == 1 ]]; then
    echo 'After Share, move the pointer to the CENTER of the RED square and leave it there.'
fi
echo "Evidence: $evidence"
cd "$project_root"
XDG_CONFIG_HOME="$config" \
SCREENTOGIF_WAYLAND_RECORDING_SMOKE=1 \
SCREENTOGIF_WAYLAND_SOURCE_SCRIPT="$project_root/ScreenToGif.Linux/scripts/wayland-test-source.py" \
SCREENTOGIF_DESKTOP_SCREENSHOT_DIR="$evidence" \
dotnet test ScreenToGif.Linux.Tests/ScreenToGif.Linux.Tests.csproj --no-restore \
    --filter FullyQualifiedName~Native_Wayland_source_records_into_editor \
    --logger 'trx;LogFileName=wayland.trx' --results-directory "$evidence"
