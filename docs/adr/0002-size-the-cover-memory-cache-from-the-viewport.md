# Size the in-memory cover cache from the viewport

The app must behave the same with 100 and 10000 tracks, so cover memory can't grow with library size. Covers are decoded
at exactly the pixel size they are shown at, and the in-memory cache is limited by a byte budget derived from how many
covers fit in the visible area times a few screens, so it holds what scrolling needs and nothing that depends on the
library or the machine.

## Considered Options

- A fixed number of covers: fast scrolling through a large library exceeds any count, and memory per entry depends on
  the cover size.
- A fixed byte constant: doesn't adapt to the window size.
- A budget relative to the machine's total RAM: how many covers are needed depends on the screen, not on RAM.
- A user setting for the cache size: a knob that hides the problem instead of solving it.
