# ActiveMQ A+ phase

## Product defects fixed

- `false` application headers were silently discarded; both Boolean values now cross the transport.
- `decimal`, `Guid`, and other non-native `IFormattable` values could enter Apache NMS and fail only
  during provider serialization. They are now formatted with invariant culture.
- `byte[]` was accepted by the generic Apache NMS primitive-map serializer but is forbidden by
  OpenWire message properties. It is now consistently omitted for the shared provider contract.
- local and unspecified `DateTime` values could describe different instants. Every kind is now
  normalized to its intended UTC instant before conversion to Unix milliseconds.
- direct topic-consumer diagnostics emitted an empty destination field. Absent optional fields are
  now omitted.

## Hard behavior evidence

- ActiveMQ Unit/Contract: 174/174 passed.
- Complete Unit/Architecture profile: 9,788/9,788 passed, 0 failed, 0 skipped.
- Canonical Classic ActiveMQ fixture: OpenWire and AMQP header round-trips passed 2/2, run
  `vicione-22590988c83f`.
- Canonical Artemis fixture: AMQP header round-trip passed 1/1, run `vicione-f51c548b1af8`.
- The provider test observes raw receive-transport headers. It requires invariant decimal and Guid
  strings, the retained false value, and absence of the OpenWire-incompatible byte array.
- The unit regression executes `Apache.NMS.Util.PrimitiveMap.Marshal()` and round-trips the exact
  shared native scalar types.

## Coverage and CRAP

Fresh focused report:
`artifacts/coverage-a-plus-20260921-active-mq-broker-fixed`.

- ActiveMQ focused lines: 1,302/2,363.
- ActiveMQ focused branches: 483/995.
- All eight selected baseline hotspots are below CRAP 30.
- Highest selected result: CRAP 22.
- `TransportHeaderExtensions.SetHeaders`: CRAP 12, 100% lines, 12/12 branches.
- `TransportHeaderExtensions.SetHeaderValue`: CRAP 6, 100% lines, 6/6 branches.

This focused report observes three assemblies and does not replace the next product-wide 32-assembly
aggregate.

## Microsoft grade-tests assessment

All 21 added tests were graded against the xUnit guidance from the Microsoft `dotnet/skills`
test-analysis extension. Distribution: **21 A, 0 B, 0 C, 0 D, 0 F**. The accompanying
test-anti-pattern review found 0 Critical, 0 High, 0 Medium, and 0 Low findings.

| Test | Grade | Band | Note |
| --- | --- | --- | --- |
| `SetHeaders_PreservesNativeValuesAndSkipsProviderOwnedOrUnsupportedHeaders` | A | 90–100 | Exact precedence and negative omission assertions. |
| `SetHeaders_ProjectsEveryDateTimeKindAsUnixMilliseconds` | A | 90–100 | Every `DateTimeKind` is checked against an independent instant oracle. |
| `SetHeaders_ProjectsSupportedScalarWireRepresentations` | A | 90–100 | Real NMS marshalling proves exact scalar wire values. |
| `SetHeaders_PreservesTheSharedOpenWireAndAmqpNativeTypes` | A | 90–100 | Exact types and values survive NMS marshal/unmarshal. |
| `SetHeaders_FormatsReferenceFormattablesInvariantly` | A | 90–100 | A hostile culture distinguishes invariant formatting. |
| `SetHeaders_TransfersAndConsumesTheClassicScheduledDelayHeader` | A | 90–100 | Transfer, continued enumeration, and one-time consumption are asserted. |
| `SetHeaders_ContainsNullValuesFromExternalImplementations` | A | 90–100 | A nonconforming external implementation exercises removal boundaries. |
| `TransportProperties_RoundTripEveryExplicitActiveMqValue` | A | 90–100 | Full persisted property round-trip with exact values. |
| `TransportProperties_OmitAndRestoreAbsentActiveMqValues` | A | 90–100 | Absence clears stale state and leaves no persisted keys. |
| `QueueEntityComparer_UsesExactTypeNameAndLifecycleSettings` | A | 90–100 | Equality, hash, null, subtype, and every field discriminator are checked. |
| `QueueNameComparer_UsesExactTypeAndNameOnly` | A | 90–100 | Name identity and deliberate lifecycle exclusion are distinguished. |
| `QueueDiagnostics_IncludeOnlyEnabledLifecycleFlags` | A | 90–100 | Exact diagnostic output covers both lifecycle combinations. |
| `TopicEntityComparer_UsesExactTypeNameAndLifecycleSettings` | A | 90–100 | Equality, hash, null, subtype, and every field discriminator are checked. |
| `TopicNameComparer_UsesExactTypeAndNameOnly` | A | 90–100 | Name identity and deliberate lifecycle exclusion are distinguished. |
| `TopicDiagnostics_IncludeOnlyEnabledLifecycleFlags` | A | 90–100 | Exact diagnostic output covers both lifecycle combinations. |
| `EntityComparer_DetectsEveryBrokerRelevantConsumerDifference` | A | 90–100 | Every broker-relevant field has a negative control. |
| `NameComparer_DistinguishesQueueAndDirectTopicConsumers` | A | 90–100 | Queue and direct-topic identity, nulls, types, and hashes are distinguished. |
| `ConsumerDiagnostics_OmitAbsentOptionalValues` | A | 90–100 | Exact complete and sparse diagnostics expose the regression. |
| `BusTopology_ResolvesRuntimeMessageTypesAndAppliesLifecycleOverrides` | A | 90–100 | Runtime metadata and explicit override semantics are both asserted. |
| `NonNativeScalars_CrossClassicProviderBoundariesAsync` | A | 90–100 | Real OpenWire and AMQP broker outcomes are asserted through raw headers. |
| `NonNativeScalars_CrossArtemisProviderBoundaryAsync` | A | 90–100 | Real Artemis AMQP outcome reuses the same exact contract oracle. |

## Pseudo-mutation discriminators

- Restoring the old true-only Boolean case fails both scalar and broker tests.
- Passing `decimal` or `Guid` through unchanged fails NMS marshalling or the broker round-trip.
- Passing `byte[]` through fails the OpenWire broker case; serializing it produces a forbidden header.
- Using current culture fails under `de-DE`.
- Treating local or unspecified time as a different kind fails the instant assertions.
- Removing any comparer field fails its dedicated negative control.
- Restoring the empty direct-topic destination fails the exact sparse diagnostic assertion.
- Dropping or inventing persisted send properties fails the present/absent round-trip pair.
- Ignoring runtime message metadata or lifecycle overrides fails the destination-address contract.

## Adversarial review

The first read-only review found the false provider proxy for `decimal`. The correction exposed and
fixed the additional OpenWire `byte[]` boundary through a real broker test. The final read-only
re-review returned **PASS with no further concrete findings**.
