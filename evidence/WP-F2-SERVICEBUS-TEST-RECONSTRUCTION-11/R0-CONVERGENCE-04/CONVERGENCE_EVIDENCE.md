# R0 convergence 04 — R0-C03-01 and R0-C03-02

Under `DIR-A0071-R0-CONVERGENCE-04`. No candidate commit hash appears here; the exchange record
published after the tree is final binds commit and tree. Evidence bound by record `0011` is untouched.

## R0-C03-01 — mappings that actually prove what they claim

Six executable obligations were **strengthened** so their own assertion states the mapped behaviour.
No executable row was created; the full consistency table is `PROOF_MAPPING_TABLE.md`.

| Behaviour | Now carried by | Strengthening |
|---|---|---|
| ordinal ordering of missing keys, no secret in the same diagnostic | `OBL-R0-SQL-0052` | names compared as an ordered sequence, message asserted not to contain the password |
| PostgreSQL connection string from typed options | `OBL-R0-SQL-0025` | effective connection string read back and compared field by field; its intent previously said it asserted nothing |
| SQL Server connection string with initial catalog | `OBL-R0-SQL-0040` | Initial Catalog asserted beside the already asserted Data Source |
| SQL Server connection string without initial catalog | `OBL-R0-SQL-0127` | absence of Initial Catalog asserted explicitly |
| no fixed-name compatibility API on the shipped surface | `OBL-R0-PER-0307` | the architecture rule inspects the assembly API surface for the removed name |

`OBL-R0-SQL-0048` and `0054` are no longer cited for connection-string construction: they assert
the value object and dialect resolution, which is not the same claim.

## One behaviour is open, not attached

The EF Core half of the run-scoped naming property has **no cohesive carrier**. Every EF Core
obligation is a saga, outbox or transaction obligation; none asserts the database name its fixture
uses. Attaching the property to one of them would create exactly the mixed test that was rejected,
so the entry is recorded with `status: BLOCKED_NO_COHESIVE_CARRIER` and travels as a QUESTION.

## R0-C03-02 — status corrected

`.testagent/status.md` names this convergence as its current state and effective artefact link.
Earlier directories appear only as bound historical evidence.

## Invariants re-measured

| Invariant | Required | Measured |
|---|---:|---:|
| executable obligation/variant entries | 3663 | 3663 |
| executable target projects | 41 | 41 |
| UnitArchitecture / LocalIntegration / External | 2762 / 773 / 128 | 2762 / 773 / 128 |
| anchor identities backed | 3114 | 3114 |
| duplicate pairs / trailing whitespace | 0 / 0 | 0 / 0 |
