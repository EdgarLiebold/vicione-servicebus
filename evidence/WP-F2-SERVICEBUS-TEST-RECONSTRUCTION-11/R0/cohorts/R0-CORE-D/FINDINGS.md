# R0-CORE-D — findings

Cohort `R0-CORE-D`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`,
baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`.
Scope: the 106 files directly in `tests/ViciOne.ServiceBus.Tests/`, all read completely.
467 inherited obligations across 534 anchor identities, plus 13 gap rows.

Numbers below are counted over the 467 inherited obligation rows of `LEDGER_DRAFT.jsonl`.

---

## 1. Summary of the cohort

These files are the residue that no thematic folder claimed, and it shows: the spread runs from
pure unit tests over reflection helpers (`FastProperty`, `StaticProperty`, `ImplementedTypeCache`,
`TaskExtension`) through cron parsing (`CronExpressionTests`, 58 obligations, 115 identities) to
whole job-service lifecycles (`JobConsumer_Specs`, `JobServiceLifecycle_Specs`,
`RecurringJobConsumer_Specs`) that run a saga state machine, a delayed scheduler and several
minutes of wall-clock time in memory.

Quality is bimodal. A clearly newer group is exemplary — it states the contract in a doc comment,
carries its own control case against a vacuous pass, and explains why the arrangement is the only
decidable one: `AwaitSemantics_Specs`, `PipeContextFailure_Specs`, `InMemoryOutboxLifecycle_Specs`,
`InMemoryOutboxDirectPath_Specs`, `TestTimeoutBudget_Specs`, `Threading_Specs`,
`ExcessiveAsyncFault_Specs`, `SerializationFault_Specs` (second fixture), `NoLicensingResidue_Specs`,
`NoOutboundVendorCall_Specs`, `EmittedMetrics_Specs`, `Telemetry_Specs`,
`JobServiceEndpointConfiguration_Specs`, `ServiceInstanceEndpoints_Specs`, `SimpleConfiguration_Specs`,
`HostConfigurationRetry_Specs`, `DynamicProxySerialization_Specs`, `JobAttemptGeneration_Specs`,
`MessageFlow_Specs`, `Batch_Specs` (the grouping and exactly-once fixtures).

The inherited group is much weaker: 66 obligations assert nothing at all beyond awaiting a task,
several names claim the opposite of what their assertion demands, and process-global state is
mutated and never restored.

---

## 2. Environment classification

**Every one of the 467 inherited obligations is proposed for the `UnitArchitecture` profile.**
Nothing in this cohort needs a broker, a database, a cloud service, an emulator or a container;
the transport is always the in-memory one and every harness is created inside the test process.

That said, hermetic is not the same as clean. The following obligations are hermetic but couple to
something outside the test, and must be corrected rather than carried over:

### 2.1 Host time zone and host culture — `CronExpressionTests.cs`

The plan (section 8) sets `TZ=UTC` before process start. That makes many of these cases pass, but it
also means their expected values are only correct because of a process-level setting the test itself
never states.

- `TestIsSatisfiedBy`, `TestLastDayOffset`, `TestCronExpressionPassingMidnight`,
  `TestCronExpressionPassingYear`, `TestMonthShift`, `TestGetTimeAfter_QRTZNET149`,
  `CanUse_DayOfMonth_And_DayOfWeek_Together`, `CanUseLastDayOfMonthInArray`,
  `LastWeekDayWithOffset`, `TestNthWeekDayPassingMonth`, `TestCorrectWeekFireDays` (six callers)
  build local `DateTime` values and call `ToUniversalTime()`, so the expected instants are the host
  zone's.
- `TestQRTZNET152_Nearest_Weekday_Expression_W_Does_Not_Work_In_CronTrigger` builds its expected
  value from `TimeZoneUtil.GetUtcOffset(d, TimeZoneInfo.Local)` — the expectation is derived from
  the same host state as the answer.
- `TestDaylightSavingsDoesNotMatchAnHourBefore` and `...2` resolve the **Windows** time zone id
  `"Eastern Standard Time"`; on Linux/macOS this only works through the ICU alias table.
- `Ensure_NthWeek_Day_IsBetween1And7` uses `DateTime.Parse(shouldSatisfyDate)` with no culture and
  then `new DateTimeOffset(dt)` with no offset.
- `TestCronExpressionParsingIncorrectDayOfWeek` builds its input from `DateTime.Now.Year` and
  `TestCronExpressionWithExtraWhiteSpace` from `DateTime.UtcNow.Date`, so the literal under test
  changes with the calendar.
- The static field `TestTimeZone = TimeZoneInfo.Local` is declared and never used.

The one daylight case that is genuinely platform independent is
`Should_carry_a_daily_time_across_the_spring_daylight_saving_jump`, which builds its zone with
`TimeZoneInfo.CreateCustomTimeZone`. It is the pattern the rest should follow, and it is already in
the file.

### 2.2 Filesystem and process-global listeners

- `NoLicensingResidue_Specs` and `NoOutboundVendorCall_Specs` read the product assemblies from disk
  with `File.ReadAllBytes(assembly.Location)`. Correct today, but it breaks under a single-file or
  in-memory host and it is a filesystem dependency in a unit profile.
- `NoOutboundVendorCall_Specs.Should_not_issue_a_single_outgoing_http_request` subscribes to
  `DiagnosticListener.AllListeners`, which is process-wide: any parallel test that issues an HTTP
  request would be attributed to this one.
- `NoOutboundVendorCall_Specs.Should_notice_an_outgoing_request_when_there_is_one` opens a real
  socket to `http://127.0.0.1:1`. Local, bounded to 250 ms, and deliberately swallowed — but it is
  still an outbound connection attempt from a hermetic test.
- `Telemetry_Specs` and `EmittedMetrics_Specs` install a process-global `ActivityListener` /
  `MeterListener` in `[SetUp]` and dispose it in `[TearDown]`. Correctly scoped, but they observe
  every activity and instrument in the process while alive, so they cannot run in parallel with
  anything else that emits on the same source.

### 2.3 Process-global exception handlers, never removed — `RequestClient_Specs.cs`

`Sending_a_request_to_a_missing_service_that_times_out`,
`Sending_a_request_using_mediator_to_a_missing_service_that_times_out` and
`Sending_a_request_using_mediator_that_faults` each subscribe to
`AppDomain.CurrentDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` and
**never unsubscribe**. Three consequences:

1. the handlers survive into every later test in the process and keep appending to lists whose
   owning test has finished;
2. the third one calls `eventArgs.SetObserved()` inside the global handler, which changes the
   behaviour of every other unobserved task exception in the run;
3. running any of the three in parallel with another faulting test attributes foreign exceptions to
   this assertion.

---

## 3. TLP-011 quality defects, by class

### F-D-03 — assertion-free obligations (66 of 467)

66 obligations have no assertion at all: they await a task, or call a method, and the only failure
mode is a cancellation or an exception. They are listed in full in the ledger (`assertionIntent`
begins with "None"). The heaviest clusters:

| File | Assertion-free obligations |
|---|---:|
| `SendByConvention_Specs.cs` | 5 (all of them) |
| `SendReceive_Specs.cs` | 4 |
| `EventPublish_Specs.cs` | 4 (all of them) |
| `InMemoryTest_Specs.cs` | 4 (all of them) |
| `ReceiveObserver_Specs.cs` | 4 |
| `PublishObserver_Specs.cs` | 3 |
| `InterceptingConsumer_Specs.cs` | 3 (all of them) |
| `Enrichment_Specs.cs` | 3 (all of them) |
| `PublishSubscribe_Specs.cs` | 3 |

Not a deletion reason (rules §4), and the obligation is real in almost every case. But most of
these fail as "the test timed out" rather than as a sentence, and several claim something the
awaited task cannot establish. Named examples:

- `InterceptingConsumer_Specs.Should_call_the_interceptor_first` / `..._second`: awaiting a
  completion source called `First` says nothing about ordering. Ordering is the whole obligation.
- `EventPublish_Specs.Should_publish_first_event` … `Should_publish_fourth_event`: the ordinals in
  the four names are never asserted; each test only awaits its own event.
- `InMemoryTest_Specs.Should_start_the_handler_properly` has an **empty body**.
- `Cancellation_Specs.Should_not_produce_a_fault_on_shutdown` states a negative ("no fault") that
  nothing checks — a published `Fault<PingMessage>` would leave it green. This is the one
  assertion-free case in the cohort where the missing assertion is the entire point of the test.
- `InMemoryDuo_Specs.Should_keep_em_separated` asserts only that the relay delivered; it never
  asserts that the message did **not** arrive without the relay, so a shared transport would pass.
- `TypeCastRetry_Specs.Should_receive_the_message` configures an exponential retry policy that
  nothing ever triggers.

### F-D-04 — fixed sleeps and fixed windows used as synchronisation (18 obligations)

`Task.Delay` / `WaitOne` / `Wait(timeout)` used to decide a result rather than to observe one:

| Obligation | Wait |
|---|---|
| `JobServiceLifecycle_Specs` — 4 negative cases | `Task.Delay(250)` as the "still not completed" window |
| `JobServiceLifecycle_Specs` — 3 heartbeat cases | `Task.Delay(HeartbeatInterval * 6 or * 8)` as the quiet window |
| `Retry_Specs.After_try_trying_again` | `Task.Delay(100)` as the "no further attempt" window |
| `Retry_Specs.Should_cancel_the_retry_and_give_it_up` | `Task.Delay(100)` as above |
| `RequestClient_Specs` — 3 unobserved-exception cases | `Task.Delay(1000)` around `GC.Collect` / `WaitForPendingFinalizers` |
| `AwaitSemantics_Specs.Should_block_the_caller_...` | `Wait(500 ms)` expected `false` as the "still blocked" evidence |
| `TestTimeoutBudget_Specs` — 3 cases | `WaitHandle.WaitOne(1500/1000 ms)` expected `false`; `Task.Delay(100)` |
| `Outbox_Specs.Should_not_receive_the_response` | `CancellationTokenSource(300)` as the "response never arrived" window |
| `PublishObserver_Specs.Should_not_invoke_the_send_observer_prior_to_send` | `OrTimeout(5 s)` expected to throw — five seconds of wall time per run |
| `CircuitBreaker_Specs.Should_work` | `Task.Delay(50)` between each of thirty publishes |

Every negative statement in this list is decided by a timer. On a loaded machine each can fail for
reasons unrelated to the behaviour, and none of them can ever prove the negative — only fail to
disprove it inside the window.

Separately, several cases pay real wall-clock time by construction: `DelayProvider_Specs` (2 cases,
≥1 s each), `DelayedRedelivery_Specs` (3 cases, ≥6 s each), `MinimalBody_Specs` (≥1 s),
`RecurringJobConsumer_Specs` (6 cases, 10–60 s each), `KillSwitch_Specs` (up to 35 s of health
polling). That is where the runtime of this cohort lives.

### F-D-05 — shared mutable static state (19 obligations)

**a) `EndpointConvention` — the global endpoint map, mutated and never cleaned up (7 obligations).**
This is the same class of defect a sibling cohort already found in the saga area.

| File | What is mapped | Where |
|---|---|---|
| `SendByConvention_Specs.cs` | `NastyMessage`, `TastyMessage`, two different `BusinessEvent` types, `NastyEvent` | inside `ConfigureInMemoryReceiveEndpoint` / `ConfigureInMemoryBus`, 5 fixtures |
| `MessageFlow_Specs.cs` | `EFoo` → the harness input queue | inside the test method |
| `SendContextMiddleware_Specs.cs` | `B` → the input queue | inside the test method, 2 fixtures |

None of these entries is ever removed. `EndpointConvention.Map<T>` writes into
`EndpointConventionCache<T>`, a static generic cache, so the mapping survives for the lifetime of
the process and points at an address whose bus has already been disposed. Two of the
`SendByConvention_Specs` fixtures map **different** `BusinessEvent` types (one interface, one
class), which is the only reason they do not collide today.

**b) Static counters that are never reset (12 obligations).**

| File | Static state |
|---|---|
| `Retry_Specs.cs` | `Consumer.Attempts` (2 fixtures), `_attempts` / `_lastAttempt` / `_lastCount` as `static` in `When_multiple_retry_policies_are_specified`, `Consumer.Attempts/LastAttempt/LastCount` in `When_the_retry_is_specified_within_the_consumer` |
| `InMemoryOutboxRedelivery_Specs.cs` | `TestHandler.Count`, a `public static int`, in all three fixtures |
| `ConcurrencyLimit_Specs.cs` | `ConsumerSaga._maxPendingDeliveryCount` and friends; `_complete` as a `static TaskCompletionSource` in both fixtures |

Every one of these is order-dependent and none survives a repeat run in the same process. The
`InMemoryOutboxRedelivery` case is the sharpest: the assertion is `Count == 0`, so a leftover count
of 1 from an earlier run makes it fail with no hint of why.

### F-D-06 — unawaited tasks and fire-and-forget (5 obligations)

- `InMemoryOutboxRedelivery_Specs` × 3: the consumer calls `context.Publish(...)` /
  `context.Send(...)` **without awaiting** and then throws. Here the un-awaited call is arguably the
  arrangement under test, but the returned task is genuinely unobserved.
- `Outbox_Specs`: `context.Respond(...)` is called without awaiting inside the handler.
- `Observer_Specs`: `context.Respond(...)` inside `IObserver.OnNext`, which is `void` and therefore
  cannot await — the observer contract forces fire-and-forget here.
- `RequestClient_Specs.Cancelling_a_request_mid_stream`: `Task.Run(async () => { await
  Task.Delay(500); cts.Cancel(); })` is never awaited or observed, and the `CancellationTokenSource`
  is never disposed.

No `async void` method exists anywhere in the 104 files (verified by grep). Several `async Task`
test methods contain no `await` at all (`MessageType_Specs` × 2, `RequestClientNew_Specs`
`Should_copy_the_time_to_live_to_the_response`, and others) — harmless but a compiler warning.

### F-D-07 — swallowed exceptions (4 obligations)

- `RequestClientNew_Specs.Should_handle_an_earlier_timeout` and `Should_handle_cancellation`: a
  five-iteration loop with `catch (Exception ex) { Console.WriteLine(ex.Message); }` and **no
  assertion at all**. Any exception, expected or not, passes. These two are the weakest obligations
  in the cohort.
- `SendObserver_Specs.An_observer_on_an_endpoint_with_response`: the consumer catches the
  `SerializationException` it deliberately causes and writes it to the console. Intentional, but the
  swallow is what makes the observer counts the only evidence.
- `NoOutboundVendorCall_Specs.Should_notice_an_outgoing_request_when_there_is_one`: swallows the
  connection failure on purpose; correct, and documented in the source.

### F-D-08 — assertion weaker than the name (24 obligations)

The rows carry these individually in `notes`; the ones that matter for the rebuild:

| Obligation | Name claims | Assertion establishes |
|---|---|---|
| `FaultPublish_Specs.A_faulting_consumer_when_fault_publishing_is_disable.Should_publish_a_single_fault_when_retried` | a single fault is published | the fault count is **zero** |
| `Retry_Specs.When_specifying_retry_for_the_consumer.Should_only_call_the_handler_once` | once | six invocations |
| `SendByConvention_Specs.Conventional_polymorphic_overridden.Should_send_by_convention_to_the_input_queue` | the input queue | delivery at `second_queue` |
| `MessageType_Specs.Should_not_allow_array_message_types_but_does` | arrays are rejected | arrays are accepted |
| `Retry_Specs.When_specifying_the_bus_level_retry_policy_for_base_type` | base-type behaviour | the marker interface is never used in an assertion |
| `CronExpressionTests.CanUseLastDayOfMonthInArray` | the L token in an array works | only that the named days match; nothing is asserted about days that must **not** match |
| `CronExpressionTests.CanGetHashCode` | hash code contract | equal texts hash equally; a constant hash would pass |
| `CronExpressionTests.ExpressionEquality` | equality | reflexive equality only; no unequal pair is tested |
| `CronExpressionTests.CronExpressionReturnsExpectedNextFireTime` | the expected next fire time | only `.Date` is compared; the 12:00 in every expected value is discarded |
| `SendEndpointCache_Specs.Querying_for_two_endpoints_at_the_same_time` | a cache queried concurrently | only that nothing throws; endpoint identity is never compared |
| `ImplementedTypeCache_Specs.It_should_be_able_to_get_an_interface` | getting an interface | a count of 1; which type was collected is never checked |
| `JobConsumer_Specs.Should_create_a_unique_job_id` | uniqueness | non-emptiness |
| `SerializationFault_Specs.It_should_respond_with_a_serialization_fault` | a serialization fault | the outer `RequestFaultException` type only |
| `DelayProvider_Specs.Should_manage_delays_in_order` / `..._without_waiting` | ordering | one delay's elapsed time |
| `RequestFilter_Specs.Should_fault_instead_of_timeout` | fault instead of timeout | the fault type; nothing bounds the duration |
| `TelemetryMonitor_Specs` × 3 | `Wait()` waits until consumed | only that the follow-up message is findable afterwards |
| `Retry_Specs.Should_cancel_the_retry_and_give_it_up` | the retry is cancelled by the stop | that the retry had not yet fired at assertion time; the stop happens later, in teardown |
| `StartStop_Specs.Should_start_stop_and_start_only` | start, stop, start | one cycle only |
| `RecurringSchedule_Specs` × 2 | "from noon until one" | an expression that names hour 12 with no upper bound |

### F-D-09 — dead and unreachable code inside tests (8 obligations)

- `ResponsePatternMatching_Specs.Should_use_the_new_syntax_to_be_awesome_er` (both fixtures):
  `Assert.Pass` inside the `switch` statement aborts the method, so the `switch` **expression**
  assertion below it never runs. Half of each test is unreachable.
- `DelayedRedelivery_Specs.Using_multiple_redelivery_filters.Should_play_nicely_together`: the
  `MessageA` / `ExceptionA` half of the arrangement is registered but never exercised, and its
  assertions are commented out. The name claims both filters work together; one is measured.
- `CronExpressionTests.TestCronExpressionWeekdaysFriday`, `...FridayEveryTwoWeeks`,
  `...ThirsdayAndFridayEveryTwoWeeks`: `nextRunTime` and `nextRunTime2` are computed from
  `DateTimeOffset.Now` and never used — dead code that also makes the test read the wall clock.
- `CronExpressionTests.CannotUseMultipleLastDayOfMonthInArray`: the `expectedDays` parameter is
  declared, supplied by `[TestCase]`, and never read.
- `Retry_Specs.When_you_say_deuces_and_stop_the_bus`: `_completed` is created and never used.
- `Introspection_Specs`: a rate limit, a concurrency limit and a whole `MultiTestConsumer` are
  configured and none of them is observed.
- `RequestClientNew_Specs.RequestClientMessages.ReturnedValue` / `AuditGetValue` are used, but
  `Sending_a_request_with_a_timeout` has the two bound the wrong way round — see F-D-10.

### F-D-10 — `RequestClientNew_Specs.Sending_a_request_with_a_timeout`: swapped subjects

`_audited` is bound to `AuditGetValue`, which the consumer **sends**;
`_returned` is bound to `ReturnedValue`, which the consumer **publishes**. But
`Should_not_copy_the_time_to_live_to_published_messages` reads `_audited` and
`Should_not_copy_the_time_to_live_to_sent_messages` reads `_returned`. The two obligations are
therefore each named after the other one's subject. Both facts are true, so the tests pass; the
names are wrong. Worth fixing during the rebuild rather than carrying over.

### F-D-11 — order-dependent obligations (7)

- `TestTimeoutBudget_Specs.Granting_the_test_timeout_through_the_fixture` × 2 — order-dependent
  **by design** (`[Order(1)]` / `[Order(2)]`), and correctly so: the second test's entire obligation
  is that it runs after the first on the same fixture instance. Must stay ordered in the rebuild.
- `ConsumeObserver_Specs.Observing_consumer_messages` × 2 — both tests read observer state produced
  by the same `[OneTimeSetUp]` publish, so they share one message and one observer list.
- `MessageContext_Specs.Sending_a_request_with_two_handlers.Should_not_complete_the_handler` —
  depends on the `[OneTimeSetUp]` request having already completed.
- The static-counter cases in F-D-05 b) are order-dependent by accident, which is the bad kind.

### F-D-12 — duplicate obligations (4 pairs)

| Pair | Difference |
|---|---|
| `BadConfiguration_Specs.Should_throw_for_message_retry` / `Should_throw_for_retry` | none — byte-identical bodies |
| `MessageContext_Specs.Should_receive_a_request_timeout_exception_on_the_handler` / `..._on_the_request` | lambda shape only |
| `SendObserver_Specs.Connecting_a_send_observer_to_the_endpoint.Should_invoke_the_observer_prior_to_send` / `..._after_send` | identical assertions (`PreSentCount 1`, `PostSentCount 1`) |
| `RequestClientNew_Specs.Should_handle_an_earlier_timeout` / `Should_handle_cancellation` | one constant (140 ms vs 100 ms), and neither asserts anything |
| `ExceptionInfo_Specs` two fixtures | same three assertions; the arrangement differs (wrapper exception vs `Exception.Data`), so this pair is legitimate |
| `SendObserver_Specs` bus fixtures A/B/C/D | four fixtures for four one-line cases, only because the observer is connected once per fixture |

### F-D-13 — over-broad snapshot assertion

`CronExpressionTests.CanGetExpressionSummary` compares a thirteen-line rendered summary with a
verbatim string literal. Any formatting change fails it, the failure does not name which field
changed, and the literal is embedded with the source file's line endings.

### F-D-14 — unseeded randomness

`PollingAlgorithm_Specs.Should_be_able_to_control_the_request_flow_by_group` creates
`new Random()` with no seed inside the message factory, so the grouping is not reproducible and no
seed is reported on failure. The `lock (random)` sits inside a deferred LINQ `Select`, so it does
not actually serialise anything.

### F-D-15 — assertions inside product callbacks

`MultiBusRequest_Specs.Using_the_request_client_across_bus_instances` asserts the injected send
endpoint provider types **inside the consumer**. A violation arrives at the test as a
`RequestFaultException`, not as the named assertion.
`SendContextMiddleware_Specs.Adding_a_send_context_middleware_component` does the same through a
filter that throws `InvalidOperationException`.

### F-D-16 — cleanup that is not verified

`ReceiveEndpoint_Specs.Should_not_be_allowed_twice` never stops or disposes the second endpoint
handle, because the `ConfigurationException` is expected before `Ready` completes.
`RecurringJobConsumer_Specs` — 4 of its 6 cases have no `try`/`finally`, so a failing assertion
leaves a running harness until the provider is disposed.

---

## 4. Findings about the files rather than the tests

### F-D-01 — `Definition_Specs.cs` carries no test

The file is named `_Specs` and is compiled, but declares only message contracts, two consumers and a
`ConsumerDefinition`, all inside a nested `PingDefinitions` namespace. Nothing references it from the
root files of this cohort. It may be referenced from a subdirectory that a sibling cohort owns; that
is not decidable from within my scope. → **Q-D-03**.

### F-D-02 — `NewConfigurationModel.cs` is excluded from compilation

`<Compile Remove="NewConfigurationModel.cs" />` in `ViciOne.ServiceBus.Tests.csproj`. The content is
a commented-out design sketch calling `Bus.Initialize(ep => …, bus => …)` with `ReceiveFrom`,
`DisableAutoStart`, `EnableAutoSubscribe` and `PurgeBeforeStarting` — an API that no longer exists.
It carries no obligation, has no test, and would not compile if it were included. → **Q-D-03**.

### F-D-17 — `[TestFixture]` missing on three test classes

`RecurringSchedule_Specs`, `TaskExtension_Specs` and `CronExpressionTest` have `[Test]` methods but
no `[TestFixture]` attribute. NUnit discovers them anyway; the anchor proves they execute
(4, 10 and 115 identities respectively). Recorded because a rebuild on a different discovery model
must not silently lose them.

### F-D-18 — `NUnit1032` suppressions

18 of the 104 `.cs` files suppress `NUnit1032` ("field of type Task should be disposed") around their
`Task<ConsumeContext<T>>` fields. That is the analyzer objecting to a real pattern: the fixtures hold
`Task` fields created during endpoint configuration. The rebuild should remove the pattern, not the
suppression.

### F-D-19 — the project is signed for a stated reason

`ViciOne.ServiceBus.Tests.csproj` imports `../../signing.props` with a comment explaining that a
signed product assembly can only grant friendship to a signed one, and that the hardened MessagePack
option set has to be asserted directly. No root-level file of this cohort uses `InternalsVisibleTo`
access; the MessagePack assertion presumably lives in a subdirectory. Recorded so the rebuild does
not drop the signing on the assumption that it is decorative.

### F-D-20 — package surface of the project

`packages.lock.json` pins 13 direct packages and 5 project references for `net10.0`, including
`Google.Protobuf`, `MathNet.Numerics`, `Microsoft.Data.SqlClient` and
`Microsoft.Extensions.Diagnostics.Testing`. **None of these four is used by any root-level file of
this cohort**; they must belong to subdirectories. `Microsoft.Data.SqlClient` in a hermetic unit
project is worth a look during the rebuild.

### F-D-21 — one file mixes two subjects

`SerializationFault_Specs.cs` holds one inherited fixture with a weak assertion and one clearly
newer fixture (`When_a_message_has_an_unrecognized_body_format`) whose doc comment explains exactly
why the naive arrangement is not decidable. The second is a model for the rebuild.

### F-D-22 — `MessageUrnSpecs` static-constructor coupling

The four attribute-validation cases rely on `MessageUrnCache<T>`'s **static constructor** throwing.
A static constructor throws `TypeInitializationException` once and then the type stays unusable, so
each of the four subject types (`AttributedNull`, `AttributedEmpty`, `AttributedWhitespace`,
`AttributedKnownPrefix`, `AttributedNoDefaultsInvalidUrn`) may only be touched by exactly one test
in the process. Any rebuild that adds a second test over the same subject type will get a different
exception on the second call.

### F-D-23 — `EndpointName_Specs` binds the test's own namespace

`Should_include_the_namespace` and `Should_include_the_namespace_and_prefix` embed
`vici-one-service-bus-tests-endpoint-name-specs` — the namespace **and class name of the test
fixture itself** — into the expected string. `MessageUrnSpecs.NestedMessage` does the same with
`ViciOne.ServiceBus.Tests:MessageUrnSpecs+X`. Renaming the fixture or moving it to `tests2` breaks
these assertions, which is exactly what the rebuild will do.

### F-D-24 — `ServiceInstanceEndpoints_Specs` re-implements the naming rule

`Should_keep_the_consumer_endpoint_name_that_the_instances_compete_on` derives the expected name in
the test with `nameof(ServiceInstanceMessage).Replace("Message", string.Empty)`. The test computes
the answer with the same rule it is checking.

### F-D-25 — non-ASCII literal in `MessageUrnSpecs.AttributedMessage_with_symbols`

The `[MessageUrn]` argument and the expected `Uri` both contain characters that appear as
replacement characters in the source as read. The case is encoding-sensitive, and a commented-out
alternative assertion sits directly beside it. A rebuild must fix the encoding intent explicitly
rather than copy the bytes.

### F-D-26 — `CircuitBreaker_Specs` documents its own failure

The source carries the comment `// this is broken, because the faults aren't produced by an open
circuit breaker`. The obligation (a circuit breaker that trips short-circuits the endpoint) is real
and currently **unproven**. It must not be replaced by a test that merely counts faults again.

---

## 5. Open questions for the Lead

**Q-D-01 — `MessageType_Specs.Should_not_allow_array_message_types_but_does`.**
The name states an intended contract and the assertion states the opposite, current behaviour. This
row cannot be dispositioned as `REPLACED_EXECUTING` without a decision: is the array message type a
supported capability (then the test needs a name that says so), or a defect (then the rebuild needs
a test that fails today, or a removal decision plus a product change)?

**Q-D-02 — `SendContextMiddleware_Specs`, the two payload cases.**
`Accessing_payload_from_consume_context_in_send_context.Should_contain_the_same_payloads` and its
publish-pipe twin assert that a send/publish filter can reach a payload the consume filter added.
The source comments state plainly that these assertions **fail**
(`// those fails, as while they DO have ",has-consume-context" they don't have access to
SomePayload`). Either the anchor records them as executing because the product has since been fixed,
or the anchor records a run in which they were green for a different reason. I did not execute
anything, so I report the contradiction rather than resolve it. A Lead decision is needed before
these two rows can be replaced by passing tests.

**Q-D-03 — `Definition_Specs.cs` and `NewConfigurationModel.cs`.**
Neither carries a test. `Definition_Specs.cs` may be referenced from a subdirectory outside my
scope; `NewConfigurationModel.cs` is excluded from compilation and calls a removed API. Proposed:
`Definition_Specs.cs` → integrator check against the sibling cohorts before any removal;
`NewConfigurationModel.cs` → `REMOVED_WITH_PRODUCT_CAPABILITY` (the `Bus.Initialize` capability it
sketches does not exist), but that needs the removal evidence the rules require.

**Q-D-04 — the four duplicate obligation pairs (F-D-12).**
Each pair carries one obligation, not two. Collapsing them changes the identity count of the
rebuilt suite, so the Lead should decide whether the identity count is a target or a consequence.

**Q-D-05 — `CronExpressionTests` time zone policy.**
21 of the 58 obligations in that file derive their expected value from the host time zone, and two
resolve a Windows time zone id. `TZ=UTC` (plan §8) makes them pass but does not make them correct.
Proposal: rebuild every one of them against a test-owned `TimeZoneInfo`, following the pattern
already present in `Should_carry_a_daily_time_across_the_spring_daylight_saving_jump`. That changes
21 expected values, which is a decision rather than a refactoring.

**Q-D-06 — `CircuitBreaker_Specs.Should_work` (F-D-26).**
The circuit breaker obligation is currently unproven and the source says so. Should R1 build the
test the name promises (an open breaker short-circuits the endpoint), which will require the
breaker's own state to be observable, or should the obligation be carried as `QUESTION` until the
product exposes that state?

**Q-D-07 — the twelve `R0-CORE-B` identities.**
Recorded here only for completeness: `ConsumeMetrics_Specs` (5), `InstrumentationRegistration_Specs`
(6) and `KillSwitchInstrumentation_Specs` (1) carry the root namespace but are declared inside
`ContainerTests/`. They are **excluded** from my 534 and appear in no row of mine. If the integrator
finds them claimed twice, the duplicate is not from this cohort.
