#!/usr/bin/env bash
set -euo pipefail

iterations="${1:-10}"
root="$(cd "$(dirname "$0")/.." && pwd)"
logs="$root/ralph/logs"
image="nexora-ralph"
container="nexora-ralph-$$"
mkdir -p "$logs"
cd "$root"

stream='
def color($code): "\u001b[\($code)m";
def reset: color("0");
def clip: if length > 160 then .[:160] + "..." else . end;
def nonblank_lines: gsub("\r"; "") | split("\n") | map(select(test("\\S")));
def call_summary: .input.command // .input.file_path // .input.pattern // .input.skill // .input.description // "" | tostring;
def result_text: if (.content | type) == "string" then .content else [.content[]? | select(.type == "text").text] | join("\n") end;

if .type == "assistant" then
  .message.content[]?
  | if .type == "text" then "\n" + .text + "\n\n"
    elif .type == "tool_use" then
      (call_summary | nonblank_lines) as $lines
      | color("1;36") + "> " + .name + reset + " " + color("36") + ($lines[0] // "" | clip)
        + (if ($lines | length) > 1 then color("2") + " (+\($lines | length - 1) lines)" else "" end) + reset + "\n"
    else empty end
elif .type == "user" then
  .message.content[]?
  | select(.type == "tool_result")
  | (if .is_error then color("31") else color("2") end) as $style
  | (result_text | nonblank_lines) as $lines
  | ($lines[:3] | map($style + "  " + clip + reset + "\n") | join(""))
    + (if ($lines | length) > 3 then color("2") + "  ... \($lines | length - 3) more lines" + reset + "\n" else "" end)
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

stop() {
  trap - INT TERM
  echo
  echo "=== Interrupted, stopping $container."
  docker stop "$container" > /dev/null 2>&1 || true
  exit 130
}

trap stop INT TERM

docker build -q -t "$image" - < ralph/Dockerfile > /dev/null

for ((i = 1; i <= iterations; i++)); do
  todo="$(remaining)"
  if [ "$todo" -eq 0 ]; then
    echo "=== No todo entries left after $((i - 1)) iterations."
    exit 0
  fi

  log="$logs/$(date +%Y%m%d-%H%M%S)-$i.log"
  echo "=== Ralph iteration $i/$iterations, $todo todo entries (log: $log)"

  MSYS_NO_PATHCONV=1 docker run --rm -i --init --name "$container" \
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
