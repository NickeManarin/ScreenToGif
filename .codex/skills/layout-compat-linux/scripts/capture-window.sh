#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 3 || "$1" != "--output" ]]; then
    echo "Usage: $0 --output <capture.png> <executable> [arguments...]" >&2
    exit 64
fi

output=$2
shift 2

if ! command -v xvfb-run >/dev/null || ! command -v xdotool >/dev/null || ! command -v xwininfo >/dev/null || ! command -v import >/dev/null; then
    echo "xvfb-run, xdotool, xwininfo, and ImageMagick import are required." >&2
    exit 69
fi

config_dir=$(mktemp -d)
cleanup() {
    rm -rf -- "$config_dir"
}
trap cleanup EXIT
export OUTPUT_PATH=$output

xvfb-run -a -s "-screen 0 1600x1000x24" bash -c '
    export XDG_CONFIG_HOME="$1"
    shift
    "$@" &
    app_pid=$!
    trap "kill $app_pid 2>/dev/null || true; wait $app_pid 2>/dev/null || true" EXIT

    for _ in $(seq 1 50); do
        window_id=$(xdotool search --onlyvisible --pid "$app_pid" 2>/dev/null | head -n 1 || true)
        if [[ -n "$window_id" ]]; then
            # A mapped Avalonia window can be discoverable before its first frame is painted.
            sleep 0.5
            geometry=$(xwininfo -id "$window_id")
            x=$(awk "/Absolute upper-left X:/ { print \$4 }" <<<"$geometry")
            y=$(awk "/Absolute upper-left Y:/ { print \$4 }" <<<"$geometry")
            width=$(awk "/Width:/ { print \$2; exit }" <<<"$geometry")
            height=$(awk "/Height:/ { print \$2; exit }" <<<"$geometry")
            import -window root -crop "${width}x${height}+${x}+${y}" +repage "$OUTPUT_PATH"
            exit 0
        fi
        sleep 0.1
    done

    echo "Timed out waiting for a visible application window." >&2
    exit 70
' capture-window "$config_dir" "$@"
