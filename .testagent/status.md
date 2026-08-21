# Status — native test reconstruction

## Current state

R0 is accepted and remotely preserved. The first frozen F1a correction was rejected by independent
review because a VSTest logger still contaminated product package graphs and the dynamic run was not
bound to the frozen commit. The Lead correction is frozen at technical commit
`5b47c9ba9b9e30de0d48d268ef65856d63a6f724`, tree
`781e7b8f805036e4637cc69fe9543bdfa22dee32`. Full locked restore, Release build, formatting and the
unfiltered native 86-case run are green; formal F1a acceptance is not claimed before evidence review.

No product behavior under `src/**` is changed. The native xUnit/MTP basis is retained and corrected;
F1b and all behavior-cohort migration remain stopped.

| Wave | State |
|---|---|
| R0 analysis and obligation inventory | complete and remotely preserved |
| F1a native foundation | technical freeze green; evidence and independent reviews pending |
| F1b coverage and cohort infrastructure | not started |
| C1 hermetic core | not started |
| C2 local persistence | not started |
| C3 local brokers | not started |
| C4 external | not started |
| inherited-stack removal and promotion | not started |

## Acceptance still required

- evidence commit binding the completed Lead-owned restore/build/test replay;
- two independent read-only reviews of the corrected frozen commit.

Real cloud access remains a later C4 execution dependency. It does not weaken F1a and is not
represented as passed.
