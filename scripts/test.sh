#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if [ "${1:-}" = "--exclude-slow" ]; then
    shift
    set -- --filter 'Category!=Slow' "$@"
fi

exec dotnet test Nexora.slnx "$@"
