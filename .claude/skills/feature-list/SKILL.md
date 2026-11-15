---
name: feature-list
description: "Grill the user about a feature and append its requirements to feature-list.json."
argument-hint: "<feature description>"
disable-model-invocation: true
---

Feature: $ARGUMENTS

If no feature is given, ask for it first.

1. Read the code this feature touches and the existing `feature-list.json` at the repo root.
   Don't ask what the code or existing entries already answer.
2. Interview the user relentlessly with `AskUserQuestion`
   until no ambiguity or open questions about the feature are left.
3. Summarize the feature in a few sentences, including what's out of scope, and ask the user to confirm.
   If they don't, go back to step 2.
4. Break it into entries: one behavior each, checkable on its own, ordered by dependency,
   with edge cases as separate entries. Skip behaviors already listed.
   Show conflicts with existing entries to the user. Show the list and adjust it until the user approves.
5. Append the entries to the JSON array in `feature-list.json` (create it if missing), 4-space indent.
   Don’t change or remove existing entries.
6. Commit the changes to the current branch.

Entry format:

```JSON
{
    "description": "Clicking the track cover in the library starts playback",
    "steps": [
        "Navigate to the Library tab",
        "Find the first track in the list",
        "Click in the center of its cover",
        "Verify playback has started"
    ],
    "state": "todo"
}
```
