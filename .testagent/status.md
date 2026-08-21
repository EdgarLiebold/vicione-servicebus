# Status — native test reconstruction

## Current state

R0 is accepted and unchanged. The Product-Owner-authorized Lead correction of F1a is implemented and
all local technical gates are green after the first Team-1 candidate was rejected for repository-wide
restore failure, false-green rules, stale documentation, fail-open configuration, and a namespace
collision.

No product behavior under `src/**` is changed. The native xUnit/MTP basis is retained and corrected;
F1b and all behavior-cohort migration remain stopped.

| Wave | State |
|---|---|
| R0 analysis and obligation inventory | complete and remotely preserved |
| F1a native foundation | technically green; awaiting two independent reviews and Lead acceptance |
| F1b coverage and cohort infrastructure | not started |
| C1 hermetic core | not started |
| C2 local persistence | not started |
| C3 local brokers | not started |
| C4 external | not started |
| inherited-stack removal and promotion | not started |

## Acceptance still required

- two independent read-only reviews of the frozen correction commit.

Real cloud access remains a later C4 execution dependency. It does not weaken F1a and is not
represented as passed.
