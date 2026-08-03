#!/usr/bin/env bash
# Usage: ./publish.sh [rid]
set -euo pipefail

rid="${1:-linux-x64}"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
publish_dir="$script_dir/../src/Nexora/bin/Release/net10.0/$rid/publish"
whitelist_dir="$script_dir"

case "$rid" in
    win*)   platform="windows" ;;
    linux*) platform="linux" ;;
    osx*)   platform="macos" ;;
    *)
        echo "Unrecognized RID '$rid': expected it to start with 'win', 'linux' or 'osx'." >&2
        exit 1
        ;;
esac

config_path="$whitelist_dir/$platform.conf"

if [ ! -f "$config_path" ]; then
    echo "No whitelist found for platform '$platform' at $config_path" >&2
    exit 1
fi

dotnet publish src/Nexora -c Release -r "$rid"

echo ""
echo "--- Trimming VLC plugins (RID: $rid, platform: $platform) ---"

if [ ! -d "$publish_dir" ]; then
    echo "Publish directory not found at $publish_dir, skipping plugin trim."
    exit 0
fi

delete_folders=()
declare -A keep_in_folder
current_section=""

while IFS= read -r line || [ -n "$line" ]; do
    line="$(echo "$line" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')"
    [ -z "$line" ] && continue
    [[ "$line" == \#* ]] && continue

    if [[ "$line" == "[DeleteFolders]" ]]; then
        current_section="delete"
        continue
    fi
    if [[ "$line" =~ ^\[KeepInFolder:(.+)\]$ ]]; then
        current_section="keep:${BASH_REMATCH[1]}"
        keep_in_folder["${BASH_REMATCH[1]}"]=""
        continue
    fi

    if [[ "$current_section" == "delete" ]]; then
        delete_folders+=("$line")
    elif [[ "$current_section" == keep:* ]]; then
        folder_name="${current_section#keep:}"
        keep_in_folder["$folder_name"]="${keep_in_folder[$folder_name]} $line"
    fi
done < "$config_path"

removed_folders=0
for folder_name in "${delete_folders[@]}"; do
    while IFS= read -r -d '' dir; do
        rm -rf "$dir"
        removed_folders=$((removed_folders + 1))
    done < <(find "$publish_dir" -type d -name "$folder_name" -print0)
done

removed_files=0
for folder_name in "${!keep_in_folder[@]}"; do
    keep_list="${keep_in_folder[$folder_name]}"
    while IFS= read -r -d '' dir; do
        while IFS= read -r -d '' file; do
            base_name="$(basename "$file")"
            base_name="${base_name%.*}"
            if [[ ! " $keep_list " =~ [[:space:]]${base_name}[[:space:]] ]]; then
                rm -f "$file"
                removed_files=$((removed_files + 1))
            fi
        done < <(find "$dir" -maxdepth 1 -type f -print0)
    done < <(find "$publish_dir" -type d -name "$folder_name" -print0)
done

find "$publish_dir" -type f \( -name "*.pdb" -o -name "*.lib" \) -delete
find "$publish_dir" -type d -empty -delete

final_size=$(du -sm "$publish_dir" | cut -f1)
echo "Removed $removed_folders plugin folder(s) and $removed_files curated file(s)."
echo "Publish complete: $publish_dir"
echo "Final size: ${final_size} MB"
