# State-machine visualizer mutation validation

The candidate at `6cf7c2ef` was exercised in a disposable detached worktree. Each source mutation
was applied alone, the owning project was rebuilt in `Release`, and all eight native MTP cases were
run unfiltered. The profile-count guard was exercised separately against the unchanged candidate.
No mutation was retained.

| Mutation | Expected owner | Result |
|---|---|---|
| Render ordinary Graphviz event vertices as diamonds instead of rectangles | exact Graphviz contract | killed: 7 passed, 1 failed, exit 2 |
| Render Mermaid generic-event types with `< >` instead of `« »` | exact Mermaid contract | killed: 7 passed, 1 failed, exit 2 |
| Retain outgoing edges from composite-event target states | composite-event graph filter | killed: 7 passed, 1 failed, exit 2 |
| Remove the request-derived requirement-projection row | requirement projection | killed: 7 passed, 1 failed, exit 2 |
| Require 464 cases from the unchanged 463-case UnitArchitecture profile | MTP minimum-count guard | killed: 463 passed, policy exit 9 |

The count probe demonstrates only that the native MTP process enforces its predeclared floor. The
durable completeness owners remain the exact solution graph and each assembly's compiled
requirement projection; the count is deliberately not treated as a substitute for either.
