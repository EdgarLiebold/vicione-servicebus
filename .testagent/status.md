# Status — ViciOne.ServiceBus test reconstruction

Chronological. Superseded entries carry an explicit marker and a link to what replaced them.

## Current state

**R0 converged under `DIR-A0071-R0-CONVERGENCE-04`, on Development Slice revision 0002 bound by
reslice `0008`. Awaiting Lead approval before the first test edit.**

No C# test or product implementation was created, changed or deleted. Two files entered the write
scope through the reslice and are edited for the first time here: `.gitattributes` receives one
exact-path whitespace exception for the immutable superseded list, and `global.json` receives the
Microsoft Testing Platform selection beside its unchanged SDK pin.

The candidate is the convergence commit; the exchange record created after it binds its commit and
tree. No file committed by that commit contains its own commit hash.

Earlier intermediate commits are unaccepted history, not active truth: `0e58fa5c` (first checkpoint,
not approved), `cf1f5bb4`, `caf3938f` and `d93d7757` (first correction round, not approved). The
accepted evidence of those rounds keeps its bound bytes; the effective artefacts live under
`R0-CONVERGENCE-04/`.

| Wave | State |
|---|---|
| R0 research | complete and frozen; converged after the second Lead review |
| F1 foundation | blocked on the corrected approval |
| C1 hermetic core | not started |
| C2 local persistence | not started |
| C3 local brokers | not started |
| C4a external, written | not started |
| C4b external, executed | waiting for access by design |
| F2 TestFramework removal | not started |
| P1 promotion | requires C4b |
| G1 freeze | requires P1 |
| G2 independent review | not started |

## Requirement state

| Requirement | State |
|---|---|
| `REQ-TEST-101` | complete — baseline, independence from the cancelled attempt, pre-edit record |
| `REQ-TEST-102` | active |
| `REQ-TEST-103` | active |
| `REQ-TEST-104` | active |
| `REQ-TEST-105` | active |
| `REQ-TEST-106` | active |
| `REQ-TEST-107` | active |
| `REQ-TEST-108` | active — neither complete nor optional; governs the transition, the removal of the inherited stack and the single freeze commit |
| `REQ-TEST-109` | active |

## Frozen obligation set

3663 entries of obligation and variant, 0 duplicates, across **41 executable target projects in 41
cohorts**, each project in exactly one profile and each with exactly one writer.

**No disposition in R0 is terminal.** Every ledger row carries `dispositionState`, which is
`PROPOSED` for semantic-ledger rows, `TOOLING_REGISTER` for the assurance promises of the removed
Python platform, and `TERMINAL` for nothing — a terminal disposition requires named new tests green
in the due profile, which cannot exist before F1.

## Open blockers

| # | Item | Owner | Blocks |
|---|---|---|---|
| B-1 | *(closed by reslice 0008: `.gitattributes` and `global.json` are in the write scope, revision 0002)* | — | — |
| B-2 | real cloud access | Product Owner | C4b, P1, G1 |

## Lead dispositions received

All eleven technical questions of the first checkpoint were dispositioned in
`DIR-A0071-R0-CORRECTION-01` section 6 and are applied
(`R0-CORRECTION-01/LEAD_DISPOSITIONS_APPLIED.md`). Findings F-01 through F-09 of the second review and R0-C02-01 to R0-C03-02 of the third and
fourth reviews are applied in `R0-CONVERGENCE-04/` (see its `CONVERGENCE_EVIDENCE.md` and
`PROOF_MAPPING_TABLE.md`); the earlier convergence directories are bound historical evidence. Question `0007` was
answered by reslice `0008` with `OPT-A-ADD-BOTH-PATHS`. No question remains open and none is
re-asked.

## Product Owner decisions taken

`R0/PO_DECISIONS_R0.md`: the Azure namespace no longer exists and cloud infrastructure is rebuilt from
scratch, so no key rotation is required; structurally delayed service effects are proven at the
boundary we own; rebuilt tests assert intended behaviour, so inherited product defects are not encoded
as expected behaviour; everything locally provable is finished to A+ first.

## Not-executed register — obligations that cannot be green yet

| Owner | Written | Emulator proof | Missing |
|---|---|---|---|
| Azure Service Bus, Event Hubs, Storage, Table | C4a | partial; capability matrix per named mechanism | real short-lived resources |
| Amazon SQS/SNS, S3, DynamoDB | C4a | partial; LocalStack limits named per mechanism | real short-lived resources |
| RabbitMQ via Amazon MQ | C4a | none | real Amazon MQ; the inherited verification model already records it as a real-cloud resource |
| DynamoDB time-to-live, S3 lifecycle | C4a | configuration asserted at the service boundary | the provider-side sweep is a documented external limitation, not a ViciOne obligation |

## Requirement to evidence

Not yet started. Due before the freeze: every bound Product Owner requirement quoted verbatim and
mapped to concrete test names, architecture rules, graph evidence and executed runs.
