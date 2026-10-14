#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 /absolute/path/to/ScreenToGif.Linux" >&2
    exit 64
fi

app_path=$1

if [ ! -x "$app_path" ]; then
    echo "The application executable must exist and be executable: $app_path" >&2
    exit 66
fi

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
applications_dir="$data_dir/applications"
icons_dir="$data_dir/icons/hicolor/256x256/apps"
desktop_file="$applications_dir/screentogif.desktop"

if [[ "$app_path" == *$'\n'* || "$app_path" == *$'\r'* || "$app_path" == *"="* || "$app_path" == *"%"* ]]; then
    echo "The application path cannot contain a newline, equals sign, or percent sign." >&2
    exit 65
fi

desktop_app_path=
for ((index = 0; index < ${#app_path}; index++)); do
    character=${app_path:index:1}
    case "$character" in
        \\) desktop_app_path+='\\\\' ;;
        \") desktop_app_path+='\\"' ;;
        \`) desktop_app_path+='\\`' ;;
        \$) desktop_app_path+='\\$' ;;
        *) desktop_app_path+="$character" ;;
    esac
done

mkdir -p "$applications_dir" "$icons_dir"
icon_file="$icons_dir/screentogif.png"
install -m 644 "$script_dir/../Resources/screentogif.png" "$icon_file"
while IFS= read -r line; do
    case "$line" in
        Exec=*) printf 'Exec="%s"\n' "$desktop_app_path" ;;
        Icon=*) printf 'Icon=%s\n' "$icon_file" ;;
        *) printf '%s\n' "$line" ;;
    esac
done < "$script_dir/../screentogif.desktop" > "$desktop_file"
rm -f "$applications_dir/ScreenToGif.Linux.desktop"

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$applications_dir"
fi

echo "Installed ScreenToGif's desktop entry and icon for $app_path"
