# Status — ViciOne.ServiceBus test reconstruction

Chronological. Superseded entries carry an explicit marker and a link to what replaced them.

## Current state

**Wave R0 (research) complete. Awaiting Lead approval before the first test edit.**
No test file has been created, changed or deleted. The candidate worktree carries zero tracked
modifications; the only untracked path is the evidence directory of this work package.

| Wave | State |
|---|---|
| R0 research | complete, frozen, submitted |
| F1 foundation | blocked on Lead approval and on blocker B-1 |
| C1 hermetic core | not started |
| C2 local persistence | not started |
| C3 local brokers | not started |
| C4a external, written | not started |
| C4b external, executed | waiting for access by design |
| F2 TestFramework removal | not started |
| P1 promotion | requires C4b |
| G1 freeze | requires P1 |
| G2 independent review | not started |

## Frozen obligation set

3672 entries of obligation and variant, 0 duplicates: 2766 `UnitArchitecture`, 778
`LocalIntegration`, 128 `External`. Bound in `R0_FROZEN_RESULT.md`.

## Open blockers

| # | Item | Owner | Blocks |
|---|---|---|---|
| B-1 | `global.json` outside the write scope | Lead | F1 and every profile run |
| B-2 | no disposition for structurally unprovable service behaviour | Lead | terminal state of 2 obligations |
| B-3 | retained Python tool has no admissible proof path | Lead | tooling assurance closure |
| B-4 | real cloud access | Product Owner | C4b, P1, G1 |

## Open Lead questions

Nine, itemised in `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/INTEGRATOR_DECISIONS.md`:
write scope, disposition vocabulary (twice), Artemis identities inside a required anchor, the
broker-log assurance without a successor, the retained-tool proof path, one tracked Python file under
`evidence/`, the analyzer diagnostic that never fires, and the Protobuf descriptor still carrying its
pre-rename identity.

## Product Owner decisions taken

Recorded in `PO_DECISIONS_R0.md`: the Azure namespace no longer exists and cloud infrastructure is
rebuilt from scratch, so no key rotation is required; structurally unprovable service behaviour is
proven at the boundary we own and documented as a limitation; rebuilt tests assert intended
behaviour, so the four inherited product defects are not encoded as expected behaviour.

## Not-executed register — obligations that cannot be green yet

| Owner | Written | Emulator proof | Missing |
|---|---|---|---|
| Azure Service Bus, Event Hubs, Storage, Table | C4a | partial, capability matrix in `R0-CLOUD/RECONCILIATION.md` | real short-lived resources |
| Amazon SQS/SNS, S3, DynamoDB | C4a | partial, LocalStack limits named per mechanism | real short-lived resources |
| DynamoDB time-to-live, S3 lifecycle deletion | C4a | not provable in any profile | service sweeper up to 48 h — documented limitation per Product Owner decision |

## Requirement to evidence

Not yet started. Due before the freeze: every bound Product Owner requirement quoted verbatim and
mapped to concrete test names, architecture rules, graph evidence and executed runs.
