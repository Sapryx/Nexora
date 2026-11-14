#!/usr/bin/env bash
set -euo pipefail

iterations="${1:-10}"
root="$(cd "$(dirname "$0")/.." && pwd)"
logs="$root/ralph/logs"
image="nexora-ralph"
mkdir -p "$logs"
cd "$root"

stream='select(.type == "assistant").message.content[]?
  | if .type == "text" then .text + "\n\n"
    elif .type == "tool_use" then "> \(.name) \(.input.command // .input.file_path // .input.pattern // .input.skill // "")\n"
    else empty end'
todo_count='[.[] | select(.state == "todo")] | length'

jq() {
  docker run --rm -i "$image" jq "$@"
}

remaining() {
  if [ -f feature-list.json ]; then
    jq "$todo_count" < feature-list.json
  else
    echo 0
  fi
}

if [ -z "${CLAUDE_CODE_OAUTH_TOKEN:-}" ]; then
  echo "Set CLAUDE_CODE_OAUTH_TOKEN first (get one with: claude setup-token)."
  exit 1
fi

docker build -q -t "$image" - < ralph/Dockerfile > /dev/null

for ((i = 1; i <= iterations; i++)); do
  todo="$(remaining)"
  if [ "$todo" -eq 0 ]; then
    echo "=== No todo entries left after $((i - 1)) iterations."
    exit 0
  fi

  log="$logs/$(date +%Y%m%d-%H%M%S)-$i.log"
  echo "=== Ralph iteration $i/$iterations, $todo todo entries (log: $log)"

  MSYS_NO_PATHCONV=1 docker run --rm -i \
    -v "$(pwd -W):/work" \
    -v "$HOME/.claude/CLAUDE.md:/home/agent/.claude/CLAUDE.md:ro" \
    -v nexora-ralph-nuget:/home/agent/.nuget/packages \
    -v nexora-ralph-artifacts:/home/agent/artifacts \
    -e CLAUDE_CODE_OAUTH_TOKEN \
    -e GIT_AUTHOR_NAME="$(git config user.name)" \
    -e GIT_AUTHOR_EMAIL="$(git config user.email)" \
    -e GIT_COMMITTER_NAME="$(git config user.name)" \
    -e GIT_COMMITTER_EMAIL="$(git config user.email)" \
    "$image" \
    claude -p "$(cat PROMPT.md)" \
      --dangerously-skip-permissions \
      --disallowedTools "Bash(git push:*)" \
      --no-session-persistence \
      --verbose \
      --output-format stream-json \
    < /dev/null 2>&1 \
    | grep --line-buffered '^{' \
    | tee "$log" \
    | jq --unbuffered -rj "$stream"
done

echo "=== Reached $iterations iterations, $(remaining) todo entries left."
