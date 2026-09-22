# Azure Service Bus and ActiveMQ durable-send classification

## Scope and defect

This slice follows the Azure Service Bus retry-taxonomy source commit
`a628ecc3c` and its evidence commit `9434753cc`. The durable sender and EF
transactional outbox both ask globally registered send-failure classifiers in
registration order. A successful `TryClassify` answer is authoritative.

With Azure Service Bus registered before ActiveMQ, the Azure classifier could
claim an ActiveMQ connection failure as transient when a nested cause was a
`TimeoutException`, `WebSocketException`, or Azure.Core
`RequestFailedException`. A permanent ActiveMQ configuration failure could
therefore enter durable retry. Four red theory cases reproduced the Azure
classifier's incorrect `true` answer before the source fix.

The first guard was too broad. The adversarial review traced an actual Azure
send path through `HostConfigurationRetryExtensions.RetryAsync`: when the host
stops after a failed send, Core wraps the last Azure failure in an exact
`ConnectionException`. A red product test confirmed that the broad guard
rejected a retryable Azure broker failure inside that wrapper. The final guard
delegates only when it finds a provider-specific foreign connection subtype
and no Azure Service Bus connection or broker cause. The exact Core wrapper
remains traversable, including when its cause is an HTTP status or timeout
without an Azure Service Bus-specific exception class.

## Behavioral evidence

| Test | Product assertion | Grade |
| --- | --- | --- |
| `ForeignConnectionWithTypedCause_DelegatesToItsTransport` | Four typed nested causes leave the Azure classifier unclassified; the real ActiveMQ classifier returns permanent | A |
| `StoppingHost_PreservesAzureSendFailureForDurableClassificationAsync` | Four real `RetryAsync` stop wrappers retain the exact original cause and classify as transient, including HTTP 503, status 0, and timeout | A |
| `AzureConnectionWrapper_RetainsItsExplicitTransientClassification` | A Service Bus connection wrapper retains its own transient decision across a shared nested connection type | A |
| `AggregateWithForeignConnection_PreservesPermanentAzureBrokerCause` | A public aggregate exception preserves a permanent Azure broker cause when another branch has a foreign connection | A |
| `AzureRegisteredFirst_ActiveMqConfigurationFailureQuarantinesWithoutRetryAsync` | Both transports register through their public DI APIs; a real InMemory outbox delivers once, quarantines the permanent ActiveMQ failure, and schedules no retry | A |

The test project references both production transport assemblies and the
existing signed durable-send test bridge. Its NuGet lockfile was refreshed in
the documented explicit unlocked restore mode. The production assemblies and
their dependency direction remain unchanged.

## Verification and measured risk

- Release Azure Service Bus Unit build: zero warnings and errors.
- Azure Service Bus Unit with Microsoft CodeCoverage: 212/212 passed, zero
  failures and skips; raw report:
  `artifacts/coverage-a-plus-20260922-asb-cross-transport/azure-servicebus-unit-final.cobertura.xml`.
- The first full Unit/Architecture attempt found one asynchronous test-name
  convention violation. The test and its requirement mapping were renamed;
  the previously failing architecture test then passed. The final complete
  Release Unit/Architecture gate passed 9,981/9,981, with zero failures and
  skips.
- Final read-only adversarial review returned PASS after the real stop-wrapper
  variants and InMemory outbox test were added.
- The isolated Azure Service Bus emulator passed 25/25, with zero failures and
  skips. Run `vicione-c4222c1a5aae` completed with `fixture-findings.json`
  containing `"findings": []`.
- A clean detached checkout of exact source commit `6d0ecbbae` passed locked
  NuGet restore and 212/212 Release Azure Unit tests with Microsoft
  CodeCoverage. Its raw report is
  `artifacts/coverage-a-plus-20260922-asb-cross-transport/azure-servicebus-unit-exact-6d0ecbbae.cobertura.xml`;
  SHA-256 `9167293bfe515c4b1f565fd0fd6845dadea32343280abc27b17410de39e37fad`.

| Classifier method | Lines | Branches | CRAP |
| --- | ---: | ---: | ---: |
| `TryClassify` | 10/10 | 8/8 | 8.00 |
| `HasForeignConnectionCause` | 10/10 | 14/14 | 14.00 |
| `HasAzureTransportCause` | 8/9 | 13/14 | 14.27 |
| `HasPermanentCause` | 13/13 | 28/28 | 28.00 |

The focused Microsoft CodeCoverage report measures this classifier and its
source-owned tests. It is not a new product-wide 35-report aggregate.
The latest complete product-wide profile still predates the Core AssemblyFinder
and Azure retry changes, so global A+ remains open.

The built-in durable dispatcher sends each delivery through one endpoint.
An aggregate containing failures from two different transport providers would
require a custom dispatcher or a constructed exception and has no unambiguous
owner under the current classifier interface. Raw `TimeoutException` and
`WebSocketException` without a provider marker are likewise ambiguous; this
slice does not change their existing classification.

The Microsoft `code-testing-agent` guided the focused test implementation,
`run-tests` the .NET 10 MTP commands, `grade-tests` and the .NET analysis
extension the assertion review, and `coverage-analysis` the focused CRAP
assessment. PowerShell was unavailable, so the Cobertura method values were
extracted with an equivalent inline XML calculation.
