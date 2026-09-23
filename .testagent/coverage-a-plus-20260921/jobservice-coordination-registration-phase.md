# JobService coordination registration slice

## Product contract and change

`JobServiceCorrelationConventions.Register` registers 30 message correlation
identities. `UseJobSagaPartitionKeyFormatters` registers 31 SQL partition key
formatters. Both methods had measured CRAP 62 in the previous complete product
profile even though they were fully covered. The registrations are now grouped
by contract family in private methods. Their order, lambda expressions, public
behavior, null guard, and one-time registration lock are unchanged.

The new `JobServiceCorrelationConventionsTests` checks every correlation type
against distinct JobTypeId, JobId, and AttemptId values. For each contract it
also verifies that an empty selected ID returns false and that null is rejected
with the correct parameter name. A second case verifies repeated parallel
registration after the global topology is frozen. The existing
`JobSagaPartitionKeyConfigurationTests` asserts all 31 formatters and the null
guard through the actual send topology.

## Verification

- Focused correlation cases: 2/2 passed.
- Focused partition cases: 2/2 passed.
- Complete Core suite: 6,378/6,378 passed.
- Complete Core Microsoft CodeCoverage suite: 6,378/6,378 passed. Report:
  `/private/tmp/vsb-jobservice-current.cobertura.xml`, SHA-256
  `5a98313b1f6520b81ae626fbb2658e0d9c6aa3ecfc610bf0bda16a692dabb52e`.
- Both changed source classes measure 100% of lines and branches in that Core
  report. Their largest resulting method complexity and CRAP score is 22. The
  original two registration methods each scored 62 in the previous complete
  profile. The full product profile has not yet been rerun for this change.
- The Engineering Release build with warnings as errors passed with zero
  warnings and zero errors.
- The complete Release Unit/Architecture solution gate passed 10,236/10,236
  with zero failures and zero skips.
- Read-only adversarial review compared every registration against the prior
  commit and returned PASS: all 30 correlations and 31 partition formatters
  retain exact order and lambda expressions.

## Open product risk

Four published Attempt events are consumed by both JobSaga and JobAttemptSaga.
The JobSaga's identity and in-memory partition are JobId; the AttemptSaga's
are AttemptId. A single outgoing SQL partition key is currently formatted
from AttemptId for these events and copied to both subscriptions. This is a
real ordering-contract tension, but the review did not prove a runtime state
error or data loss. The current refactor does not change it. A targeted
multi-attempt, two-endpoint SQL ordering test and an explicit product decision
about the required ordering guarantees are needed before claiming A+ for this
behavior. The existing partition test's phrase “owning identity” should be
read as its asserted mapping, not as proof of both saga ordering guarantees.

The complete product A+ goal remains open.
