# Azure Service Bus receive headers and persisted routing metadata

## Scope and product correction

The current complete pre-phase profile at `4965a8468` consists of 35 passing
Cobertura reports, including all 13 local providers and 32/32 product
assemblies. It measures 80,091/90,318 lines (88.6767%), a branch interval of
80.1061–86.8794%, and 114 methods with CRAP above 30. The complete profile
on the changed bytes is recorded below.

`ServiceBusHeaderProvider` previously let an application property named
`TransportSentTime` override the broker enqueue instant during lookup. The
header provider now resolves that broker-owned value first and omits application
entries with that name from enumeration, independent of key casing. Other
application properties keep exact-name lookup. Raw `MessageId` and
`CorrelationId` application headers retain their intended D-format precedence
over the broker's N-format fields.

## Behavioral evidence

Nine new tests verify the following contracts in
`ViciOne.ServiceBus.AzureServiceBus.Tests`:

| Contract | Test |
| --- | --- |
| Present system fields, application values including empty strings, and both spoofed sent-time spellings | `GetAll_ProjectsPresentSystemAndApplicationHeadersWithoutSpoofedTime` |
| Blank system fields are absent | `GetAll_OmitsBlankSystemValues` |
| Application keys are exact and mapped system names ignore case | `TryGetHeader_RequiresExactApplicationNameAndIgnoresSystemCase` |
| Raw identity headers retain canonical GUID formatting | `RawIdentityHeaders_TakePrecedenceOverBrokerFormatting` |
| Both sent-time lookup spellings return the exact UTC broker instant despite forged app values | `TransportSentTime_UsesTheBrokerEnqueueInstantInUtc` |
| Persisted provider keys and unrelated properties survive a full write/read | `PersistedRoutingMetadata_RoundTripsExactlyAndPreservesOtherProperties` |
| UTF-8 values, conflicting partition/session keys, blank and unsupported fields | `ReadPropertiesFrom_DecodesUtf8AndMakesSessionWinOverConflictingPartition` |
| Blank provider fields are not persisted | `WritePropertiesTo_OmitsBlankProviderMetadata` |
| Equal, conflicting, and reset session/partition assignments | `SessionAndPartitionKey_KeepOneRoutingIdentity` |

The assertions distinguish realistic mutations: moving broker time after app
lookup, changing the reserved-key comparison to case-sensitive, changing app
lookup to case-insensitive, prioritizing broker N-format identity over raw
D-format identity, dropping any persisted routing key, skipping UTF-8 decoding,
or retaining a conflicting session. An initial assertion for case-insensitive
application lookup failed against existing behavior; red-team review then
identified deliberate exact-name semantics and the separate sent-time spoofing
path. The final tests encode both contracts. Every
new test has concrete value, negative, collection, or state assertions; none
is an assertion-free coverage probe. Per-method review with the Microsoft
`grade-tests`, `assertion-quality`, and `test-gap-analysis` rubrics assigns all
nine tests grade A (90–100 band); there are no zero-assertion or trivial-only
tests, wall-clock dependencies, or mocks.

## Gates and measurement

- Release solution build: zero warnings, zero errors.
- Complete Unit/Architecture gate on the changed bytes: 9,912/9,912 passed,
  zero failed and zero skipped.
- Azure Service Bus Unit with Microsoft CodeCoverage: 151/151 passed.
- Azure Service Bus local emulator with Microsoft CodeCoverage: 25/25 passed;
  fixture findings empty (`vicione-52036e92cc0a`).
- Targeted CRAP in the fresh Unit report: `ReadPropertiesFrom` 110 → 10,
  `WritePropertiesTo` 110 → 10, `ServiceBusHeaderProvider.GetAll` 110 → 12;
  `TryGetHeader` is 22.76. The original scores are from the complete
  pre-phase profile and the new scores from the focused post-phase report.
- Adversarial read-only review found three initial counterexamples. All were
  corrected; the final re-review returned PASS with no remaining concrete
  finding.
- Complete current-byte global aggregate at `e1a965290`: 35 fresh, passing
  Cobertura reports from 22 Unit/Infrastructure and all 13 local-provider
  modules; 32/32 product assemblies observed and no source/test diff. It
  measures 80,159/90,376 lines (88.6950%), a branch interval of
  29,184–31,649/36,380 (80.2199–86.9956%), and 111 methods above CRAP 30.
  The 405 broad-matrix, 69 SQL Server, 31 RabbitMQ, and 25 Azure Service Bus
  provider tests all passed without failures or skips. All four fixture runs
  recorded empty findings. Raw reports and method scores are under
  `artifacts/coverage-a-plus-20260922-e1a965290/`.

The global A+ requirement remains open; this complete profile is its next
measured baseline.
