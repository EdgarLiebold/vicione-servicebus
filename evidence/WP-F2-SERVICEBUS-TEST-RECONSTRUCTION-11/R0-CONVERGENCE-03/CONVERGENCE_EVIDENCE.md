# R0 convergence 03 — findings R0-C02-01 to R0-C02-03 and the editorial corrections

Produced under `DIR-A0071-R0-CONVERGENCE-03`, on Development Slice revision `0002`.
This file carries no candidate commit hash: a file cannot hold the identity of the commit that
contains it. The exchange record published after the tree is final binds commit and tree.

The evidence bound by record `0009` under `R0-CONVERGENCE-02` is unchanged. This directory is the
effective result; the earlier ones remain as bound historical evidence.

## R0-C02-01 — F-05 is now represented, not only asserted

The ledger no longer says two things at once.

| Was | Now |
|---|---|
| `boundaries` of `OBL-R0-SML-0271` read `Target: none. Dropped.` | states the stability-within-a-run, difference-across-runs and no-fixed-name properties |
| `notes` began with `DROP` and said the Lead still owns the final word | states the final decision as decided: the shared fixed-name class disappears and each owning fixture derives its name from its run identity |
| `OBL-R0-SQL-0037`, `0038`, `0045`, `0046` froze a fixed database name as their variant | variant is `database name derived from the run identity`; `boundaries` and `assertionIntent` require the run-derived name and a second run identity producing a different one |

The fixed name occurs **zero** times in the ledger, in requirements and in explanatory text alike.
Row count, profiles and product semantics of those four obligations are unchanged; only the naming
requirement moved from fixed to run-scoped.

## R0-C02-02 — a behaviour-to-obligation mapping, not a project list

Each of `OBL-R0-SML-0268` … `0271` now carries a machine-readable `proofMapping`: a list of
`{behavior, provenByObligationIds}` binding every behaviour contract and negative path to exact
executable obligation identifiers. `provenBy` additionally names
`tests2/Transports/ViciOne.ServiceBus.SqlTransport.Tests`, which was missing although it holds the
executable configuration cases.

| Capability | Behaviours | Bound to |
|---|---|---|
| `0268` fail-closed reader | incomplete configuration refused before any connection with a distinct exception; unparsable value treated as missing and named; ordinal diagnostics that never print a secret | `OBL-R0-SQL-0051`, `0052`, `0053` |
| `0269` validated endpoint | complete typed values; missing part refused; port bounds 1..65535; description never reveals the password; provider connection string built from the typed endpoint per dialect | `OBL-R0-SQL-0048`, `0049`, `0050`, `0051`, `0054` |
| `0270` distinct configuration exception | a missing contract reads as a missing contract; fail-closed resolution for null or unrecognised configuration | `OBL-R0-SQL-0052`, `0054` |
| `0271` run-scoped naming | stability within one run; difference across two run identities; no fixed name and no compatibility API | `OBL-R0-SQL-0037`, `0038`, `0045`, `0046` |

No second verdict system is introduced and no executable row is added: `OBL-R0-SQL-0048` is
**strengthened** to state that the provider fixture builds its connection string from exactly those
typed values, which is what the mapping binds to it. Every referenced identifier was verified to
exist and to be an executable obligation.

## R0-C02-03 — the active plan states current facts

| Superseded statement | Replaced by |
|---|---|
| plan corrected only under the first directive; effective result under `R0-CORRECTION-01` | current state is the second review and this convergence, on revision `0002`, effective evidence `R0-CONVERGENCE-03` |
| `global.json` will be added later and is not edited | `global.json` is in scope and already carries the MTP selection beside its unchanged SDK pin |
| blocker `B-1` `global.json` write scope, owner Lead | removed from the blocker table with a one-line historical note |

A **closed search** over the complete active set — `plan.md`, `status.md`, `research.md` and the
effective ledger — for superseded counts, scopes, paths, blockers and mechanisms returns **0**
statements lacking an explicit historical marker.

## Editorial corrections

`.gitattributes`: the adjacent comment no longer calls `R0-CORRECTION-01` the superseding list; it
names the newest convergence directory. The exact-path rule itself is byte-unchanged.

Units, measured separately:

| Quantity | Value |
|---|---:|
| lines with trailing whitespace in the immutable list | **543** |
| whitespace **violations** reported without the rule | **543** |
| **diagnostic lines** `git diff --check` renders for them | **1086** |
| diagnostic lines with the rule in place | **0** |

Each violation is rendered as two lines — the message and the offending content. The earlier report
called 1086 "complaints", which conflated the two units.

## Invariants the Lead confirmed, re-measured after this correction

| Invariant | Required | Measured |
|---|---:|---:|
| executable obligation/variant entries | 3663 | 3663 |
| executable target projects | 41 | 41 |
| `UnitArchitecture` | 2762 | 2762 |
| `LocalIntegration` | 773 | 773 |
| `External` | 128 | 128 |
| anchor identities backed | 3114 | 3114 |
| duplicate obligation/variant pairs | 0 | 0 |
