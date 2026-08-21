# Status — native test reconstruction

## Current state

R0 is accepted and remotely preserved. The first frozen F1a correction was rejected by independent
review because a VSTest logger still contaminated product package graphs and the dynamic run was not
bound to the frozen commit. The final Lead correction is frozen at technical commit
`99c7e5a373bc3e56861e302675f8cc5fc34cb31d`, tree
`e7e055d3b889a6be53baaf3dd0136aa79a1e5dfc`. Full locked restore, Release build, formatting and the
unfiltered native 89-case run are green. Two independent targeted delta reviews closed the package
policy and configuration-default findings with PASS; F1a is accepted and remotely preserved.

No product behavior under `src/**` is changed. The native xUnit/MTP basis is retained and corrected.
The Lead-owned F1b technical candidate is frozen at commit
`87dd0fa3682dbfdc0fe1bf626d72747948eb0be1`, tree
`e4e375303c0017cae1c10c1dd0dbea16c5ba3475`; all positive gates and six isolated mutations are
complete. F1b awaits its two independent read-only reviews. All behavior-cohort migration remains
stopped.

| Wave | State |
|---|---|
| R0 analysis and obligation inventory | complete and remotely preserved |
| F1a native foundation | accepted and remotely preserved at technical commit `99c7e5a3` |
| F1b native requirement binding | frozen; independent review pending |
| C1 hermetic core | not started |
| C2 local persistence | not started |
| C3 local brokers | not started |
| C4 external | not started |
| inherited-stack removal and promotion | not started |

## Next work

- preserve the accepted F1a technical commit and its evidence without modification;
- independently review and accept or reject the bounded six-entry F1b architecture reference cohort;
- keep behavior-cohort migration, inherited-test deletion and profile promotion stopped until their
  own accepted slices.

Real cloud access remains a later C4 execution dependency. It does not weaken F1a and is not
represented as passed.
