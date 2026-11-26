#!/usr/bin/env bash
set -euo pipefail

source "$(dirname "${BASH_SOURCE[0]}")/sandbox.sh"

stream='
if .type == "item.started" and .item.type == "command_execution" then
  command_line("Bash"; .item.command)
elif .type == "item.completed" then
  .item
  | if .type == "agent_message" then "\n" + .text + "\n\n"
    elif .type == "command_execution" then
      (if .exit_code == 0 then color("2") else color("31") end) as $style
      | (.aggregated_output // "") | render_output($style)
    else empty end
elif .type == "error" then
  color("31") + .message + reset + "\n"
elif .type == "turn.failed" then
  color("31") + .error.message + reset + "\n"
else empty end'

context='
(map(select(.type == "turn.completed")) | last)
| select(. != null)
| .usage
| "=== Usage: \(.input_tokens) input tokens (\(.cached_input_tokens) cached), \(.output_tokens) output tokens\n"'

run_agent() {
  sbx exec "$1" codex -a never exec \
    --sandbox danger-full-access \
    --ephemeral \
    --json \
    -c model_reasoning_effort='"medium"' \
    "$2"
}

run_ralph codex openai 'sbx secret set openai --oauth' nexora-ralph-codex \
  "$HOME/.agents/skills:/home/agent/.agents/skills:ro" "$@"
