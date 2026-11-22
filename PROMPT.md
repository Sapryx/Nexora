You work autonomously. Nobody will answer your questions: use your own judgment to make decisions.

1. Read `progress.txt` and run `git log --oneline -n 20` and `git status`.
   The debug build and the unit tests have already run; their result is in "Checks" below.
   If the tree is dirty or the checks failed, fixing that becomes your task.
2. Pick one entry from "Todo entries" below that you consider the most important right now
   (it's not necessarily the first one).
   If the behavior already exists, extend or add a UI test that checks every step of the entry, then mark it done.
3. State what you are going to be working on.
4. Implement the task using the `/implement` skill. Work on one task only.
5. Test the behavior end-to-end with a UI test in `tests/Nexora.UiTests` and check its screenshot.
6. Mark the task as `"state": "done"` in `feature-list.json`.
7. Append the progress you've made to the `progress.txt` file.
   State what the task was and what you did to complete it. Be concise.
8. Make a commit with a descriptive name to the current branch.
9. Provide feedback: what took you a lot of time, tokens or tool calls and how it could be improved,
   for examples, by creating a skill, a script/tool and. What information would be good having at the start (if any).

If you encounter any blockers that prevent you from completing the task, mark it with `"state": "blocked"`
and describe the problem in `progress.txt`.

You are only allowed to change the `"state"` field in `feature-list.json`. Editing or deleting tasks is forbidden.

`progress.txt` entry format:

```
Date: <YYYY-MM-DD | HH:mm:SS>
Task: <Task description>
Result: <What was done>
Verification: <How the work was verified>
```

Get the date and time by running `date '+%Y-%m-%d | %H:%M:%S'`, never write them yourself.
