# R0-CORE-C — anchor reconciliation

Cohort `R0-CORE-C`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.
Anchor file `build/verification/expected/core.txt`,
SHA-256 `bc2910d3b7611aed036211fc69e67155bdbfe403ed8af5ccadfabc914ca865f5` (measured, matches
Lead plan § 9), 1876 lines of which 3 are documenting header lines and **1873 are identities**.

## 1. Scope closure (TLP-017)

`git ls-files` over the fourteen assigned subdirectories of `tests/ViciOne.ServiceBus.Tests/`
returns **115 tracked files**. All 115 were read in full; none was sampled.

| Directory | files (`git ls-files`) | assigned | read |
|---|---:|---:|---:|
| `Serialization/` | 34 | 34 | 34 |
| `Courier/` | 23 | 23 | 23 |
| `Testing/` | 11 | 11 | 11 |
| `ReliableMessaging/` | 11 | 11 | 11 |
| `MessageData/` | 10 | 10 | 10 |
| `Messages/` | 8 | 8 | 8 |
| `Transforms/` | 3 | 3 | 3 |
| `Transactions/` | 3 | 3 | 3 |
| `Audit/` | 3 | 3 | 3 |
| `Caching/` | 2 | 2 | 2 |
| `Topology/` | 2 | 2 | 2 |
| `Transports/` | 2 | 2 | 2 |
| `Diagnostics/` | 2 | 2 | 2 |
| `Groups/` | 1 | 1 | 1 |
| **total** | **115** | **115** | **115** |

`READ_MANIFEST.tsv` carries `path<TAB>sha256` for all 115, sorted `LC_ALL=C`, plus one header line
(116 lines). Its own SHA-256 is `1ca94bcba3c382e963d20dd9b5c89c129f76ab173ccfcd51f87424e27a57afa2`.

Nothing in `SagaStateMachineTests/`, `Saga/`, `ContainerTests/`, `Middleware/`, `Pipeline/`,
`Initializers/`, `Configuration/`, `Conventional/` or the 106 root-level files was read as scope or
disposed; those belong to other agents.

## 2. How the filtered anchor subset was derived

The anchor lists fully qualified NUnit identities. Every identity begins with
`ViciOne.ServiceBus.Tests.`; the segment that follows names the directory-derived namespace segment.
The subset for this cohort is exactly the identities whose first segment after that prefix is one of
the fourteen assigned directory names:

```
grep -E "^ViciOne\.ServiceBus\.Tests\.(Serialization|Courier|Testing|MessageData|Caching|Transactions|
Transforms|ReliableMessaging|Transports|Audit|Topology|Diagnostics|Groups|Messages)\." \
  build/verification/expected/core.txt | sort
```

Derivation, scope and commit as above; no other filter was applied and no identity was reassigned.

| Segment | anchor identities | orientation figure from the task | agreement |
|---|---:|---:|---|
| `Serialization` | 236 | 236 | yes |
| `Courier` | 81 | 81 | yes |
| `Testing` | 55 | 55 | yes |
| `MessageData` | 27 | 27 | yes |
| `Caching` | 13 | 13 | yes |
| `Transactions` | 11 | 11 | yes |
| `Transforms` | 9 | 9 | yes |
| `ReliableMessaging` | 7 | 7 | yes |
| `Transports` | 5 | 5 | yes |
| `Audit` | 4 | not given | — |
| `Groups` | 3 | not given | — |
| `Topology` | 2 | not given | — |
| `Diagnostics` | 1 | not given | — |
| `Messages` | 0 | not given | — |
| **filtered total** | **454** | — | — |

The nine orientation figures were verified against the file rather than trusted. All nine agree.
The five figures not given in the task are reported here for the first time: `Audit` 4, `Groups` 3,
`Topology` 2, `Diagnostics` 1, `Messages` 0. **454** is the filtered total this cohort owns.

## 3. Reconciliation in both directions

The census was derived from the read sources, not from the anchor: every `.cs` file in scope was
parsed with namespace and brace tracking, `[Test]`-attributed methods were collected with their
declaring type chain, and each case was then expanded into its runtime identity form —

* nested types joined with `+` (`InMemory_Specs+Storing_message_data_in_memory`);
* `[TestFixture(typeof(T))]` rendered as the fully qualified serializer type in parentheses after the
  class name, and `[TestFixture(typeof(T))]` on a generic fixture rendered as `<T>`;
* `[Values(...)]` and `[TestCaseSource]` rendered as the case name in parentheses after the method
  name, using the `SetName("{m}(<name>)")` the source itself declares.

Result:

| Direction | count |
|---|---:|
| identities discovered from the read sources | **454** |
| identities in the filtered anchor subset | **454** |
| anchor identities with no ledger row | **0** |
| ledger rows with no anchor identity | **0** |

**The two sets are equal element for element. There is no deviation to explain and nothing was
adopted from the anchor.** Per-directory the discovered counts are identical to the anchor column of
the table in § 2.

Three expansion rules had to be modelled correctly before the sets matched; they are recorded here
because they are the shape the rebuilt identities have to keep, not because they were differences:

1. `Serialization/MessageBodyLength_Specs.cs` — `The_length_a_message_body_reports` carries six
   `[TestCaseSource(nameof(Bodies))]` methods over 13 named body cases: 78 identities from six source
   methods. The names come from `BodyCases()`, which is also the subject census the class asserts
   against the compiler.
2. `Courier/ActivityRedelivery_Specs.cs` — `Should_include_all_variables([Values(1,2,3)] int)` is
   three identities from one method.
3. `Courier/Subscription_Specs.cs` — `Adding_a_custom_routing_slip_event_subscription<T>` is a
   generic fixture with two closings, `<Json>` and `<RawJson>`.

## 4. Ledger

`LEDGER_DRAFT.jsonl` carries **473 rows**:

* **454** rows, one per anchor identity, with parameterized variants fully resolved rather than
  collapsed. The `variants` field carries the resolved fixture and case suffix.
* **19** further rows for obligations that carry **no** executable identity and therefore appear in
  no anchor. They are non-terminal (`QUESTION`) and are listed so that nothing is dropped silently:
  * `Diagnostics/Trace_Specs.cs` — the whole file is commented out and contributes 0 of the 1
    `Diagnostics` identity (that one belongs to `StatsD_Specs`). § 9 forbids deleting it for
    "empty body" or "no assertion" alone.
  * the eight `Messages/*.cs` support types (`Messages` contributes 0 identities);
  * `Serialization/SerializationTest.cs` (the shared fixture base, whose `Return<T>` carries six
    addressing assertions that all 236 `Serialization` identities silently inherit),
    `Audit/InMemoryAuditStore.cs`, `ReliableMessaging/ReliableStateMachine.cs`,
    `ReliableMessaging/ReliableConsumer.cs`, the two `.proto` files, the two generated `.proto.cs`
    files and `TradesBookedViciOneServiceBus.Populate.cs`;
  * one cohort-wide row for the § 11 serializer-format requirement.

Disposition split: **373** `PROPOSED_REPLACED_EXECUTING`, **100** `QUESTION`. No row is
`REMOVED_*`, `BENCHMARK_ONLY` or `DIAGNOSTIC_ONLY`. Nothing was dropped for test count, coverage,
text similarity, empty body, missing assertion, `Explicit`, `Ignore`, platform condition or missing
infrastructure — no case in this cohort carries `Explicit`, `Ignore` or a platform condition at all.

## 5. Differences reported, not adopted

Only one structural difference exists between the anchor and the Lead plan, and it is a difference in
**ownership**, not in identity:

**The anchor binds MessagePack identities to `core`; Lead plan § 5 binds their owner to a separate
test project.** 67 of the 454 identities run against `src/ViciOne.ServiceBus.MessagePack` — every
`[TestFixture(typeof(MessagePackMessageSerializer))]` variant plus the three files that address the
module directly (`MessagePackHardening_Specs.cs`, `MessagePackEnvelopeClone_Specs.cs`,
`InterfaceFormatterCaching_Specs.cs`). The 67 split by file as: `MoreSerialization_Specs.cs` 17,
`MessagePackHardening_Specs.cs` 12, `InterfaceFormatterCaching_Specs.cs` 8, `GivenAComplexMessage.cs` 7,
`MessagePackEnvelopeClone_Specs.cs` 6, `PropertyType_Specs.cs` 4, `IEnumerable_Specs.cs` 3,
`XmlPayload_Specs.cs` 3, `Interface_Specs.cs` 2, `ReceiveFault_Serialization_Specs.cs` 2,
`JobDeserialization_Specs.cs` 1, `MessageDataSerialization_Specs.cs` 1, `Redelivery_Specs.cs` 1.
§ 5 maps `src/ViciOne.ServiceBus.MessagePack` to
`ViciOne.ServiceBus.MessagePack.Tests`. Those rows carry `targetProject:
"ViciOne.ServiceBus.MessagePack.Tests"` as the **proposal** and say so in `notes`. The inherited
binding is left as it stands; the Lead disposes whether the identities move out of `core` or the § 5
owner rule yields for them.

## 6. Product code read for this cohort

Enumerated, so that what was and was not read is checkable.

Read **in full**:

* `src/ViciOne.ServiceBus/Serialization/` — all 48 files (3474 lines).
* `src/ViciOne.ServiceBus.MessagePack/` — all 12 `.cs` files (1187 lines), plus the `.csproj` and
  `packages.lock.json` entries by name.
* `src/ViciOne.ServiceBus/Courier/` — all 23 files (1830 lines).
* `src/ViciOne.ServiceBus/Testing/AsyncTestHarness.cs` (the single file § 2 Nr. 3 allows to be
  edited editorially).
* `src/ViciOne.ServiceBus.Abstractions/MessageData/MessageDataDefaults.cs`.
* `src/ViciOne.ServiceBus/Topology/GlobalTopology.cs`, `MessageCorrelation.cs`.

Read **selectively, by the surface the cohort exercises**, with the full file list enumerated:

* `src/ViciOne.ServiceBus/MessageData/` (48 files enumerated). Read in full: the three repositories
  (`InMemory`, `FileSystem`, `Encrypted`), `InMemoryMessageDataId`, `IInlineMessageData`, all eight
  `Values/*`, and both `PropertyProviders/{Put,Get}MessageDataPropertyProvider`. The
  `Configuration/*` and `Conventions/*` families were read for their public contract.
* `src/ViciOne.ServiceBus/Testing/` (85 files enumerated). Read in full: `AsyncTestHarness`,
  `Implementations/AsyncElementList`, `Implementations/AsyncInactivityObserver`, `SentMessageList`,
  `ReceivedMessageList`, `PublishedMessageList`, `FilterSet`, `SentMessageFilterSet`,
  `MultiTestConsumer`.
* `src/ViciOne.ServiceBus/Caching/` (27 files enumerated) and
  `src/ViciOne.ServiceBus/Internals/Caching/` (20 files enumerated). The cohort splits across both:
  `Caching/Cache_Specs.cs` and `Caching/CacheRecovery_Specs.cs` exercise
  `Internals.Caching.ViciOneServiceBusCache`, while `Audit/InMemoryAuditStore.cs` is the only
  consumer of `ViciOne.ServiceBus.Caching.GreenCache`. Read in full from `Internals/Caching`:
  `ICache`, `ICacheValue`, `CacheValue`, `CacheOptions`, `UsageCachePolicy`,
  `TimeToLiveCachePolicy`, `TimeToLiveCacheValue`, `ITimeToLiveCacheValue`, `IValueTracker`,
  `ValueTracker`.
* `src/ViciOne.ServiceBus/Topology/` (37 files enumerated), read for the correlation-id and
  set-serializer conventions the two `Topology/` cases exercise.
* `src/ViciOne.ServiceBus.Abstractions/` (804 tracked files) was **not** read in full. Read: the
  `Courier/Courier/Contracts/*` and `Courier/Courier/Messages/*` families by name and shape,
  `IRoutingSlipBuilder`, `RoutingSlipExtensions`, `IItineraryBuilder`, `IExecuteActivity`,
  `ICompensateActivity`, and `MessageDataDefaults`. **This is reported as an incomplete read**, not
  claimed as complete.

## 7. Open questions for the Lead

1. **MessagePack ownership split** (§ 5 above) — 84 identities.
2. **`Diagnostics/Trace_Specs.cs`** — removed capability with a removal proof, or an unbuilt gap?
3. **Section 11 "raw XML"** — see `FINDINGS.md` § 1 and `OBL-R0-CORE-C-0473`.
4. **`Sending_message_data_with_an_object_type`** — the source marks its third dictionary assertion
   `// Will fail`, yet the identity is bound as executing. One of the two is stale.
5. **`AsyncTestHarness.BeginTestScope` caller** — see `FINDINGS.md` § 2.
6. **Cross-cohort files** — `Caching/CacheRecovery_Specs.cs` depends on
   `Middleware/Caching/Tests.cs` (`Middleware.Caching.TestValueObjects`), which is another agent's
   scope; four `Messages/*.cs` types are declared in the root namespace `ViciOne.ServiceBus.Tests`
   and referenced from outside this cohort. The integrator owns both.

## 8. Reporting honesty

Every number above names its derivation, scope, commit and file hash. The census is derived from the
115 files this agent read, not from the anchor. Where a read is partial (`Abstractions`, and the
selectively read families of `MessageData`, `Testing`, `Caching` and `Topology`) it is named as
partial in § 6. No number in this document is a guess.
