# Azure Service Bus subscription reconciliation

## Product defects and contract

An existing subscription could have a broad `$Default` rule while setup
requested a named SQL rule. The first `GetRuleAsync` failure was caught as if
the subscription itself did not exist; a subsequent `AlreadyExists` response
was treated as success. A filter-only configuration could likewise return
success without changing an existing unidentifiable or ambiguous rule set.
Finally, when another creator won the subscription race, setup fetched the
winner but skipped all setting and rule reconciliation.

The corrected path catches `MessagingEntityNotFound` only before a
subscription is loaded. Existing subscriptions and concurrent creation winners
use the same reconciliation path. Filter-only reconciliation updates exactly
one GUID-named generated rule or fails closed; it does not claim ownership of
other rule sets. Explicitly named rules remain additive: the configured name
is reconciled, while any separately managed rules remain the caller's
responsibility.

## Hard regressions

Both newly reported silent-success paths were demonstrated in a red phase:

- `MissingConfiguredRule_RejectsExistingSubscriptionWithBroadDefaultRuleAsync`
  reproduced the original broad-rule defect against the real emulator and
  checks the persisted `$Default` rule after the corrected failure.
- `GeneratedFilter_RejectsBroadDefaultRuleInsteadOfReportingSuccessAsync`
  failed with “No exception was thrown” on the old code; the real broker now
  retains its single broad rule and setup reports that it cannot reconcile it.
- `ExistingGeneratedRule_UpdatesItsFilterWithoutAddingAnotherRuleAsync`
  verifies the real broker's single GUID rule changes from `ClientId = 69` to
  `ClientId = 27`, then remains stable on a repeated setup.
- `ConcurrentCreation_ReconcilesTheWinnerSubscriptionAndNamedRuleAsync` uses
  the SDK administration boundary to force the precise absent/create-race/
  winner sequence. Before the fix the update count was zero. It now verifies
  both the winner's `MaxDeliveryCount = 17` and its SQL rule, the exact SDK
  calls, the returned properties, and caller cancellation token forwarding.

All four tests have concrete broker or SDK state oracles and are A (90–100)
under the Microsoft `grade-tests` rubric. The shared broker fixture helper
only creates the actual topic/subscription and performs cleanup; assertions
remain in each test. Requirement projections map all four cases.

The Microsoft `code-testing-agent` guided the focused red/green additions,
`run-tests` supplied the .NET 10 MTP invocation, `grade-tests` and its .NET
extension guided the assertion review, and `coverage-analysis` guided the
focused CRAP measurement.

## Gates and focused risk

The Release Unit/Architecture build succeeded with zero warnings and errors.
The Azure Unit suite passed 213/213 after the corrections. The initial
28-test real emulator green run passed 28/28 with no skips and empty fixture
findings (`vicione-945bf1ce4915`). The final emulator rerun on the refactored
test bodies passed 28/28, zero skips, and empty fixture findings
(`vicione-98115bb0daf5`). Its Microsoft CodeCoverage report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/generated-filter-final.cobertura.xml`.
The complete Unit/Architecture gate passed 9,982/9,982 with zero failures
and skips.
An isolated, clean checkout of exact source/test commit `84b6f2df7` passed
locked restore, zero-warning Release build, and 213/213 Azure Unit tests with
Microsoft CodeCoverage. The preserved report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/azure-unit-exact-84b6f2df7.cobertura.xml`
with SHA-256 `2c4c95c79e62e5a4279f2c5c696e593baf2488a14ac60b33df7736c1fae0c450`.

Microsoft CodeCoverage on the final green emulator run measured the
refactored source methods below CRAP 30:

| Method | CRAP |
| --- | ---: |
| `CreateTopicSubscriptionAsync` | 15.71 |
| `ReconcileSubscriptionAsync` | 19.94 |
| `SubscriptionSettingsDiffer` | 8 |
| `NormalizeForwardTo` | 4 |
| `ReconcileNamedRuleAsync` | 16 |
| `ReconcileGeneratedFilterAsync` | 14 |

The previous long `CreateTopicSubscriptionAsync` had CRAP 218 in the focused
baseline report. The current profile is focused on Azure Service Bus. The
last complete product-wide measurement remains the 36-report profile at
`44d9e3254`; changed source bytes require a new aggregate before claiming a
global score. The product-wide A+ target remains open.

The final read-only adversarial review returned PASS after the two reported
silent-success paths were corrected. A second PASS confirmed that the shared
broker fixture helpers retained every assertion.
