# Status — native test reconstruction

## Current state

R0 is accepted and remotely preserved. The first frozen F1a correction was rejected by independent
review because a VSTest logger still contaminated product package graphs and the dynamic run was not
bound to the frozen commit. The final Lead correction is frozen at technical commit
`99c7e5a373bc3e56861e302675f8cc5fc34cb31d`, tree
`e7e055d3b889a6be53baaf3dd0136aa79a1e5dfc`. Full locked restore, Release build, formatting and the
unfiltered native 89-case run are green. Two independent targeted delta reviews closed the package
policy and configuration-default findings with PASS; F1a is locally accepted.

No product behavior under `src/**` is changed. The native xUnit/MTP basis is retained and corrected;
F1b and all behavior-cohort migration remain stopped.

| Wave | State |
|---|---|
| R0 analysis and obligation inventory | complete and remotely preserved |
| F1a native foundation | accepted locally at technical commit `99c7e5a3` |
| F1b coverage and cohort infrastructure | not started |
| C1 hermetic core | not started |
| C2 local persistence | not started |
| C3 local brokers | not started |
| C4 external | not started |
| inherited-stack removal and promotion | not started |

## Next work

- preserve the accepted F1a technical commit and its evidence without modification;
- begin F1b only from a separately bounded Lead plan;
- keep behavior-cohort migration, inherited-test deletion and profile promotion stopped until their
  own accepted slices.

Real cloud access remains a later C4 execution dependency. It does not weaken F1a and is not
represented as passed.
