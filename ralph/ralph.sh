#!/usr/bin/env bash
set -euo pipefail

mode="sandbox"
if [ "${1:-}" = "--local" ]; then
  mode="local"
  shift
fi

iterations="${1:-10}"
root="$(cd "$(dirname "$0")/.." && pwd)"
state="$HOME/.nexora-ralph"
logs="$state/logs"
image="nexora-ralph"
sandbox="nexora-ralph"

finish() {
  if [ "$mode" = "sandbox" ]; then
    sbx stop "$sandbox" > /dev/null 2>&1 || true
  fi
  read -rp "=== Press Enter to close." _ || true
}

trap finish EXIT

verify='dotnet build Nexora.slnx -v q -nologo && dotnet test Nexora.slnx -v q -nologo --logger "console;verbosity=minimal"'
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
todo_list='to_entries[] | select(.value.state == "todo") | "\(.key). \(.value.description)\n" + (.value.steps | map("   - \(.)\n") | join(""))'
context='
(map(select(.type == "assistant" and .parent_tool_use_id == null)) | last) as $last
| (map(select(.type == "result")) | last) as $result
| select($last != null and $result != null)
| $result.modelUsage[$last.message.model].contextWindow as $window
| select($window != null)
| ($last.message.usage
    | .input_tokens + .cache_creation_input_tokens + .cache_read_input_tokens + .output_tokens) as $used
| "=== Context: \($used) / \($window) tokens (\($used * 1000 / $window | round / 10)%)\n"'

remaining() {
  if [ -f feature-list.json ]; then
    jq "$todo_count" < feature-list.json
  else
    echo 0
  fi
}

prompt() {
  cat PROMPT.md
  printf '\n## Checks\n\nThey %s. The end of their output:\n\n```\n%s\n```\n' "$verdict" "$(tail -n 60 <<< "$checks")"
  printf '\n## Todo entries\n\nNumbers are indexes in `feature-list.json`.\n\n%s\n' "$(jq -rj "$todo_list" < feature-list.json)"
}

in_sandbox() {
  sbx exec "$sandbox" "$@"
}

run_sandbox() {
  in_sandbox claude "$@" --dangerously-skip-permissions --append-system-prompt "$(cat "$HOME/.claude/CLAUDE.md")"
}

in_local() {
  "$@"
}

run_local() {
  claude "$@" --permission-mode auto
}

stop() {
  trap - INT TERM
  echo
  echo "=== Interrupted."
  exit 130
}

if ! command -v jq > /dev/null; then
  echo "Install jq first."
  exit 1
fi

if [ "$mode" = "sandbox" ]; then
  if ! sbx secret ls 2> /dev/null | grep -q anthropic; then
    echo "Sign in to Claude for sandboxes first: sbx run claude -- auth login"
    exit 1
  fi

  echo "=== Preparing sandbox $sandbox."
  mkdir -p "$state/nuget" "$state/artifacts"
  docker build -q --provenance=false -t "$image" - < ralph/Dockerfile > /dev/null
  docker save -o "$state/image.tar" "$image"
  sbx template load "$state/image.tar" > /dev/null
  rm "$state/image.tar"
  sbx rm -f "$sandbox" > /dev/null 2>&1 || true
  sbx create --no-progress --name "$sandbox" --pull never -t "$image" \
    -e TZ="$(timedatectl show -p Timezone --value)" \
    -e GIT_AUTHOR_NAME="$(git config user.name)" \
    -e GIT_AUTHOR_EMAIL="$(git config user.email)" \
    -e GIT_COMMITTER_NAME="$(git config user.name)" \
    -e GIT_COMMITTER_EMAIL="$(git config user.email)" \
    claude "$root" \
    "$state/nuget:/home/agent/.nuget" \
    "$state/artifacts:/home/agent/artifacts" \
    > /dev/null
  sbx policy allow network --sandbox "$sandbox" api.nuget.org > /dev/null
fi

trap stop INT TERM

for ((i = 1; i <= iterations; i++)); do
  todo="$(remaining)"
  if [ "$todo" -eq 0 ]; then
    echo "=== No todo entries left after $((i - 1)) iterations."
    exit 0
  fi

  log="$logs/$(date +%Y%m%d-%H%M%S)-$i.log"
  echo "=== Ralph iteration $i/$iterations ($mode), $todo todo entries (log: $log)"

  if checks="$("in_$mode" sh -c "$verify" < /dev/null 2>&1 | tr -d '\r')"; then
    verdict="passed"
  else
    verdict="failed"
  fi
  echo "=== Checks $verdict."

  "run_$mode" -p "$(prompt)" \
      --disallowedTools "Bash(git push:*)" \
      --model claude-opus-5-5 \
      --effort medium \
      --no-session-persistence \
      --verbose \
      --output-format stream-json \
    < /dev/null 2>&1 \
    | grep --line-buffered '^{' \
    | tee "$log" \
    | jq --unbuffered -rj "$stream"

  jq -rjs "$context" < "$log"
done

echo "=== Reached $iterations iterations, $(remaining) todo entries left."
