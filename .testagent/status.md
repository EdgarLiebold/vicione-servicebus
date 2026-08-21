# Status — native test reconstruction

## Current state

R0 is accepted and remotely preserved. The first frozen F1a correction was rejected by independent
review because a VSTest logger still contaminated product package graphs and the dynamic run was not
bound to the frozen commit. The Lead correction is in progress; no F1a acceptance is claimed.

No product behavior under `src/**` is changed. The native xUnit/MTP basis is retained and corrected;
F1b and all behavior-cohort migration remain stopped.

| Wave | State |
|---|---|
| R0 analysis and obligation inventory | complete and remotely preserved |
| F1a native foundation | correction and clean-commit replay in progress |
| F1b coverage and cohort infrastructure | not started |
| C1 hermetic core | not started |
| C2 local persistence | not started |
| C3 local brokers | not started |
| C4 external | not started |
| inherited-stack removal and promotion | not started |

## Acceptance still required

- clean technical commit and full Lead-owned restore/build/test replay;
- evidence bound to that commit and tree;
- two independent read-only reviews of the corrected frozen commit.

Real cloud access remains a later C4 execution dependency. It does not weaken F1a and is not
represented as passed.
