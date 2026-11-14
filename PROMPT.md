You work autonomously. Nobody will answer your questions: use your own judgment to make decisions.

1. Read `progress.txt` and run `git log --oneline -n 20`. Run `git status`, the debug build and the unit tests.
   If the tree is dirty or the build or tests fail, fixing that becomes your task.
2. Read `feature-list.json` and pick one entry with `"state": "todo"`
   that has the biggest priority (it's not necessarily the first one).
3. Implement the task using the `/implement` skill. Work on one task only.
4. Mark the task as `"state": "done"` in `feature-list.json`.
5. Append the progress you've made to the `progress.txt` file.
   State what the task was and what you did to complete it. Be concise.
6. Make a commit with a descriptive name to the current branch.

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
