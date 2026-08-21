# R0-SML anchor reconciliation

Cohort `R0-SML`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.
Worktree measured clean for the whole cohort scope (`git status --porcelain` over `tests/`, `src/`, `tools/`, `build/` returned nothing).

## 1. Derivation of every number below

- **Read set**: `git ls-files <scope>` for the five test projects and the six product projects of the assignment.
  `READ_MANIFEST.tsv` holds `path<TAB>sha256`, sorted `LC_ALL=C`, **933 rows**.
  Verified against `../../BASELINE_TRACKED_FILE_MANIFEST.tsv`: 933 of 933 paths present, **0 hash mismatches, 0 paths absent from the baseline**.
- **Ledger**: `LEDGER_DRAFT.jsonl`, **295 rows**, ids `OBL-R0-SML-0001` .. `OBL-R0-SML-0295`, contiguous, no gaps.
  Derived from reading the test bodies and the owning product code, not generated from the anchor files.
- **Anchor identity counts**: line count of each anchor minus its documentation header (3 lines for
  `abstractions.txt`, `analyzer.txt`, `signalr.txt`; 4 lines for `diagnostics.txt`).
- **Anchor hashes measured now** (all four match the values bound in section 9 of the Lead plan):

  | Anchor | Identities | SHA-256 measured | matches plan |
  |---|---:|---|---|
  | `build/verification/expected/abstractions.txt` | 74 | `3745e3c75c3058852896616907dd4193821d32b8ccdda2c08770dded9f8ff64e` | yes |
  | `build/verification/expected/analyzer.txt` | 115 | `117f1e65313a9a26a62ae07ba2dcebdf61edd70d1936a8555c1e3f1d0534610c` | yes |
  | `build/verification/expected/signalr.txt` | 26 | `664946953234bb1b57a15f93e2c9d8f6e23e6e9b8bbce76a4281405724b72bbf` | yes |
  | `build/verification/expected/diagnostics.txt` | 44 | `d0237bff09f81100692b3cd6899eba779b0299561641abf18499739889493e96` | yes |

## 2. Result in one table

| Project | Files read | Anchor | Anchor identities | Ledger rows | Anchor -> row | Row -> anchor | Anchor without row | Row without anchor |
|---|---:|---|---:|---:|---:|---:|---:|---:|
| `tests/ViciOne.ServiceBus.Abstractions.Tests` | 23 (19 `.cs`, `NewId/texts.txt`, csproj, DotSettings, lock) | `abstractions.txt` | 74 | 78 | 74 / 74 | 74 / 78 | **0** | **4** |
| `tests/ViciOne.ServiceBus.Analyzers.Tests` | 15 (13 `.cs`, csproj, lock) | `analyzer.txt` | 115 | 115 | 115 / 115 | 115 / 115 | **0** | **0** |
| `tests/ViciOne.ServiceBus.SignalR.Tests` | 18 (16 `.cs`, csproj, lock) | `signalr.txt` | 26 | 28 | 26 / 26 | 26 / 28 | **0** | **2** |
| `tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests` | 5 (3 `.cs`, csproj, lock) | `diagnostics.txt` | 44 | 44 | 44 / 44 | 44 / 44 | **0** | **0** |
| `tests/ViciOne.ServiceBus.TestInfrastructure` | 6 (4 `.cs`, csproj, lock) | none (helper project) | n/a | 6 | n/a | n/a | n/a | 6 |
| product-derived gaps (second ledger input, plan section 9) | see section 6 | none | n/a | 22 | n/a | n/a | n/a | 22 |
| **Total** | **933 tracked files in scope** | four anchors | **259** | **295** | **259 / 259** | **259 / 295** | **0** | **36** |

No anchor identity of any of the four anchors is unexplained. No ledger row carries a duplicate
`discoveredCase`. The 36 rows without an anchor identity are each named and explained below.

## 3. Abstractions - 74 / 74, plus 4 rows without an anchor identity

Method census measured on the source: 78 `[Test]` attributes across the ten spec files, of which four sit
inside `#if` regions whose symbols are defined nowhere in the repository. 78 - 4 = 74, exactly the anchor.

Per fixture, anchor identities versus ledger rows:

| Fixture | File | Anchor | Ledger |
|---|---|---:|---:|
| `ExceptionFilter_Specs` | `ExceptionFilter_Specs.cs` | 5 | 5 |
| `Using_the_type_extensions` | `TypeExtensions_Specs.cs` | 2 | 2 |
| `Using_a_new_id` | `NewId/Usage_Specs.cs` | 5 | 5 |
| `Using_the_newid_formatters` | `NewId/Formatter_Specs.cs` | 17 | 17 |
| `When_generating_id` | `NewId/Generator_Specs.cs` | 9 | 9 |
| `Using_the_newid_generator` | `NewId/NewId_Specs.cs` | 9 | 9 |
| `When_interoperating_with_the_guid_type` | `NewId/GuidInterop_Specs.cs` | 20 | 20 |
| `When_getting_a_network_address_for_the_id_generator` | `NewId/NetworkAddress_Specs.cs` | 4 | 4 |
| `Generating_ids_over_time` | `NewId/LongTerm_Specs.cs` | 1 | 1 |
| `Generating_ids_and_preserve_same_order_for_sql_and_ToSequentialGuid` | `NewId/Order_Specs.cs` | 2 | 2 + 4 dormant |

Note that the fixture class of `TypeExtensions_Specs.cs` is named `Using_the_type_extensions`, not
`TypeExtensions_Specs`; the anchor uses the fixture name and so does the ledger.

**The four rows without an anchor identity** (`OBL-R0-SML-0020` .. `OBL-R0-SML-0023`), all in
`NewId/Order_Specs.cs`:

| Row | Symbol | Why it is absent from the anchor |
|---|---|---|
| 0020 | `..._when_using_different_mac_addresses` (sql server) | inside `#if ORDERING_MAC_ADDRESS` |
| 0021 | `..._when_using_different_mac_addresses` (ToSequentialGuid) | inside `#if ORDERING_MAC_ADDRESS` |
| 0022 | `..._when_using_different_processes` (sql server) | inside `#if ORDERING_PROCESS_ID` |
| 0023 | `..._when_using_different_processes` (ToSequentialGuid) | inside `#if ORDERING_PROCESS_ID` |

Neither symbol is defined in any `csproj`, `Directory.Build.props`, `Directory.Build.targets` or on any
command line in the repository, so the four methods never compiled and could never appear in a recorded
run. They carry the obligation "an identifier's ordering follows the worker id / the process id", which
the live suite does not hold anywhere: `Using_the_newid_generator.Should_let_the_process_id_separate_two_otherwise_equal_generators`
asserts that the process id makes identifiers *different*, never that it makes them *ordered*.
Plan section 9 lists compilation success among the reasons that never justify a deletion on their own,
so all four are `QUESTION`, not dropped. **The difference is reported, not adopted.**

## 4. Analyzers - 115 / 115, exact in both directions

Per fixture, anchor identities versus `[Test]` count measured on the source:

| Fixture | File | Anchor | Measured `[Test]` | Ledger |
|---|---|---:|---:|---:|
| `AnalyzerInstanceState_Specs` | `AnalyzerInstanceState_Specs.cs` | 4 | 4 | 4 |
| `Await_Specs` | `Await_Specs.cs` | 5 | 5 | 5 |
| `CancellationToken_Specs` | `CancellationToken_Specs.cs` | 5 | 5 | 5 |
| `DictionaryInitializer_Specs` | `DictionaryInitializer_Specs.cs` | 3 | 3 | 3 |
| `HarnessIntegrity_Specs` | `HarnessIntegrity_Specs.cs` | 3 | 3 | 3 |
| `UnitTest` | `MessageContractAnalyzerUnitTests.cs` | 48 | 48 | 48 |
| `MessageContractAnalyzerWithVariableUnitTest` | `MessageContractAnalyzerWithVariableUnitTest.cs` | 43 | 43 | 43 |
| `MessageDataInitializer_Specs` | `MessageDataInitializer_Specs.cs` | 4 | 4 | 4 |
| **Total** | | **115** | **115** | **115** |

The fixture class of `MessageContractAnalyzerUnitTests.cs` is named `UnitTest`; the anchor uses that name.
No parameterized case exists anywhere in this project - there is no `TestCase`, `TestCaseSource` or
`ValueSource` in the whole cohort except in the Diagnostics project - so one method is one identity throughout.

`MessageContractAnalyzerWithVariableUnitTest` is a strict variable-bound mirror of `UnitTest`: normalising
`_WithVariable_` / `_WithVariables_` out of its 43 names yields a proper subset of the 48 `UnitTest` names.
The five `UnitTest` cases with no mirror are
`WhenActivatingGenericContractAreStructurallyCompatibleAndMissingProperty_ShouldHaveDiagnosticAndCodeFix_1`,
`..._2`, `WhenMessageContractHasNullableAreStructurallyCompatibleAndMissingCaseNullableProperty_ShouldHaveDiagnosticAndCodeFix`,
`WhenMessageDataTypesAreStructurallyCompatible_ShouldHaveNoDiagnostics` and
`WhenNotUsingViciOneServiceBusSymbols_ShouldNotInterfere`. That asymmetry is a coverage observation for the
rebuild, not an anchor deviation.

## 5. SignalR - 26 / 26, plus 2 rows without an anchor identity

| Fixture | File | Anchor | Active `[Test]` | Ledger |
|---|---|---:|---:|---:|
| `HubLifeTimeManagerTests` | `HubLifeTimeManagerTests.cs` | 15 | 15 (plus 2 commented out) | 15 + 2 dormant |
| `ScaleoutHubLifetimeManagerTests` | `ScaleoutHubLifetimeManagerTests.cs` | 10 | 10 | 10 |
| `ViciOneServiceBusHubLifetimeManagerTests` | `ViciOneServiceBusHubLifetimeManagerTests.cs` | 1 | 1 | 1 |

A naive `grep -c "\[Test\]"` on `HubLifeTimeManagerTests.cs` reports 17; two of those are `//[Test]` inside
the commented-out block at the end of the file. 15 active is the anchor number.

**The two rows without an anchor identity** (`OBL-R0-SML-0209`, `OBL-R0-SML-0210`), both in
`HubLifeTimeManagerTests.cs`: `SendGroupExceptAsyncDoesNotWriteToExcludedConnections` and
`SendConnectionAsyncWritesToConnectionOutput`, commented out in full together with a commented-out
`TestConsumer` class. Their bodies use `Assert.NotNull`, `Assert.Equals` and `Assert.Null`, which no longer
compile against the current NUnit, which is presumably why they were commented rather than fixed. Both are
substantially supplanted by cases that do run (`ExcludedConnectionShouldNotReceiveSendGroupMessage` and
`ConnectionShouldReceiveMessage`), but "an empty method body" and "does not compile" are not deletion
reasons under plan section 9, so both are `QUESTION`. **The difference is reported, not adopted.**

The six `Utils/*` and six `OfficialFramework/*` files of this project carry no test identity: they are the
fixture base classes, the consumer factories and the imported ASP.NET Core test client. They are read in
full and their effects are recorded in the `oldExecutionState` field of every row of the project.

## 6. Diagnostics - 44 / 44, exact in both directions

| Fixture | File | Anchor | Ledger | Shape |
|---|---|---:|---:|---|
| `Reading_the_command_line` | `CommandLine_Specs.cs` | 11 | 11 | 11 `[Test]` |
| `Reading_a_ledger_of_expected_identities` | `MessageSequenceLedger_Specs.cs` | 10 | 10 | 10 `[Test]` |
| `Bringing_the_consumer_to_a_standstill_before_the_snapshot` | `ObservationBoundary_Specs.cs` | 11 | 11 | 6 `[Test]` + 1 method with 5 `[TestCase]` |
| `Writing_the_result_where_the_caller_asked_for_it` | `ObservationBoundary_Specs.cs` | 12 | 12 | 12 `[Test]` |

This is the only project in the cohort with a parameterized case. `Should_name_the_verdict_of_a_run` carries
five `[TestCase]` attributes, each with an explicit `TestName`; the anchor records the five `TestName`
values and not the method name, so the ledger resolves the method into five rows whose `discoveredCase`
is the `TestName` and whose `variants` list the three booleans:

| `discoveredCase` (anchor line) | `variants` | expected verdict |
|---|---|---|
| `nothing arrived and nothing stood still` | allSeen=false, quiesced=false, exact=false | `timeout` |
| `not everything arrived` | allSeen=false, quiesced=true, exact=true | `timeout` |
| `looked exact, but was read against live handlers` | allSeen=true, quiesced=false, exact=true | `inconclusive` |
| `everything arrived and the set was not exact` | allSeen=true, quiesced=true, exact=false | `invalid` |
| `everything arrived, stood still, and was exact` | allSeen=true, quiesced=true, exact=true | `exact` |

**All 44 are proposed `PROPOSED_REPLACED_EXECUTING`, owner `Tools`, target
`tests2/Tools/ViciOne.ServiceBus.Diagnostics.Tests`, profile `UnitArchitecture`.** Every one of them is
hermetic today: none starts a broker, none reads an environment variable, none starts a process. The three
that touch the file system use a `Guid.NewGuid()` path under the temporary directory and clean up
(`TemporaryFile` implements `IDisposable`; two others delete their directory in a `finally`).

**No anchor identity of `diagnostics.txt` may be `DIAGNOSTIC_ONLY`.** The two `DIAGNOSTIC_ONLY` rows are
`OBL-R0-SML-0266` (`BusLifecycleScenario.Run`, the `bus-lifecycle` scenario) and `OBL-R0-SML-0267`
(`PublishLoadScenario.Run`, the `publish-load` scenario). They are product-derived rows, not anchor rows;
the justification is in `FINDINGS.md` section 3.

## 7. TestInfrastructure - no anchor, six rows

`tests/ViciOne.ServiceBus.TestInfrastructure` maps to none of the eleven inherited anchors, correctly: its
`csproj` sets `IsTestProject=false`, it declares no test and no runner collects it. Its four C# files carry
capabilities, not identities. The behavioural obligations *of* those capabilities do execute today, but
they live in `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests/RunnerContract_Specs.cs`, which belongs
to the `sql-transport` anchor and therefore to a different R0 cohort. **Cross-cohort note for the
integrator: the disposition of these four files is decided here, but the tests that hold their semantics
must be reconciled by the sql-transport cohort.**

The six rows are `OBL-R0-SML-0268` .. `OBL-R0-SML-0273`; the per-file decision is in `FINDINGS.md` section 5.

## 8. The 22 product-derived rows

`OBL-R0-SML-0274` .. `OBL-R0-SML-0295` are the 22 gap rows discovered from the current product code
(the two `DIAGNOSTIC_ONLY` scenarios are `0266`/`0267`, the six TestInfrastructure dispositions are
`0268`..`0273`). The 22 have no anchor identity by construction - plan section 9 requires the
ledger to carry "aus dem aktuellen Produktcode/Contract neu erkannte Testluecken" as its second input, and
"keine fehlende alte Testmethode befreit ein beobachtbares aktuelles Produktverhalten von einer Disposition".
All 22 are `QUESTION` pending Lead disposition and are listed in `FINDINGS.md` section 6.

## 9. Disposition summary

| Disposition | Rows |
|---|---:|
| `PROPOSED_REPLACED_EXECUTING` | 256 |
| `QUESTION` | 37 |
| `PROPOSED_DIAGNOSTIC_ONLY` | 2 |
| **Total** | **295** |

No row is `REMOVED_WITH_PRODUCT_CAPABILITY`: no product capability of this cohort was removed with a
decision and a removal record, so that disposition is not available to any row here.
No row is `BENCHMARK_ONLY`: this cohort owns no benchmark.
No row is `REPLACED_EXECUTING`: in R0 no new test exists yet, per the cohort reading rules section 5.

## 10. Open reconciliation questions for the Lead

1. The four dormant `Order_Specs` obligations (worker-id ordering, process-id ordering): is ordering by
   worker id and by process id a product guarantee of `NewIdGenerator`, or was it deliberately abandoned?
2. The two commented-out SignalR obligations: confirm they are covered in substance by the running cases,
   or name them as rebuild work.
3. Whether the nine `Usage/**` compile-only sample files of the Abstractions test project are a
   compile-surface obligation to be restated in the new architecture, or are dropped with a reason.
4. `MCA0002` is declared and documented but never reported anywhere in the product (see `FINDINGS.md`
   section 4.1). Two anchor identities pin its absence. This must be decided before those two cases are
   rebuilt, because their names would otherwise carry the old confusion forward.
