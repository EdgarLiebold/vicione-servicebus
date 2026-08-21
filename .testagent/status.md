# Status — ViciOne.ServiceBus test reconstruction

Chronological. Superseded entries carry an explicit marker and a link to what replaced them.

## Current state

**Wave R0 corrected under Lead directive `DIR-A0071-R0-CORRECTION-01`. Awaiting Lead approval before
the first test edit.**

No C# test, product source, build implementation, old test file or `global.json` was created, changed,
moved or deleted. The R0 evidence of the first checkpoint is **committed** at
`0e58fa5c8fb4f15a2b812176130d76aa69ea1d4f`; this correction adds one further commit. The worktree is
clean after each.

*(Superseded: the previous version of this file stated that the R0 evidence directory was untracked.
That was written before the commit and never re-read afterwards. Replaced by the sentence above.)*

| Wave | State |
|---|---|
| R0 research | complete and frozen; first checkpoint not approved, correction submitted |
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

3663 entries of obligation and variant, 0 duplicates: 2762 `UnitArchitecture`, 773
`LocalIntegration`, 128 `External`, across 41 executable target projects, each in exactly one profile.

**No disposition in R0 is terminal.** Every ledger row carries `dispositionState`, which is
`PROPOSED` for semantic-ledger rows, `TOOLING_REGISTER` for the assurance promises of the removed
Python platform, and `TERMINAL` for nothing — a terminal disposition requires named new tests green
in the due profile, which cannot exist before F1.

## Open blockers

| # | Item | Owner | Blocks |
|---|---|---|---|
| B-1 | `global.json` write scope — the Lead adds it together with the approval | Lead | F1 and every profile run |
| B-2 | real cloud access | Product Owner | C4b, P1, G1 |

## Lead dispositions received

All eleven technical questions of the first checkpoint were dispositioned in
`DIR-A0071-R0-CORRECTION-01` section 6 and are applied; see
`evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0-CORRECTION-01/LEAD_DISPOSITIONS_APPLIED.md`.
No question of that set remains open and none is re-asked.

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
