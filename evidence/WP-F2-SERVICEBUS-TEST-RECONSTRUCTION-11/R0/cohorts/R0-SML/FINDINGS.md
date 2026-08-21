# R0-SML findings

Cohort `R0-SML`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`, baseline
`ae73c6da748e3bc3257dffa4971ee8680e086207`. Every statement below names the file it was read from.
Where a number appears, its derivation is named. Nothing here is a guess unless it says so.

---

## 1. Quality obligations for the rebuild (TLP-011)

Each item is a concrete construct found in the read set, with the rule of plan section 11 it collides with.

### 1.1 Fixed waits used as synchronisation - 8 sites

| Site | Construct | Why it matters |
|---|---|---|
| `tests/ViciOne.ServiceBus.SignalR.Tests/HubLifeTimeManagerTests.cs` `DisconnectConnectionRemovesConnectionFromGroup` | three `await Task.Delay(2000)` | six seconds per run, and the case's **only** assertion is `client.TryRead() is null`. Shortening or removing the sleeps cannot make it fail; it would pass for a manager that never delivers anything at all. This is a fixed wait **and** an unfalsifiable negative in one case. |
| same file, `AssertNoMessageAsync(client, milliseconds = 1000)` | proves a negative by waiting one second for a `TimeoutException` | used by four cases: `ConnectionShouldNotBeReceivedIfConnectionIdCasingIsOff`, `ExcludedConnectionShouldNotReceiveSendAllMessage`, `ExcludedConnectionShouldNotReceiveSendGroupMessage` (and the helper itself). Four seconds of wall clock per run and a race on a loaded machine. |
| `tests/Tools/.../MessageSequenceLedger_Specs.cs` `Should_report_a_timeout_rather_than_an_answer_about_the_messages` | 200 ms real budget | bounded and diagnostic, but wall-clock driven; should go through `TimeProvider`. |
| same file, `Should_raise_cancellation_instead_of_reporting_an_incomplete_set` | `CancelAfter(100 ms)` | same. |
| `tests/Tools/.../ObservationBoundary_Specs.cs` `Should_not_report_a_standstill_when_the_stop_swallowed_its_own_cancellation` | 200 ms budget against `Task.Delay(Infinite)` | same. |
| same file, `Should_not_report_a_standstill_when_the_stop_raised_its_cancellation` | 200 ms budget | same. |
| same file, `Should_observe_for_the_whole_window_before_it_asks_for_the_stop` | 300 ms window measured with `Stopwatch`, asserted with `Is.GreaterThanOrEqualTo` | asserts an **ordering** property through **elapsed time**. The order (observe, then stop, then read) is directly observable as a call sequence and should be asserted that way. |

`tests/ViciOne.ServiceBus.SignalR.Tests/OfficialFramework/TaskExtensions.cs` additionally uses
`Task.Delay` as its timeout mechanism with a `DefaultTimeout` of 5000 ms - that is a legitimate bounded
timeout, but see 1.4 for its leaks.

### 1.2 Assertion-free success paths - 3 cases

- `NewId/NetworkAddress_Specs.cs` `Should_pull_all_adapters` - enumerates adapters, writes them to the console, asserts nothing. Touches no ViciOne.ServiceBus code at all.
- `NewId/NetworkAddress_Specs.cs` `Should_pull_using_net` - re-implements the product's adapter filter in the test, writes the result to the console, asserts nothing.
- `NewId/GuidInterop_Specs.cs` `Should_display_sequentially_for_newid` - calls `ToString("DS")` and writes it to the console. The `DS` format specifier has no expected value anywhere in the suite.

All three are ledger `QUESTION`. Plan section 9 forbids "no assertion" as the sole deletion reason, so
none is dropped here.

### 1.3 Assertions that do not cover the case name, or that are strictly weaker than a sibling

- `ScaleoutHubLifetimeManagerTests.WritingToRemoteConnectionThatFailsDoesNotThrow` - the "does not throw" half is implicit; there is no `Assert.DoesNotThrow` and no observation that the swallowed write failure was logged. The case cannot distinguish "swallowed and reported" from "silently dropped".
- `HubLifeTimeManagerTests.InvokeUserSendsToAllConnectionsForUser` - creates and connects a third client for `userB` and never reads it. A manager that broadcast to every user would pass.
- `DictionaryInitializer_Specs.WhenTypesAreStructurallyCompatible_ShouldHaveDiagnostic` - name says a diagnostic is expected; the body asserts none.
- `UnitTest.WhenTypeNotValidStructure_ShouldHaveDiagnostic_ButItLooksGoodToMe` and its `WithVariable` mirror - the name admits the contradiction in its own suffix. See 4.1.
- `Using_the_newid_generator.Should_be_able_to_determine_lower_id` - `Is.LessThanOrEqualTo` on exactly the pair that `Should_be_able_to_determine_greater_id` already asserts with `Is.LessThan`. It cannot fail unless that one does.
- `Reading_the_command_line.Should_refuse_an_option_followed_by_another_option` and `Should_refuse_a_number_that_is_not_a_number` - assert only the exception **type** while every sibling refusal case also asserts the message that makes the refusal actionable.
- `Writing_the_result_where_the_caller_asked_for_it.Should_report_an_unknown_scenario_without_a_sink_it_never_read` - asserts only exit code 1; the structured failure it writes to stdout is not asserted.
- `ExceptionFilter_Specs.Should_include_the_parameter_name_in_the_exception` - asserts a property of the BCL `ArgumentNullException`, not of ViciOne.ServiceBus. It exists only to justify the sibling case that passes a paramName where a message is meant.

### 1.4 Unobserved tasks, undisposed resources, sync-over-async

- `SignalR.Tests/OfficialFramework/TaskExtensions.cs`, both `OrTimeout` overloads: `var cts = new CancellationTokenSource();` is **never disposed**, and `cts.Cancel()` is only reached on the success path. On the timeout path the method throws `TimeoutException` and leaves both the `CancellationTokenSource` and its `Task.Delay` continuation alive. Every timeout-proving case in the cohort (four of them, see 1.1) leaks one per run.
- The same file changes its own semantics under a debugger: `Task.Delay(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : timeout, ...)`. A case that proves a negative through a timeout does not prove it at all when a debugger is attached.
- `Analyzers.Tests/Helpers/DiagnosticVerifier.Helper.cs`, `Verifiers/CodeFixVerifier.cs`, `Helpers/CodeFixVerifier.Helper.cs`: every Roslyn call is sync-over-async - `GetCompilationAsync().Result`, `GetAnalyzerDiagnosticsAsync().Result`, `GetSyntaxTreeAsync().Result`, `GetSemanticModelAsync().Result`, `GetOperationsAsync(...).Result`, `RegisterCodeFixesAsync(context).Wait()`, `Simplifier.ReduceAsync(...).Result`, `GetSyntaxRootAsync().Result`. The target architecture keeps this layer (plan section 5), so it must be rebuilt async end to end.
- `DiagnosticVerifier.GetSortedDiagnosticsFromDocuments` has an inner `for` loop over all documents that calls `document.GetSyntaxTreeAsync().Result` for every diagnostic and adds the diagnostic once per matching tree; with one document it is merely wasteful, with several it can add the same diagnostic more than once.

No `async void` exists anywhere in the cohort. Measured: `grep -rn "async void"` over the five test
projects and the six product projects returns nothing.

### 1.5 Swallowed exceptions

Three in **product** code, each reached by the tests but never asserted on:

- `ViciOneServiceBusHubLifetimeManager.AddToGroupAsync` / `RemoveFromGroupAsync`: `catch (RequestTimeoutException e) { LogContext.Warning?.Log(...); }` - a group membership that silently did not happen.
- `AllConsumer.Handle`, `ConnectionConsumer.Handle`, `GroupConsumer.Handle`, `UserConsumer.Handle`: `catch (Exception e) { LogContext.Warning?.Log(e, "Failed to write message"); }`. `WritingToGroupWithOneConnectionFailingSecondConnectionStillReceivesMessage` and `WritingToRemoteConnectionThatFailsDoesNotThrow` inject the failure and assert only the surviving path.
- `DiagnosticVerifier.CreateFrameworkReferences`: `catch (BadImageFormatException)` with an empty body and a comment. Correct here (native libraries share the runtime directory), and the method does assert `references.Count > 0` afterwards, so the swallow cannot silently produce an empty set.

One in **test** code: `ObservationBoundary_Specs`, `Should_not_report_a_standstill_when_the_stop_swallowed_its_own_cancellation` deliberately swallows an `OperationCanceledException` inside the injected stop. That is the fault injection itself and is correct.

### 1.6 Shared mutable state

- **`NewId` process-wide static generator.** `src/ViciOne.ServiceBus.Abstractions/NewId/NewId.cs` holds `static INewIdGenerator? _generator`, `static ITickProvider? _tickProvider`, `static IWorkerIdProvider? _workerIdProvider` and a `static SpinLock`, with four public setters. Nine cases of the Abstractions cohort call `NewId.Next()`, `NewId.Next(2)`, `NewId.NextGuid(2)` or `NewId.NextSequentialGuid()` and therefore share one sequence across the whole assembly. `NewId_Specs.cs` documents this in its XML comments: it deliberately builds local generators "because setting the process wide tick provider needs the shared generator reset for it to take effect, and that reset moves the sequence every other case in this assembly would share". The rebuild must give each case its own generator or own the shared one through a non-parallel collection with a guaranteed reset.
- **`When_generating_id`** mutates its own fixture field `_processIdProvider` inside `Should_not_match_when_generated_from_two_processes`, relying on `[SetUp]` to rebuild it for every case. NUnit creates one fixture instance per fixture by default, so this is per-case-order-sensitive state.
- **`Using_the_newid_formatters`** loads `NewId/texts.txt` in its **constructor** into a `readonly Dictionary` field. A missing or unparsable file fails all 17 cases at once with a constructor exception rather than a named cause.
- **`SignalR.Tests/OfficialFramework/TestClient.cs`** holds `static int _id`, incremented with `Interlocked` per client - safe, but process-global, so a claim value is not reproducible across runs.
- **`SignalR.Tests/OfficialFramework/MemoryBufferWriter.cs`** holds `[ThreadStatic] static MemoryBufferWriter _cachedInstance` with a `Get`/`Return` protocol and a `#if DEBUG` in-use guard. It is used exactly once, in `TestClient.ConnectAsync`, in a `try`/`finally`.
- **`ViciOneServiceBusHubLifetimeTestFixture`** derives its `ServerName` prefix from `Environment.MachineName`, so the server name a case asserts against carries machine identity.

### 1.7 Environment variables and secrets read directly

- **Test code: none.** No test method in the five test projects calls `Environment.GetEnvironmentVariable`, reads a user secret, or reads a configuration file for credentials. `Environment.MachineName` (see 1.6) is machine identity, not configuration.
- **`tests/ViciOne.ServiceBus.TestInfrastructure/TestRunnerContract.cs`** reads eight environment variables directly (`VICIONE_SERVICEBUS_MSSQL_HOST/PORT/USER/PASS`, `VICIONE_SERVICEBUS_PG_HOST/PORT/USER/PASS`). This is the shared helper, not a test, and it is the *sanctioned* single place today - but plan section 7 names a different owner (`ViciOne.ServiceBus.Testing/Infrastructure/Configuration/`) and a different namespace (`VICIONE_TESTS__...`), so it is rebuilt, not moved. See section 5.
- **`tools/diagnostics/.../RunScopedBroker.cs`** reads five environment variables directly. This is tool code, deliberately fail-closed, and it is correct that it refuses rather than defaults. The hermetic case for that refusal does not exist yet (section 6).
- No hard-coded credential of any kind appears in the read set. `DatabaseEndpoint.ToString()` deliberately omits the password; `TestRunnerContractException` messages name only variable names.

### 1.8 Tautologies

None found. Measured by reading every assertion in the cohort: no assertion compares a value with itself,
and no assertion re-asserts the literal it just constructed without a product call in between.

---

## 2. Per-project semantic notes

### 2.1 Abstractions

- 9 of the 19 C# files (`Usage/**`) contain no test at all. They are compile-only samples that implement `IConsumer<T>`, `ISaga` + `InitiatedByOrOrchestrates<T>`, `IActivity<TArguments,TLog>`, `ConsumerDefinition<T>`, `SagaDefinition<T>` and `ActivityDefinition<,,>` and use `ExcludeFromTopologyAttribute`, `CorrelatedBy<Guid>`, `InVar.Timestamp`, `context.Publish`, `context.RespondAsync`, `context.IsResponseAccepted<T>()` and `Headers.Set`. Their obligation is "these public shapes stay implementable from a consumer's position", and it is held only by the fact that the project compiles. That obligation must be restated explicitly in the new architecture (`OBL-R0-SML-0280`).
- `Formatter_Specs` is the only genuinely data-driven fixture: 11 cases x 549 recorded vectors from `NewId/texts.txt` (measured: 12 JSON keys, 549 entries each; 11 encoding columns and one `Guids` column, and there are exactly 11 `Should_compare_known_conversions_*` cases, so every column is used). The loop is hand written rather than `TestCaseSource`, so a failure names the fixture and not the failing vector index.
- Two cases perform a full 1024-element ordering scan (`Order_Specs`) and one performs a 1024-element scan with a synthetic accelerating clock (`LongTerm_Specs`); two more each generate 200 000 identifiers (`NewId_Specs`). That is the runtime cost profile of this project.

### 2.2 SignalR

- All 26 cases are end-to-end through `InMemoryTestHarness`; there is not one direct unit case for `ViciOneServiceBusSubscriptionManager`, `ConcurrentHashSet`, `SerializedHubMessageExtensions`, `DependencyInjectionHubLifetimeScopeProvider`, `ViciOneServiceBusSignalRConfigurationExtensions` or any of the six consumer definitions.
- **The `ProjectReference` to `src/ViciOne.ServiceBus.TestFramework` in `ViciOne.ServiceBus.SignalR.Tests.csproj` is unused.** Measured: `grep -rn "TestFramework" tests/ViciOne.ServiceBus.SignalR.Tests/` returns exactly one hit, the `ProjectReference` line itself. No file uses the `ViciOne.ServiceBus.TestFramework` namespace and no TestFramework type (`AsyncTestFixture`, `BusTestFixture`, `InMemoryTestFixture`, `InMemoryContainerTestFixture`, `IntentionalTestException`, `MediatorTestFixture`, `ActivityTestContext`) appears anywhere in the project. The harness types the fixtures actually use - `BusTestHarness`, `InMemoryTestHarness`, `ConsumerTestHarness<T>`, `IReceivedMessage<T>`, `Harness.SubscribeHandler<T>` - come from `src/ViciOne.ServiceBus/Testing/**`, namespace `ViciOne.ServiceBus.Testing`, which is Core product code. So this cohort does **not** block on the TestFramework resolution of plan section 10; it blocks on where `src/ViciOne.ServiceBus/Testing/**` ends up.
- **Name collision to decide before `tests2/Testing/ViciOne.ServiceBus.Testing/` is created.** That new project name is already taken by a namespace inside the shipped product: `src/ViciOne.ServiceBus/Testing/**` declares `namespace ViciOne.ServiceBus.Testing`, and four SignalR test files reach it with a bare `using Testing;`. `tools/diagnostics/.../BusLifecycleScenario.cs` and `RunScopedBroker.cs` reach the same namespace with `using ViciOne.ServiceBus.Testing;` to get `RabbitMqTestHarness`. A new assembly of the same name would put two `ViciOne.ServiceBus.Testing` namespaces in the graph of every test project that references both.
- `SignalR.Tests/OfficialFramework/**` (6 files, 848 lines) is imported ASP.NET Core test infrastructure: `TestClient`, `HubConnectionContextUtils` (including the `MockHubConnectionContext` whose `WriteAsync` always throws - the fault injector), `DuplexPipe`, `MemoryBufferWriter`, `MockHubProtocolResolver`, `TaskExtensions`. Large parts are unused by this cohort: `TestClient.ConnectAsync`, `StreamAsync`, `InvokeAsync`, `SendInvocationAsync`, `SendStreamInvocationAsync`, `TickHeartbeat`, `Connected`, `HandshakeResponseMessage`; `MemoryBufferWriter`'s entire `Stream` surface; `TaskExtensions.OrThrowIfOtherFails`. Only `TestClient.ReadAsync`, `TryRead`, `Connection`, `Dispose` and `TaskExtensions.OrTimeout` are actually reached.

### 2.3 Diagnostics

- The tool is unusually well factored for testability, and deliberately so: `Program.Report` and `Program.Deliver` take `TextWriter? output, TextWriter? error` **specifically** so a case can observe where the result went. The XML comment records the measurement that produced that seam: "They defaulted to the real console, so a case could observe that this method did not throw and nothing else... Measured - removing the line that writes the result left the whole fixture passing." That is exactly the mutation probe plan section 11 requires, already performed and recorded.
- `Program.HandleCancellation` takes subscribe/unsubscribe delegates for the same reason: so the handler lifecycle can be asserted without touching the real console of the test process.
- `AssemblyInfo.cs` grants `InternalsVisibleTo("ViciOne.ServiceBus.Diagnostics.Tests")` with the comment "Visibility is not a substitute for testability". The rebuilt project keeps the same assembly name or the grant must move with it.

---

## 3. The two `DIAGNOSTIC_ONLY` candidates

**They are `bus-lifecycle` (`BusLifecycleScenario.Run`) and `publish-load` (`PublishLoadScenario.Run`) -
and neither is one of the 44 anchor identities.**

### 3.1 Why these two and no others

Four independent properties hold for both and for nothing else in this cohort:

1. **They cannot run hermetically.** Both begin with `await RunScopedBroker.CreateVirtualHost("test", cancellationToken)`, which reads `RabbitMqTestHarness.HostVariable`, `PortVariable`, `UsernameVariable`, `PasswordVariable` and `ManagementPortVariable` from the environment and raises `InvalidOperationException` when any is absent: *"is not set, so this scenario does not know which broker to measure... There is no default host, port or account to fall back to."* `PublishLoadScenario.Run` then builds a real bus with `Bus.Factory.CreateUsingRabbitMq` against that broker. Neither can enter the `UnitArchitecture` profile, and neither belongs in `LocalIntegration` either, because of properties 2 and 3.
2. **They are measurements, not verdicts.** `BusLifecycleScenario.Run` returns `{ scenario, cycles, sampleEvery, samples }` and asserts nothing; the samples are start / round-trip / stop milliseconds plus `ThreadPool.ThreadCount`, process thread count, `ThreadPool.PendingWorkItemCount` and managed megabytes. `PublishLoadScenario.Run` returns an `outcome` word plus rates and ledger details. `Program.Main` is explicit in its own class comment: *"Neither scenario gates anything: the exit code is non zero only when the scenario could not run at all, never because a number was worse than another number."* A `PASS` from either would be a claim they do not make - which is precisely what `DIAGNOSTIC_ONLY` ("bewusst menschliches Werkzeug, nie Verhalten-PASS") is for.
3. **Their duration forbids a required profile.** Defaults are `--cycles 240` (240 bus start/stop cycles with a 30 s round-trip bound each) and `--messages 100000` (a hundred thousand publishes in flight against a bounded-concurrency consumer, a 3 s drain window, a 30 s stop budget and a 180 s completion limit). `UnitArchitecture` must run complete on every change.
4. **The repository already says so, in words, in two places.** `tools/diagnostics/ViciOne.ServiceBus.Diagnostics/README.md` opens with *"Two scenarios that are started deliberately and gate nothing"* and closes with *"The measurements themselves stay on demand and gate nothing."* Both replaced `[Explicit]` fixtures that sat inside the required RabbitMQ category and therefore never ran: `bus-lifecycle` replaced `BusLifecycleAccumulation_Probe`, `publish-load` replaced `HammerTime_Specs`. Making either an executing case would recreate exactly the situation the tool was built to end.

### 3.2 Why none of the 44 may be `DIAGNOSTIC_ONLY`

The tool was deliberately carved so that every decidable part of both scenarios sits behind an internal
seam that needs no broker:

- `PublishLoadScenario.Quiesce(Func<CancellationToken,Task> stop, TimeSpan budget)` - the stop is a delegate, so 4 anchor cases drive it with injected stops.
- `PublishLoadScenario.ObserveThenQuiesceThenRead(ledger, stop, window, budget, token)` - 2 anchor cases drive the ordering.
- `PublishLoadScenario.Outcome(bool, bool, bool)` - a pure function; 5 anchor `TestCase` variants.
- `MessageSequenceLedger` - 10 anchor cases, entirely in memory.
- `Program.ParseOptions`, `Program.Number`, `Program.Usage` - 11 anchor cases, pure.
- `Program.Report`, `Program.Deliver`, `Program.HandleCancellation`, `Program.Main` - 12 anchor cases, driven through injected `TextWriter`s and temporary files.

Measured: **none of the 44 starts a broker, reads an environment variable, or starts a process.** All 44
are therefore `PROPOSED_REPLACED_EXECUTING`, owner `Tools`, target
`tests2/Tools/ViciOne.ServiceBus.Diagnostics.Tests`, profile `UnitArchitecture`, exactly as the Lead plan
requires. Three of them touch the file system, each under a `Guid.NewGuid()` path in the temporary
directory with cleanup (`TemporaryFile : IDisposable`, and two `finally { Directory.Delete(dir, true); }`);
one obligation for the rebuild is that the cleanup **result** is checked, which it is not today.

### 3.3 The trap to avoid

`RunScopedBroker.Read()` and `RunScopedBroker.ManagementPort()` look like they belong to the two scenarios
and therefore to `DIAGNOSTIC_ONLY`. They do not. They only read environment variables and build messages,
so their whole refusal contract - "no default host, port or account", the exact variable named, the runner
command in the message - is hermetically testable and has **no case at all** today
(`OBL-R0-SML-0290`). Inheriting the scenarios' disposition would silently drop the guarantee that the
diagnostics refuse to measure the wrong broker.

---

## 4. Roslyn compiler fixtures - how they are built today (the layer the target keeps)

Source: `Helpers/DiagnosticVerifier.Helper.cs`, `Helpers/CodeFixVerifier.Helper.cs`,
`Helpers/DiagnosticResult.cs`, `Verifiers/DiagnosticVerifier.cs`, `Verifiers/CodeFixVerifier.cs`.

### 4.1 How a positive (must-report) fixture is built

1. **Source assembly.** Every case concatenates `readonly string` fixture fragments held on the fixture class - `Usings`, `MessageContracts`, `ExtraMessageContracts`, `RecordContracts`, `RecursiveContracts`, `GenericMessageContracts`, `MessageContractsDifferentNamespace`, `Dtos`, `DtosIncompatibe` - with a verbatim string holding the case-specific code. Fragment order fixes the absolute line numbers that the expected `DiagnosticResultLocation` carries, which is why the same logical case has different line numbers in `UnitTest` (e.g. 58) and `MessageContractAnalyzerWithVariableUnitTest`.
2. **Workspace.** `CreateProject` builds an `AdhocWorkspace` solution with one project `TestProject`, adds each source as `Test0.cs`, `Test1.cs`, ..., and adds references:
   - **the entire shared framework directory** - every `*.dll` next to `typeof(object).Assembly.Location`, with `BadImageFormatException` skipped for native neighbours, and `references.Count > 0` asserted. The comment records why: *"A hand-picked subset silently omitted facades such as System.Runtime and System.ComponentModel, which made every test snippet fail to bind with CS0012 and therefore made every analyzer report zero diagnostics."*
   - `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis`;
   - and, unless `includeViciOneServiceBus: false`, `ViciOne.ServiceBus` (via `typeof(Bus)`), GreenPipes (via `typeof(IProbeSite)`) and NewId (via `typeof(NewId)`).
3. **Output kind.** The compilation options are switched to `OutputKind.DynamicallyLinkedLibrary`. The comment records why: *"Analyzer fixtures are type declarations, not programs. The workspace default is a console application, which fails every fixture with CS5001 before any analyzer runs."*
4. **Bind check before the analyzer runs.** `GetSortedDiagnosticsFromDocuments` calls `compilation.GetDiagnostics()`, filters `DiagnosticSeverity.Error`, and if any exist throws `InvalidOperationException` naming the error count and the first five errors. This is the crucial inversion: **a fixture that does not compile fails loudly instead of reporting zero analyzer diagnostics**, which is otherwise indistinguishable from "the analyzer found nothing".
5. **Analysis.** `compilation.WithAnalyzers(ImmutableArray.Create(analyzer))` then `GetAnalyzerDiagnosticsAsync().Result`; results are ordered by `Location.SourceSpan.Start`.
6. **Comparison.** `VerifyDiagnosticResults` compares count first (printing every actual diagnostic on mismatch, formatted as the `GetCSharpResultAt(line, column, Analyzer.RuleId)` call that would produce it), then per diagnostic in order: primary location path/line/column, the number and positions of `AdditionalLocations`, `Id`, `Severity` and the **exact** `GetMessage()` text.

### 4.2 How a negative (must-not-report) fixture is built

Identical construction; the case simply calls `VerifyCSharpDiagnostic(test)` with no expectations, so the
count comparison asserts zero. There are 26 such cases. One variant exists:
`VerifyCSharpDiagnosticWithoutViciOneServiceBus` builds the same project **without** the ViciOne.ServiceBus,
GreenPipes and NewId references, used once by `WhenNotUsingViciOneServiceBusSymbols_ShouldNotInterfere` to
prove the analyzer stays silent in a project that does not reference the product at all.

### 4.3 How a deliberately-failing fixture is built

This is the subject of `HarnessIntegrity_Specs`, the harness's own three-case self-check:

- `Should_fail_loudly_when_a_fixture_does_not_bind` - a source referencing `ThisTypeDoesNotExistAnywhere`; asserts `InvalidOperationException` whose message contains `"compilation reported"` **and** `"CS0246"`. This is the deliberately-failing compilation, and the assertion is on the *harness's* refusal, not on the analyzer.
- `Should_not_report_a_compilation_defect_for_a_binding_fixture` - the control: a minimal valid source must raise nothing.
- `Should_resolve_the_shared_framework_reference_set` - a source touching `System.Uri`, `System.ComponentModel.IServiceProvider` and `System.Threading.Tasks.Task` must bind; the comment names those as exactly the facades the old hand-picked reference set omitted.

There is a second kind of deliberate failure, in `AnalyzerInstanceState_Specs`: a private
`AnalyzerWithCompilationBoundField` class carrying an `ITypeSymbol` field, a
`Dictionary<string, IMethodSymbol>` field and a `string` field, with `#pragma warning disable CS0649`
because the fields exist only for the scan to find. `Should_detect_a_compilation_bound_instance_field`
asserts the scan reports exactly `{ _members, _symbol }`. That is the mutation control for
`Should_hold_no_compilation_bound_state_in_an_analyzer_instance_field`, which would otherwise pass for a
scan that read nothing.

### 4.4 How a code fix is verified

`VerifyFix` creates one document, collects analyzer diagnostics and the pre-existing compiler diagnostics,
then loops: build a `CodeFixContext` for `analyzerDiagnostics[0]`, `RegisterCodeFixesAsync(...).Wait()`,
apply `actions[0]` (or `actions[codeFixIndex]` and stop), re-run the analyzer, and - unless
`allowNewCompilerDiagnostics` - fail if the fix introduced any new compiler diagnostic. Finally
`Simplifier.ReduceAsync` and `Formatter.Format` run and the **whole document** is compared as one string
against the recorded expectation, with `\r\n` normalised to `\n` on both sides.

**Carried into the target, this layer must keep:** the full shared-framework reference set, the
`DynamicallyLinkedLibrary` output kind, the fail-loud bind check, the three harness self-check cases, the
per-diagnostic id/severity/message/line/column comparison, and the deliberately-offending analyzer class
as a scan control. **It must lose:** the sync-over-async (see 1.4), the dead Visual Basic surface
(`VerifyBasicDiagnostic`, `VerifyBasicFix`, `GetBasicDiagnosticAnalyzer`, `GetBasicCodeFixProvider` - no VB
analyzer exists and nothing calls them), and the two never-supplied parameters `codeFixIndex` and
`allowNewCompilerDiagnostics`. **It must decide:** the whole-document string comparison is what plan
section 11 calls an "ueberbreite Snapshotbehauptung". For a code fix it is arguably the right assertion,
but the 20 recorded expectations encode the fix's peculiar leading-comma formatting
(`Quantity = 10\n,\nPrice = default(decimal) }`), so a Roslyn formatter change breaks all 20 at once with
no indication which behaviour changed.

### 4.5 The largest semantic finding of the analyzer cohort

**`MCA0002` (`ValidMessageContractStructureRuleId`) is declared but unreachable.** It appears in
`MessageContractAnalyzer.SupportedDiagnostics`, has a full `DiagnosticDescriptor` with severity `Error`,
and is listed in `AnalyzerReleases.Shipped.md`. Measured: `grep -rn "MCA0002\|ValidMessageContractStructureRule"`
over `src` and `tests` returns three hits, all three inside `MessageContractAnalyzer.cs` - the id constant,
the descriptor, and the `SupportedDiagnostics` array. **`ReportDiagnostic` is never called with it.**
Two anchor identities document the absence in their own names:
`UnitTest.WhenTypeNotValidStructure_ShouldHaveDiagnostic_ButItLooksGoodToMe` and its `WithVariable` mirror.
A consumer sees an announced `Error`-severity rule that can never fire, and an `.editorconfig` severity for
it has no effect. **Lead decision needed before those two cases are rebuilt**, because otherwise their new
names carry the old confusion forward.

---

## 5. `tests/ViciOne.ServiceBus.TestInfrastructure` - per-file disposition

The project is a helper, correctly: `IsTestProject=false`, `IsPackable=false`, no test declared, no anchor.
Its consumers, measured by reference: `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests` and
`tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests` - two owners, so the
"at least two owners need the same framework-neutral capability" rule of plan section 5 is satisfiable.

| File | Decision | Reason |
|---|---|---|
| `TestRunnerContract.cs` | **new `ViciOne.ServiceBus.Testing`** (`Infrastructure/Configuration/`), rebuilt as `TestConfigurationProvider` + `TestConfigurationValidator` | It is the fail-closed configuration reader two owners need, which is exactly the capability plan section 7 assigns to that project. Rebuilt, not moved, for two stated reasons: the target owner is a provider/validator over typed options rather than a static class with two `Lazy` fields, and CI injects through the `VICIONE_TESTS__...` namespace rather than `VICIONE_SERVICEBUS_MSSQL_*`. What must survive verbatim: no default, no probing, no fallback; every missing variable named in ordinal order; the exception raised *before* any connection attempt; no secret in the message. |
| `DatabaseEndpoint.cs` | **split**: the validated value object -> `ViciOne.ServiceBus.Testing` (as `SqlTestOptions`); the three connection-string builders -> the owning provider fixtures | The file mixes two responsibilities that plan section 5 puts on opposite sides of the line. The endpoint value (engine, host, port validated 1..65535, username, password, secret-free `ToString`) is framework-neutral configuration. `SqlServerConnectionString(...)` and `PostgresConnectionString(...)` are database provider logic - they encode `Encrypt=False`, `TrustServerCertificate=True`, `Persist Security Info=False` and Npgsql key names - and section 5 says database logic belongs in the owning fixture, not in `ViciOne.ServiceBus.Testing`. The secret-free `ToString` is a security invariant and keeps an executing case. |
| `TestRunnerContractException.cs` | **new `ViciOne.ServiceBus.Testing`**, beside the validator | A distinct exception type is what lets a case assert "the contract was incomplete" rather than "something threw". Sealed, single message constructor, deliberately no inner-exception or serialization constructor - correct for a test-only failure signal, and it should stay that way. Rename to match the new validator. |
| `TestDatabase.cs` | **dropped** (ledger `QUESTION` - the Lead confirms, because two other cohorts see the names) | Two independent reasons. (a) Plan section 5: the four constants are the domain names of the SQL transport, provisioning, persistence and PostgreSQL persistence suites; a class that couples four unrelated owners through shared name constants is domain knowledge in framework-neutral infrastructure. (b) Plan section 11: "Externe Ressourcen haben eindeutige laufgebundene Namen" - four fixed database names shared by every run are the opposite of run-scoped, and the file's own XML comment documents the damage that caused: the provisioning suite needed a second constant *because dropping a database disconnects every session still attached to it*. The rebuilt owners derive a run-scoped name from the run identity, and nothing framework-neutral remains, so the capability disappears rather than moving. |

Two build artefacts are also disposed: the `csproj` and `packages.lock.json` are replaced by the new
project's files. One observation for the rebuild - `packages.lock.json` resolves a **Direct** dependency on
`GitHubActionsTestLogger 3.0.5` although this `csproj` declares no `PackageReference` and sets
`IsTestProject=false`. The reference is injected from a shared props file and reaches a project that is
explicitly not a test project. `ViciOne.ServiceBus.Testing` must not inherit a test logger.

**Nothing about this project belongs in the non-executable `ViciOne.ServiceBus.Testing.Xunit`.** That
project is the xUnit extension and the completeness sentinel; none of these four files carries any test
framework coupling at all - there is no `using NUnit.Framework` anywhere in the project, which is precisely
why the capability is framework-neutral to begin with.

---

## 6. The 22 product-derived gaps, and where they land

Full detail is in `LEDGER_DRAFT.jsonl`, rows `OBL-R0-SML-0274` .. `OBL-R0-SML-0295`. Summary:

**Abstractions (7)** - the scalar (non-SSSE3) fallback branch of the four Guid-producing generator methods,
never taken on the hardware anything runs on; the sequence rollover at 65535 including the mid-batch
refresh of the cached `a`/`b` bytes; the `index + count > ids.Length` guards on the three array overloads
(and the complete absence of negative-argument guards); the four public static `NewId.Set*` configuration
methods, uncovered *because* of the shared static state described in 1.6; the "no usable adapter"
`InvalidOperationException` of `NetworkAddressWorkerIdProvider`; malformed input to `NewId(string)` and
`NewId(byte[])`; and the compile-only `Usage/**` obligation.

**Analyzers (3)** - `MCA0002` (section 4.5); the analyzer **package** surface, which plan section 5
assigns to this test project ("Analyzer, Codefixes und Paketoberflaeche") and which has no case at all:
the `analyzers/dotnet/cs` package path, the `tools/*.ps1` payload, `DevelopmentDependency`, no `lib`
output, and the hard constraint that the analyzer assembly must **not** reference
`Microsoft.CodeAnalysis.Workspaces` while the code-fix assembly must; and the verifier's dead/unused
surface plus its sync-over-async (1.4). Also unheld: nothing binds `AnalyzerReleases.Shipped.md`
(five rule ids) and `AnalyzerReleases.Unshipped.md` (empty, 0 bytes) to `SupportedDiagnostics` - which is
how `MCA0002` could drift into a documented rule nobody reports.

**SignalR (6)** - the three plural overloads `SendConnectionsAsync` / `SendGroupsAsync` / `SendUsersAsync`
with their empty-list short circuits and the null/empty group-name filter, none of which has any case;
eight `ArgumentNullException` guards, plus the asymmetry that `SendUserAsync(userId)` has none; the
`RequestTimeoutException` swallow in `AddToGroupAsync`/`RemoveFromGroupAsync`; the
`NullReferenceException` in `OnDisconnectedAsync` when `OnConnectedAsync` never ran (the `if (groups != null)`
check that follows is unreachable, because the dereference already happened one line earlier); the
untested utility types including `ViciOneServiceBusSubscriptionManager.RemoveSubscription` leaving an empty
`HubConnectionStore` in its dictionary forever; and the MessagePack leg of `ToProtocolDictionary`, which
every fixture serialises on every send and no case ever reads back.

**Diagnostics (6)** - `RunScopedBroker.Read`/`ManagementPort` (section 3.3); `Program.Main`'s help and
empty-argument exits (2 and 0); the `bus-lifecycle --sample-every > --cycles` refusal that the tool README
lists beside the four refusals that do have cases; the `catch (OperationCanceledException)` branch of
`Program.Main` that reports `status: "cancelled"`; `Snapshot.Summarise`'s sort and 20-element cap, which no
case can observe because no case has more than one entry in any detail list; and the three unlisted rows of
the `Outcome` truth table - `(false,false,true)`, `(false,true,false)`, `(true,false,false)` - all of which
would return their branch value correctly, so they are not defects, but the case set claims to name "the
verdict of a run" and enumerates five of eight.

---

## 7. Questions for the Lead

1. **`MCA0002`**: implement it, or remove it from `SupportedDiagnostics` and from
   `AnalyzerReleases.Shipped.md`? Two anchor identities pin its absence and cannot be rebuilt honestly
   until this is answered.
2. **Worker-id and process-id ordering** (`Order_Specs` `#if` regions, `OBL-R0-SML-0020`..`0023`): is
   ordering by worker id / process id a product guarantee of `NewIdGenerator`, or was it deliberately
   abandoned? The live suite asserts only *difference*, never *order*.
3. **The three assertion-free cases** (`Should_pull_all_adapters`, `Should_pull_using_net`,
   `Should_display_sequentially_for_newid`): rebuild each with a real assertion, or retire with an explicit
   decision? `REMOVED_WITH_PRODUCT_CAPABILITY` is not available - no product capability was removed.
4. **The two commented-out SignalR cases** (`OBL-R0-SML-0209`, `0210`): confirm they are covered in
   substance by the running cases, or name them as rebuild work.
5. **`Usage/**` (9 files)**: restate the compile-surface obligation explicitly in the new architecture
   (e.g. as an architecture case), or drop it with a reason?
6. **`TestDatabase.cs`**: confirm the drop. Two other cohorts (sql-transport, EF Core) consume those
   database name constants and must each derive a run-scoped name instead.
7. **`TestRunnerContract` rename and namespace change**: `VICIONE_SERVICEBUS_MSSQL_*` /
   `VICIONE_SERVICEBUS_PG_*` -> `VICIONE_TESTS__...` per plan section 7 touches the canonical runner
   (`tools/ci/run_broker_category.py`) and the CI job definitions. Confirm the runner change is inside this
   work package.
8. **Cross-cohort**: `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests/RunnerContract_Specs.cs` holds
   the executing obligations of the four TestInfrastructure files disposed here, but belongs to the
   `sql-transport` anchor. The integrator must not let that project's cohort dispose them a second time,
   nor leave them undisposed.
9. **The new `ViciOne.ServiceBus.Testing` project name collides with an existing product namespace.**
   `src/ViciOne.ServiceBus/Testing/**` already declares `namespace ViciOne.ServiceBus.Testing` and owns
   `BusTestHarness`, `InMemoryTestHarness`, `ConsumerTestHarness<T>`, `IReceivedMessage<T>` and
   `RabbitMqTestHarness`. All 26 SignalR obligations and both `DIAGNOSTIC_ONLY` scenarios reach it.
   Two questions follow. (a) Does the new `tests2/Testing/ViciOne.ServiceBus.Testing` take a different
   assembly and namespace name, or does `src/ViciOne.ServiceBus/Testing/**` move into it? (b) The SignalR
   rebuild cannot start before that is answered, because every one of its fixtures resolves those types
   through a bare `using Testing;`. Separately: the `ProjectReference` from
   `ViciOne.ServiceBus.SignalR.Tests.csproj` to `src/ViciOne.ServiceBus.TestFramework` is unused and should
   simply not be carried over.
10. **`Should_observe_for_the_whole_window_before_it_asks_for_the_stop`** asserts an ordering property
    through elapsed wall-clock time. Confirm that the rebuild may change *how* it is proved (a recorded call
    sequence instead of a `Stopwatch`) without that counting as a weakening of the obligation.
