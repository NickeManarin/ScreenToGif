#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 3 || "$1" != "--output" ]]; then
    echo "Usage: $0 --output <capture.png> <executable> [arguments...]" >&2
    exit 64
fi

output=$2
shift 2

if ! command -v xvfb-run >/dev/null || ! command -v xdotool >/dev/null || ! command -v import >/dev/null; then
    echo "xvfb-run, xdotool, and ImageMagick import are required." >&2
    exit 69
fi

config_dir=$(mktemp -d)
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
            import -window "$window_id" "$OUTPUT_PATH"
            exit 0
        fi
        sleep 0.1
    done

    echo "Timed out waiting for a visible application window." >&2
    exit 70
' capture-window "$config_dir" "$@"
