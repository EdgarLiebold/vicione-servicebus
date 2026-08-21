# R0 convergence — findings F-02 through F-09

Produced under `DIR-A0071-R0-SECOND-REVIEW-02` and reslice `0008` (Development Slice revision 0002,
SHA-256 `b74f634971dcb2071b36b5d90f2db33b0008a9b14873b4e366e5e0a6eec66048`).

**This file deliberately contains no candidate commit hash.** A file cannot carry the identity of the
commit that contains it; the exchange record published after the tree is final binds commit and tree.

## Why a new directory

`R0-CORRECTION-01` is bound byte-exact by exchange record `0005`. Correcting it in place is what
invalidated the earlier envelope. The effective artefacts therefore live here, and the bound
directories keep their bytes — verified: both files restored to the hashes record `0005` binds.

## Finding by finding

| Finding | Correction | Proof |
|---|---|---|
| **F-02** four writers on one Core target | Merged into one `CO-CORE` cohort, one writer `W-CORE`, one project. The four census areas remain read-only analysis lanes | 41 cohorts, 41 write areas, 0 shared |
| **F-03** false-green Core gates | One unfiltered project gate over the complete 1,805-obligation projection; traits are diagnosis only and never own completeness | plan section 3, `CO-CORE` gate command |
| **F-04** Event Hubs zero-population Unit target | Removed from `targetTestProjects` and `profileByTargetProject`; retained as `reservedTargetProject` with `created=false`, `measuredPopulation=0`, profile `UnitArchitecture` and a factual rationale | 0 active targets with zero obligations |
| **F-05** `OBL-R0-SML-0271` open while closure was claimed | Replaced by run-scoped, collision-free database naming derived from the run identity inside the owning SQL and EF fixtures; `SHARED_CAPABILITY` + `PROPOSED_REPLACED_EXECUTING`, no executable profile of its own, no fixed-name compatibility API | 0 reclassified rows contradict their own kind |
| **F-06** shared capabilities without proving owners | `provenBy` set for `OBL-R0-SML-0268`–`0271` and for every other shared capability: benchmark rows to the benchmark-correctness project, the sixteen derived future cases to the four consuming projects. `DIAGNOSTIC_ONLY` rows are exempt because section 9 states they never prove behaviour | 0 shared capabilities without proving owners |
| **F-07** stale status and evidence | `status.md` states current facts only and lists the four earlier commits as unaccepted history; this file carries no self-referencing hash | `git status` clean at the candidate |
| **F-08** whitespace conflict | One exact-path `.gitattributes` rule for the immutable superseded list, with the reason in an adjacent comment | both directions measured, below |
| **F-09** three Microsoft-skill constraints | Recorded as plan section 3b: mandatory `code-testing-agent` entry, skill paired with the .NET extension and recorded before the first edit, immediate solution registration with proven discovery, source-parallel folders and namespaces | plan section 3b |

## F-08 measured, both directions

| Probe | Result |
|---|---|
| Full baseline range **with** the rule | **0** whitespace complaints |
| Same range **without** the rule (rule removed, then restored byte-identically) | **1086** complaints |
| Seeded trailing space in the **corrected** list `R0-CONVERGENCE-02/OBLIGATION_SET_FROZEN.tsv` | **flagged** |
| Seeded trailing space in a normal C# product file | **flagged**, and the file restored byte-identically |

The exemption is one exact path, not a wildcard. The file it names is immutable by contract, so no
future file can inherit it, and every current source, test, build and corrected evidence file keeps
the normal rules.

## Path deviation, declared

The directive's example rule reads `tests2/.reconstruction/R0/OBLIGATION_SET_FROZEN.tsv`. That path
matches nothing: `tests2/` does not exist yet, and the immutable bytes record `0002` binds live at
`evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/OBLIGATION_SET_FROZEN.tsv`. The mandated
**mechanism** — one documented exact-path `whitespace=-blank-at-eol` rule — is applied unchanged to
the path that actually carries those bytes. Copying the example verbatim would have produced a rule
that matches nothing and a range that stays red, which would be a closure claim contradicting the
evidence. If a different path was intended, one line corrects it.

## Target state

| Value | Count |
|---|---:|
| executable obligation/variant entries | 3663 |
| executable target projects | 41 |
| cohorts | 41 |
| `UnitArchitecture` / `LocalIntegration` / `External` | 2762 / 773 / 128 |
| ledger rows | 3664 |
| obligations / shared capabilities / infrastructure / assurance | 3358 / 41 / 149 / 116 |
