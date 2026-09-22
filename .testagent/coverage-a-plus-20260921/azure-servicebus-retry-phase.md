# Azure Service Bus retry taxonomy and complete failure trees

## Product corrections

The last complete 35-report profile at `e1a965290` measured 80,159/90,376
product lines (88.6950%), a conservative branch interval of
29,184–31,649/36,380 (80.2199–86.9956%), and 111 methods above CRAP 30.
The receive and send host retry predicates ranked at CRAP 240 and 210, both
with no lines reached by that complete profile.

Host retries previously accepted every `ConnectionException` and
`RequestFailedException`, even when their explicit transient flag or HTTP
status made retry inappropriate. They missed an explicitly transient
`ServiceTimeout` and `GeneralError`. The receive path wraps broker failures in
`ServiceBusConnectionException`; the filter's OR semantics let that transient
outer wrapper override an inner permanent HTTP or SDK failure. Its ordinary
filter matching also missed a transient SDK exception in the middle of a
longer chain. Aggregate siblings required the same permanent-first decision.

The receive and send host policies now inspect every cause in linear and
aggregate exception trees. Permanent authorization, explicit non-transient
connection failures, SDK failures outside entity recovery, and permanent HTTP
statuses veto retry. Eligible
transient causes can trigger retry regardless of their depth. Receive and send
retain different entity-recovery rules; a shared broker-reason taxonomy keeps
the send classifier consistent with the host send policy. The send classifier
also inspects every aggregate sibling with permanent precedence. Azure HTTP
status 0 (no response) remains retryable, as in the
[Azure.Core retry-policy example](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/core/Azure.Core/samples/Configuration.md).

The send classifier is registered globally with other transport classifiers.
It therefore claims only Azure's `ServiceBusConnectionException`, leaving a
generic `ConnectionException` for its owning transport. The Azure host policy
is transport-scoped and can still honor the generic exception's explicit
transient flag.

## Hard regression evidence and test quality

The tests reproduced the failures before each correction: permanent wrapped
HTTP and SDK errors, transient `GeneralError`, a permanent second aggregate
sibling, a transient SDK middle node, status 0 under a timeout, classifier
aggregate siblings, wrapped non-transient broker reasons, and a foreign
connection error claimed by Azure. The new tests construct real exception
trees and invoke the actual host retry policies or send classifier. The retry
context test also checks scheduled delay and that authorization stops without
a retry. No test sleeps, replaces the implementation with a mock, or asserts
mere execution.

The 16 new test methods are A (90–100 band) under the Microsoft `grade-tests`
rubric and its .NET extension. Each has a product outcome assertion:

| Host-policy test | Grade | Distinguishing oracle |
| --- | --- | --- |
| `BrokerReasons_HaveDistinctReceiveAndSendRetryEligibility` | A | Receive/send reason and transient matrix |
| `RequestFailedStatuses_RetryOnlyRecoverableHttpFailures` | A | Status 0, 4xx boundaries and server failures |
| `ConnectionFailures_RespectTheirExplicitTransientFlag` | A | Both values of the explicit flag |
| `ConnectionWrapper_PreservesHttpFailureEligibility` | A | Wrapped permanent and recoverable statuses |
| `ConnectionWrapper_PreservesBrokerReasonEligibility` | A | Wrapped broker reasons and policy asymmetry |
| `ConnectionWrapper_RejectsPermanentBrokerCauseEvenWithNestedTimeout` | A | Permanent middle node vetoes transient base |
| `AggregateFailures_RejectPermanentSiblingAfterRecoverableFailure` | A | Permanent second sibling vetoes both policies |
| `AggregateFailures_RetryWhenLaterSiblingIsRecoverable` | A | Bare and wrapped aggregates see later transient child |
| `NestedWrapper_RetriesRecoverableBrokerCauseBetweenUnknownExceptions` | A | Transient broker reason at an intermediate node |
| `TimeoutWrapper_DoesNotTurnMissingHttpResponseIntoAPermanentFailure` | A | Status 0 inside timeout remains retryable |
| `RetryContext_SchedulesARecoverableTimeoutButStopsAuthorization` | A | Real retry context schedules only eligible failure |

| Send-classifier test | Grade | Distinguishing oracle |
| --- | --- | --- |
| `RequestWithoutHttpResponse_IsTransientEvenWhenWrapped` | A | Direct and wrapped status 0 |
| `AggregateFailures_PermanentSiblingOverridesRecoverableFirstChild` | A | Permanent second sibling and connection wrapper |
| `AggregateFailures_LaterRecoverableSiblingRemainsTransient` | A | Unclassified first sibling, transient second sibling |
| `BrokerFailures_KeepTheirSendEligibilityInsideConnectionWrappers` | A | Direct and wrapped reason matrix |
| `ForeignConnectionFailure_IsNotClaimedByAzureClassifier` | A | Both generic connection flags stay unclassified |

Every new `[RequirementCoverage]` marker has its matching requirements-JSON
projection. The Microsoft `code-testing-agent` guided the focused
source/test workflow, `run-tests` the .NET 10 MTP commands, `grade-tests` and
the .NET analysis extension the assertion review, and `coverage-analysis` the
CRAP assessment. PowerShell was unavailable, so the Cobertura method values
were extracted with an equivalent inline XML calculation using
`complexity² × (1 − line coverage)³ + complexity`.

## Gates and focused measurement

- Release Unit/Architecture build: zero warnings and zero errors.
- Complete Unit/Architecture gate: 9,970/9,970 passed, zero failures and skips.
- Azure Service Bus Unit with Microsoft CodeCoverage: 201/201 passed; raw report
  `artifacts/coverage-a-plus-20260922-asb-retry/azure-servicebus-unit-final-bytes.cobertura.xml`.
- Isolated Service Bus emulator: 25/25 passed, zero failures and skips;
  fixture run `vicione-199cfc8c3a87` has `findings: []`.

| Current focused method | Lines | Branches | CRAP |
| --- | ---: | ---: | ---: |
| `ServiceBusFailureTaxonomy.CanRetryBrokerFailure` | 12/13 | 9/14 | 14.09 |
| `ServiceBusHostConfiguration.HasNonRetryableCause` | 12/13 | 20/22 | 22.22 |
| `ServiceBusHostConfiguration.HasRetryableCause` | 12/13 | 23/24 | 24.26 |
| `ServiceBusSendFailureClassifier.HasPermanentCause` | 13/13 | 28/28 | 28 |
| `ServiceBusSendFailureClassifier.TryClassify` | 7/7 | 4/4 | 4 |

The two baseline CRAP 240/210 lambdas were replaced by the shared reason
decision and tree checks above. These are focused Azure Unit results, not a
new product-wide 35-report aggregate. The complete profile at `e1a965290`
remains the last global comparison point; Core and Azure Service Bus source
have both changed since then. Global A+ is still open.

The final read-only adversarial review returned PASS on this slice. It also
identified a pre-existing multi-transport boundary for direct
`TimeoutException`, `WebSocketException`, and Azure `RequestFailedException`
in the globally registered classifier list. That boundary needs a separate
review; this slice corrected the newly introduced generic-connection claim.
