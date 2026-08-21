# R0-CORE-D — anchor reconciliation

Cohort `R0-CORE-D`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.

## 1. Scope

The 106 files that sit directly in `tests/ViciOne.ServiceBus.Tests/` with no subdirectory.
The scope set was produced by

```
git ls-files tests/ViciOne.ServiceBus.Tests | sed 's|tests/ViciOne.ServiceBus.Tests/||' | awk -F/ 'NF==1'
```

which returns 106 paths: 104 `.cs` files, `ViciOne.ServiceBus.Tests.csproj` and `packages.lock.json`.
All 106 were read completely; `READ_MANIFEST.tsv` carries `path<TAB>sha256` for every one of them,
sorted with `LC_ALL=C`. No subdirectory of the project was read or censused.

## 2. Anchor and derived subset

Anchor: `build/verification/expected/core.txt`,
SHA-256 `bc2910d3b7611aed036211fc69e67155bdbfe403ed8af5ccadfabc914ca865f5`,
1876 lines of which 3 are documenting header comments, so **1873 identities** — the number the Lead
plan binds for the `core` category. Every one of the 1873 begins with `ViciOne.ServiceBus.Tests.`
(verified: 0 lines do not).

Derivation of my subset, step by step:

1. Strip the leading `ViciOne.ServiceBus.Tests.` from each identity and take the first remaining
   dot-separated segment.
2. Drop every identity whose first segment is one of the 22 subdirectory namespaces
   (`SagaStateMachineTests`, `ContainerTests`, `Middleware`, `Serialization`, `Saga`, `Courier`,
   `Pipeline`, `Testing`, `ReliableMessaging`, `MessageData`, `Messages`, `Initializers`,
   `Conventional`, `Configuration`, `Transforms`, `Transactions`, `Audit`, `Transports`, `Topology`,
   `Diagnostics`, `Caching`, `Groups`).
   `Messages` contributes 0 identities; the other 21 contribute 1327 together.
3. Result: **546 identities** whose first segment is a class name or a file-local nested namespace.
4. Subtract the **12 identities already claimed by sibling cohort `R0-CORE-B`**, which that cohort
   proved are declared in files inside `ContainerTests/` although they carry the root namespace:
   `ConsumeMetrics_Specs` (5), `InstrumentationRegistration_Specs` (6),
   `KillSwitchInstrumentation_Specs` (1). These are excluded here, are **not** claimed by this
   cohort, and appear in no ledger row of mine.

**Derived filtered anchor total for R0-CORE-D: 546 − 12 = 534 identities.**

## 3. Both-direction match

| Direction | Count |
|---|---:|
| Filtered anchor identities (net of R0-CORE-B) | 534 |
| Identities covered by R0-CORE-D ledger rows | 534 |
| Anchor identities with no ledger row | **0** |
| Ledger identities with no anchor identity | **0** |

The comparison is done per identity, with parameterized cases expanded: a ledger row for a
parameterized method carries its fully resolved variant list, and the reconciliation compares that
list, not the method name. 15 rows carry a variant list (79 resolved variant identities in total);
the remaining 452 behavioural rows map one to one onto a single anchor identity.

Every one of the 534 identities was mapped to a declaring file by resolving the class part of the
identity against the class and nested-namespace declarations found in the 104 root `.cs` files.
The mapping is total in both directions: 534 mapped, 0 unmapped, and no class of a root file
carries an identity that is absent from the anchor.

## 4. Obligation count versus identity count

534 identities collapse into **467 behavioural obligations**, because 15 parameterized methods carry
79 identities between them:

| File | Method | Variants |
|---|---|---:|
| `CronExpressionTests.cs` | `CanUseLastDayOfMonthInArray` | 8 |
| `CronExpressionTests.cs` | `CanUse_DayOfMonth_And_DayOfWeek_Together` | 5 |
| `CronExpressionTests.cs` | `CannotUseMultipleLastDayOfMonthInArray` | 1 |
| `CronExpressionTests.cs` | `CronExpressionReturnsExpectedNextFireTime` | 10 |
| `CronExpressionTests.cs` | `Ensure_L_Token_CanOnlyBeUsedIn_DayOfWeek_ORDayOfMonth` | 7 |
| `CronExpressionTests.cs` | `Ensure_NthWeek_Day_IsBetween1And7` | 10 |
| `CronExpressionTests.cs` | `Ensure_NthWeek_IsBetween1And5` | 7 |
| `CronExpressionTests.cs` | `ExpressionEquality` | 7 |
| `CronExpressionTests.cs` | `ExpressionToString` | 1 |
| `CronExpressionTests.cs` | `GivenMonthAbbreviation_ShouldGetTimeAfter` | 3 |
| `CronExpressionTests.cs` | `LastWeekDayWithOffset` | 6 |
| `CronExpressionTests.cs` | `OffSetValue_CannontBe_GreaterThan30` | 1 |
| `CronExpressionTests.cs` | `QuestionMark_With_ExtraWhitespace_Should_Be_Valid` | 2 |
| `CronExpressionTests.cs` | `Should_Throw_Error_When_Extra_NonWhitespace_Character_After_QuestionMark` | 3 |
| `JobAttemptGeneration_Specs.cs` | `Should_leave_the_job_saga_unchanged` | 11 |

`CronExpressionTests.cs` therefore carries 58 obligations across 115 identities and
`JobAttemptGeneration_Specs.cs` 2 obligations across 12 identities. Every other file has one
obligation per identity.

## 5. The reverse cross-cohort pattern, explicitly checked

R0-CORE-B found identities under the root namespace that are declared inside a subdirectory. The
reverse — a file of mine declaring a namespace that belongs to a sibling cohort — was checked
directly. The complete set of namespaces declared by the 104 root `.cs` files is:

```
ViciOne.ServiceBus.Tests
ViciOne.ServiceBus.Tests.FaultMessages          (ConsumeObserver/FaultPoly contracts)
ViciOne.ServiceBus.Tests.JobConsumerTests
ViciOne.ServiceBus.Tests.MessageTypeSubjects
ViciOne.ServiceBus.Tests.MultiBusMessages
ViciOne.ServiceBus.Tests.MyNamespace
ViciOne.ServiceBus.Tests.NoLog
ViciOne.ServiceBus.Tests.ObserverTests
ViciOne.ServiceBus.Tests.PingDefinitions
ViciOne.ServiceBus.Tests.PolymorphicFault_Specs
ViciOne.ServiceBus.Tests.ReceivingObserver_Specs
ViciOne.ServiceBus.Tests.RequestClientMessages
ViciOne.ServiceBus.Tests.Scenario
```

None of these thirteen is one of the 22 subdirectory namespaces, so **no root file of this cohort
declares into a sibling cohort's namespace**. Five of them do carry test identities and are
therefore inside my subset by the stated rule (first segment is not one of the 22):

- `MyNamespace` — 4 identities, `ConsumeObserver_Specs.cs`
- `NoLog` — 1 identity, `ExcessiveAsyncFault_Specs.cs`
- `ObserverTests` — 17 identities, split across `PublishObserver_Specs.cs` (6) and `SendObserver_Specs.cs` (11)
- `ReceivingObserver_Specs` — 8 identities, `ReceiveObserver_Specs.cs`
- `PolymorphicFault_Specs` (the nested namespace) — 0 identities; the tests of that file live in the
  root-namespace class `Test1`, which carries 5 identities.

The remaining eight nested namespaces hold message contracts, consumers and definitions only.

## 6. Files in scope that carry no anchor identity

Five `.cs` files carry no test identity at all. None of them is a silent drop; each is explained:

| File | Explanation |
|---|---|
| `ITestBusConfiguration.cs` | An interface plus two `Scenario` implementations used as a serializer-selection strategy. Support code, no `[Test]`. |
| `ServiceBusExtensions.cs` | One extension method `Type.ToMessageName()` delegating to `MessageUrn.ForType`. Support code. |
| `ServiceProviderExtensions.cs` | `StartHostedServices` / `StopHostedServices` helpers used by `InvalidConfiguration_Specs`. Support code. |
| `Definition_Specs.cs` | Despite the `_Specs` name this file declares only message contracts, a `PingConsumer`, a `SubmitOrderConsumer` and a `ConsumerDefinition`. It contains no `[Test]` method at all. See FINDINGS F-D-01. |
| `NewConfigurationModel.cs` | Excluded from compilation by `<Compile Remove="NewConfigurationModel.cs" />` in the csproj. It is a commented-out design sketch calling a `Bus.Initialize` API that no longer exists. See FINDINGS F-D-02. |

`ViciOne.ServiceBus.Tests.csproj` and `packages.lock.json` are not `.cs` files and carry no
identities by construction; both were read and are in the manifest.

## 7. Deviations

**None.** Anchor and ledger agree in both directions with zero unexplained identities, so nothing was
adopted as a new truth and nothing needs the Lead to dispose a count difference.

Two facts about the anchor itself are reported rather than adopted:

1. The anchor is the recorded result of a clean run, so identities that a run produces only under
   certain conditions (parameterized variant strings, which embed the argument rendering) are pinned
   textually. Three `CronExpressionTest` variant strings contain the literal `...` inside their
   rendered array argument (`[1, 2, 3, 4, 5, ...]`), which any naive "split on the last dot" mapping
   will misparse. This cohort's mapping splits on the last dot **before the first parenthesis**, and
   the 534/534 match is the evidence that this is the correct reading.
2. `MessageUrnSpecs.AttributedMessage_with_symbols` pins a string containing non-ASCII characters.
   The anchor records the identity, not the argument encoding, so this is not a reconciliation
   difference — but it is a rebuild hazard, recorded as FINDINGS F-D-25.

## 8. Ledger totals

| Kind | Rows |
|---|---:|
| Inherited behavioural obligations (`PROPOSED_REPLACED_EXECUTING`) | 467 |
| Gaps found in current product code (`QUESTION`, second ledger entrance) | 13 |
| **Total rows in `LEDGER_DRAFT.jsonl`** | **480** |

All 467 inherited rows are proposed for the `UnitArchitecture` profile; none of them needs a broker,
a database, a cloud service or a container. Environment classification and the cases that are
hermetic-but-not-clean are in `FINDINGS.md` section 2.

## 9. Open questions for the Lead

Carried in `FINDINGS.md`; the ones that change a disposition rather than only the rebuild are
Q-D-01 (`MessageType_Specs.Should_not_allow_array_message_types_but_does`), Q-D-02 (the two
`SendContextMiddleware_Specs` payload cases whose source states the assertions fail), Q-D-03
(`Definition_Specs.cs` and `NewConfigurationModel.cs`) and Q-D-04 (the four duplicate obligation
pairs).
