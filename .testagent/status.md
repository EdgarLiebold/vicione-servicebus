# Status — native test reconstruction

## Preserved baseline

The accepted native foundation and MessageBody behavior are committed and remotely preserved at
`b3519291c65026fd3714929d9b721dc4e52ea01d`.

## Current source state

- the remainder of the Abstractions unit behavior is reconstructed in the source-owner project;
- host-dependent worker-id behavior is isolated in an independent LocalIntegration project;
- the framework-neutral Roslyn host and all 115 inherited Analyzer obligations are reconstructed;
- Analyzer behavior and CodeFix behavior have separate source-owner projects;
- the 91 message-contract obligations are preserved as 48 canonical scenarios and 91 native xUnit
  cases; applicable fixes run as separate native CodeFix cases;
- all 26 executing inherited SignalR behaviors are reconstructed without the inherited TestFramework;
- five greenfield SignalR hardening cases cover multi-target, empty-target, and MessagePack paths;
- all 67 inherited MessagePack obligations plus six mixed-fixture body obligations are replaced by 49
  source-owner cases covering direct serialization, body accessors, architecture, configuration,
  in-memory dispatch, and delayed redelivery;
- all nine inherited state-machine-visualizer obligations are replaced by seven source-owner behavior
  cases plus one requirement-projection case, covering exact Graphviz and Mermaid output, declarative,
  dynamic, request-derived, empty, and composite-event graphs;
- all 58 inherited Cron-expression obligations are replaced by deterministic source-owner cases;
  repeated spaces and tabs now preserve field alignment instead of silently shifting the schedule;
- all 17 inherited endpoint-name formatter obligations are replaced by 17 source-owner cases under
  `Configuration/EndpointNaming`, with literal naming oracles and exact reserved-type failures;
- 15 inherited/current-product MessageUrn obligations are mapped to 17 source-owner cases under the
  Abstractions root and `Attributes`; both runtime overloads now share input validation;
- all seven request-rate algorithm obligations are mapped to eight deterministic source-owner cases;
- the ambiguous implemented-message-type cache is replaced by six exact topology cases; the product
  no longer depends on reflection enumeration order and retains interfaces inherited through an
  excluded base class as independent direct topology edges;
- all five inherited polymorphic-fault obligations are replaced by two exact Abstractions metadata
  cases and one real in-memory core publication case;
- the ambiguous inherited array-message case is replaced by an explicit single-message contract
  that preserves element count, order, and values through the real in-memory serialization path;
- future-location round trips use fixed external input and malformed stored locations now expose one
  stable format boundary instead of parser, LINQ, or null-reference implementation exceptions;
- TaskExecutor execution is now deterministic across Action, Task, and explicit ValueTask delegates;
  bounded capacity, cancellation, faults, disposal, and validation have executable contracts;
- TaskUtil synchronous waits now have deterministic context, completion, cancellation, fault, and
  validation contracts; no behavior assertion depends on elapsed wall-clock time;
- task initializer projections now expose explicit asynchronous names and type-safe reference and
  nullable-value fallback contracts; all ten inherited behaviors are terminally mapped;
- the header initializer convention now has an exact publish-context contract for standard and
  typed custom headers without wall-clock assertions;
- integer-backed state conversion is now verified through the complete in-memory state-machine
  publication path, including asynchronous projection and custom-header delivery;
- scalar message initialization now covers exact copies, invariant strings, nullable boundaries,
  enums, and object identity across the full inherited type matrix;
- object-graph initialization now proves covariant fault projection, recursive private setters,
  and observable read-only-property behavior for interface and concrete targets;
- dictionary and ExpandoObject initialization now proves enum/scalar conversion, explicit converter
  availability, exact concrete DTO preservation, nested dictionary contracts, and nested list
  materialization under their actual source owners;
- the public message-initializer capability matrix now covers request/response merging, missing
  values, nullable chains, collections, nested objects, exceptions, variables, and nested tasks in
  13 independent source-owner facts instead of one shared fixture;
- the property-provider factory matrix now proves all supported task, array, enumerable,
  dictionary, scalar, nullable, object, enum, Uri, contract, exception, and variable result shapes
  in 14 source-owner facts plus one independent unsupported-pair hardening fact;
- the agent cohort now proves ready-fault propagation, empty and recursive shutdown, agents added
  after readiness, and deterministic cache recreation after faults or explicit invalidation in six
  source-owner facts;
- the inherited authentication sample is replaced by the actual public custom-pipe-specification
  contract under its Abstractions configuration owner: validation rejects null and empty setup,
  while an accepted branch runs before the following pipe segment;
- bound pipe contexts now have an assertion-bearing Core contract for exact left/right context
  identity and ordered execution across `ContextPipe`, the bound pipe, and the following segment;
- cache bucket, age, capacity, and usage-sensitive retention now have seven deterministic source-owner
  cases synchronized through cache observer events and the central test timeout;
- completed node factories now have an exact Core promotion contract covering the stored node,
  observer payload, value identity, and complete one-operation statistics delta;
- cache insertion, index factories, pending-value arbitration, and coordinated multiple indexes now
  have 13 deterministic source-owner facts; explicit removal atomically invalidates a stored node,
  preserves the bucket traversal link until compaction, and never reports a pending factory as
  removed;
- the endpoint-resource cache now has ten deterministic source-owner behavior facts plus two
  hardening facts for invalid TTL configuration and timestamp-frequency correctness; its index
  keeps lifecycle-managed nodes strongly so successful creation cannot race with garbage
  collection, and the old/new same-named test projects no longer share an SDK intermediate path;
- the real send-endpoint provider resolves two distinct addresses concurrently and returns the
  exact cached endpoint instance for each warm lookup without merging the addresses;
- real in-memory dispatch exposes a typed message body as an exact raw
  `System.Text.Json.Nodes.JsonObject`, including its complete camel-case property set and values;
- System.Text.Json collection, interface-metadata, date-time, scalar, private-setter, and payload
  type-marker behavior is reconstructed as 13 source-owner cases without the inherited fixture
  hierarchy; mixed MessagePack rows retain their existing independent owner;
- DateTime/DateTimeOffset invariant conversion and `JsonElement`/dictionary object transformation
  add five source-owner facts under their actual product paths;
- three real in-memory fault paths now prove exact request-fault propagation, unsupported-body
  receive faults, and nested contract-type mismatch without regular dispatch;
- a minimal two-field System.Text.Json envelope now proves exact virtual delayed redelivery without
  wall-clock waiting; the test exposed and the product removes a thread-pool registration race;
- the inherited mixed MessageBody fixture is terminally closed across its three source owners: all
  87 obligations are mapped exactly once, Core and MessagePack own exact concrete-type censuses,
  and four Core serializing bodies prove every first-accessor order against external byte and text
  oracles, genuinely rejected writes, and unchanged bodies after the failed write;
- eight System.Text.Json contract-shape obligations are replaced by seven native cases covering
  immutable constructor binding, maximum-decimal precision, envelope/raw extension data, and
  declared polymorphism for scalar, array, and list properties;
- UnitArchitecture: 1155 total, 1155 passed, 0 failed, 0 skipped; consume observers, recorded-message
  metadata, all three filter kinds and every public sent/published/received observation-list query
  shape reject their targeted one-cause regressions; a real dynamically connected endpoint proves
  that endpoint-local publications remain visible through the dedicated receive-endpoint observer;
  the DI utilities additionally prove exact publish filtering, fail-fast arguments, virtual
  readiness timeout, ordered task registrations and uniform global configuration of both dynamic
  connector overloads. Two consecutive 105-case Testing runs reject the previously exposed retry-
  observer race. The observer-pipeline cohort additionally proves exact transport, publish,
  receive, consume, mediator and message-observer boundaries plus the complete deterministic
  message-flow timeline;
  message identity and header metadata are additionally closed through exact correlation precedence,
  explicit overrides, conversation causation, source address, `MessageId`-derived UTC time, header
  copy/alias semantics and interface-object materialization. Eleven one-cause product mutations reject
  regressions in those paths; message contexts and dynamic contracts add 24 source-derived Core
  cases for exact request/response causation, independent response delivery, timeout/cancellation,
  nested and generic interfaces, retained serializers, collectible proxy structure, complete custom
  attributes and invalid-contract boundaries. Eight one-cause mutations reject regressions there;
  request clients, response matching, mediator, and multibus add exact deadline/cancellation,
  response arbitration, accepted-type, metadata, TTL, outbox, filter-fault, mediator-time, and
  secondary-bus-isolation contracts. All 40 inherited identities are terminally mapped to 24
  permanent methods, the six inherited fixtures are removed, and eleven one-cause mutations reject
  the protected failures. The capability set is retained behind the A+ ViciOne.ServiceBus API;
  MassTransit API compatibility is explicitly not required;
- LocalIntegration: 3 total, 3 passed, 0 failed, 0 skipped;
- Release builds: 0 warnings, 0 errors;
- inherited-test source: the fully replaced Abstractions NUnit project, endpoint-name, MessageUrn,
  and request-rate fixtures have been removed; the Abstractions compile-only usage examples now
  live in the non-packable `samples/OrderWorkflow` project; `MessageType_Specs.cs` is removed after
  its array behavior and both already-migrated MessageUrn behaviors became terminal;
- locked restores, bounded formatter/analyzer gates, mapping closure, and full static review pass;
- targeted mutation review: passed for Abstractions, the Analyzer foundation, MessagePack, and the
  state-machine visualizer, including product
  behavior, code-fix output, requirement projection, omitted tests, fail-closed Roslyn compilation,
  the local host-derived worker-id contract, message-contract scenario closure, exact diagnostics,
  exact CodeFix output, and required source forms.
- the Cron cohort independently rejects its product regression, a missing requirement projection
  row, and removal of its test project from the Unit profile.
- the endpoint-name formatter cohort passed its focused 170-case project run, all four targeted
  product mutations, and its then-current 635-case unfiltered UnitArchitecture profile;
- the MessageUrn cohort passed its focused 99-case project run, all five targeted product
  mutations, and its then-current 652-case unfiltered UnitArchitecture profile;
- the request-rate cohort passed three consecutive focused 107-case runs, all six targeted product
  mutations, and its 660-case unfiltered UnitArchitecture profile;
- the reflection cohort adds eight source-owner cases, corrects the invalid missing-key gap, and
  hands the formerly ambiguous implemented-message-type obligation to its topology cohort;
- the implemented-message topology cohort closes that obligation with exact type/direct assertions,
  removes the inherited count-only fixture, and rejects five independent product mutations;
- the polymorphic-fault cohort splits the inherited mixed fixture by source owner, proves exact
  class/interface fault metadata and real derived-to-base in-memory publication, and removes the
  inherited file only after all five obligations close;
- the polymorphic-fault cohort rejects independent hierarchy-metadata, exception-data,
  supported-message-type, and requirement-projection mutations before its final 679-case run;
- the array-message cohort rejects a product mutation that invalidates arrays as message contracts,
  and its ordinary assertions prove the exact serialized element sequence;
- the future-location cohort rejects endpoint-normalization, format-boundary, null-guard, and
  requirement-projection mutations before its final 689-case run;
- the TaskExecutor cohort rejects premature ValueTask completion, missing bounded-capacity
  validation, broken single-reader backpressure, ordinary-error cancellation, post-disposal
  exception leakage, and missing requirement metadata before its final 706-case run;
- the TaskUtil cohort rejects premature return, AggregateException leakage on the generic result
  path, caller-token substitution, faulted-instead-of-canceled state, synchronous continuations,
  an invalid registration signal, caller-context mutation, and missing requirement metadata before
  its final 724-case run;
- the task initializer cohort rejects null-source selector invocation, branch inversions, eager
  fallback factories, source/delegate-state wrapping, and missing requirement metadata before its
  final 739-case run;
- the header initializer cohort rejects name-normalization, standard-prefix, null-validation,
  TTL-inspector, and requirement-projection mutations before its final 741-case run;
- the state-property converter cohort rejects state-name, asynchronous-conversion, custom-header,
  and requirement-projection mutations before its final 742-case run;
- the scalar-initializer cohort rejects conversion, copy, nullable-boundary, and requirement-
  projection mutations before its final 747-case run;
- the object-graph initializer cohort replaces four assertion-free inherited cases with observable
  contracts and rejects private-setter, getter-only projection, missing fault-message, and
  requirement-projection mutations before its final 753-case run;
- the dictionary and ExpandoObject cohort rejects object-enum conversion, dictionary lookup,
  object-to-contract conversion, exact property copy, nested-list materialization, and requirement-
  projection mutations before its final 759-case run;
- the message-initializer capability cohort maps all 27 inherited rows to 13 facts and rejects
  independent pipeline, dictionary, array, list, Uri, duplicate-property, exception, variable,
  nested-task, and requirement-projection mutations before its final 772-case run;
- the property-provider cohort maps all 42 inherited rows to 14 behavior facts, adds one independent
  false-path hardening fact, and rejects twelve effective product mutations plus requirement drift
  before its final 787-case run;
- the agent cohort maps all six inherited rows one-to-one and rejects five independent product
  mutations, one missing invalidation action, and requirement drift before its final 793-case run;
- the custom pipe-specification cohort maps both inherited rows to two source-owner methods with
  three execution cases, rejects missing validation, omitted specification application, broken
  continuation, invalid empty-role acceptance, and requirement drift before its final 796-case run;
- the bound pipe-context cohort replaces its assertion-free timeout fixture with one deterministic
  fact and rejects missing configured output, lost left or right contexts, broken outer
  continuation, and requirement drift before its final 797-case run;
- the cache bucket/capacity cohort maps all seven inherited obligations to five methods and seven
  cases; it rejects broken bucket links, disabled expiration or capacity cleanup, lost observer and
  usage signals, and requirement drift before its final 804-case run;
- the node-tracker promotion cohort maps its single inherited obligation to one exact source-owner
  fact and rejects skipped promotion, incorrect hit/miss accounting, missing add accounting, lost
  observer output, and requirement drift before its final 805-case run;
- the cache index/factory cohort terminally maps all fourteen inherited obligations to twelve
  source-owner facts, adds one independent pending-removal hardening fact, and rejects missing
  atomic eviction, a severed bucket chain, false pending-removal success, and requirement drift
  before its final 818-case run;
- the endpoint-resource cache cohort maps all thirteen inherited obligations to ten source-owner
  facts, adds two TTL hardening facts, removes the two fixtures and their obsolete support types,
  and rejects cache-node lifetime, timestamp-frequency, capacity, usage-policy, recovery,
  single-flight, requirement, and intermediate-artifact drift before its final 831-case run;
- the send-endpoint cache cohort maps its sole inherited obligation to one real in-memory provider
  fact and rejects a direct-factory cache bypass before its final 832-case run;
- the raw System.Text.Json object cohort maps its sole inherited obligation to one real in-memory
  serialization/dispatch fact and rejects an empty-body projection before its final 833-case run;
- the System.Text.Json serialization cohort terminally closes all 23 rows in six inherited files
  through 16 new core dispositions and seven existing MessagePack dispositions; its 13 cases reject
  interface-attribute loss, list/dictionary confusion, and an absent hostile type marker before the
  final 846-case run;
- the object/date-time conversion cohort maps five inherited rows to five source-owner facts,
  rejects three independent product mutations, passes under a non-gregorian process culture, and
  removes both obsolete NUnit files before the final 851-case run;
- the serialization-fault cohort maps three inherited rows to three source-owner facts, rejects
  successful request handling, a registered media type, and a repaired nested field before its
  final 854-case run, and removes both obsolete NUnit files;
- the minimal-envelope cohort maps its compound inherited row to one real in-memory source-owner
  fact, rejects interval and redelivery-count drift, fixes the delayed-registration race exposed by
  the full profile, and removes the obsolete NUnit file before its final 855-case run;
- the MessageBody contract cohort composes 51 accepted Abstractions obligations, six accepted
  MessagePack obligations, and 30 Core/cross-owner obligations into one exact 87/87 terminal
  disposition; 20 new native cases reject ten independent behavior, census, and projection
  mutations before the inherited mixed fixture is removed and the final 875-case run passes;
- the System.Text.Json contract-shape cohort maps all eight obligations from four inherited files,
  rejects five independent converter, contract, value, and projection mutations, isolates and
  restores mutable serializer options, and removes the four obsolete NUnit files before its final
  882-case run;
- the System.Text.Json collection-compatibility cohort composes 17 accepted MessagePack identities
  with 17 Core identities into one exact 34/34 source-file disposition; 13 new cases and two
  stronger existing carriers preserve collection shapes, cardinality, values and order, reject five
  independent behavior/projection mutations, and remove the inherited fixture before the final
  899-case run;
- the serializer-configuration and forwarding cohort terminally maps and removes both inherited
  fixtures, preserves forwarding over the real in-memory path, and rejects validation, callback,
  shared-state, metadata, body, pipe-classification, expiration-boundary, and requirement-projection
  regressions before its final 937-case run;
- System.Text.Json and MessagePack consume one shared envelope-metadata projection and one
  context-owned standard `TimeProvider`; deterministic fake-time cases prove exact instant capture,
  zero and negative TTL behavior, and cross-format field parity without wall-clock waiting;
- the testing-observation cohort maps all 13 inherited list/inactivity obligations to 17 ordinary
  source-owner facts; both primitives use one standard `TimeProvider`, synchronous filters never run
  under the list monitor, source failures remain visible, and eleven one-cause mutations reject
  process-time fallback and every protected concurrency/cancellation boundary before the final
  954-case run;
- the inherited Abstractions NUnit project is retired after 78/78 terminal dispositions; its
  byte-identical formatter oracle remains embedded, and its compile-only usage surface is preserved
  by the non-packable, Engineering-bound OrderWorkflow sample;
- the repository-wide retrospective [product-defect accommodation audit](../evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/RETROSPECTIVE_PRODUCT_DEFECT_ACCOMMODATION_AUDIT.md)
  found no weakened, skipped, filtered, or defect-accommodating test in the reconstructed scope; it
  explicitly excludes not-yet-reconstructed product areas from that verdict.

## Remaining program

All other source owners, database/broker/cloud profiles, inherited-stack deletion, TestFramework
removal, and the atomic `tests2` to `tests` promotion remain open. No current result represents those
future cohorts as complete.

## Accepted: System.Text.Json application-format compatibility

- Baseline: `5f513c7d44694d1e4f5f9a4be20c504672b32ef9`, clean and equal to the private remote.
- Research and exact R0/PO boundary: complete.
- Scope: four executable obligations plus resolution of `OBL-R0-CORE-C-0473`; three
  MessagePack XML identities remain owned by their already accepted cohort.
- Product boundary: unchanged; no XML/Raw-XML or Protobuf product serializer is permitted.
- Four source-owner facts prove generated Protobuf and opaque XML application values over envelope
  and raw System.Text.Json, including exact media types, wire shapes, scalar/repeated/timestamp
  values, text, UTF-8 bytes, and non-aliasing.
- Google.Protobuf 3.36.0 and Grpc.Tools 2.83.0 are confined to the native core test project; no
  product project or surviving inherited test project references either package.
- The four one-cause mutations fail for the intended reasons; focused 369/369, complete 886/886,
  and LocalIntegration 3/3 pass with zero skipped tests. Release builds have zero warnings and zero
  errors. Seven inherited files are terminally replaced and removed.

## Accepted: System.Text.Json collection compatibility

- Baseline: `952a1fa6d507309d71c6b163813f0c03c95b48c6`, clean and equal to the private
  remote.
- Exact closure: all 34 inherited identities are terminal, consisting of 17 accepted MessagePack
  identities and 17 Core identities owned by 13 new cases plus two stronger existing facts.
- The source-mirrored Core tests prove nested lists and objects, empty/single/multiple dictionaries
  and arrays, concrete/interface sets, read-only graphs, generic arrays, empty contracts, enums,
  constructor-bound private-setter values, and ordered duplicate-key pairs.
- Five one-cause mutations fail for exact behavior or projection reasons; focused 17/17, native
  Core 382/382, complete UnitArchitecture 899/899, and LocalIntegration 3/3 pass with zero skipped
  tests. Release builds have zero warnings and zero errors.
- `MoreSerialization_Specs.cs` is removed after its bounded inherited-project compile proof. No
  empty directory remains below `tests/`.

## Accepted: Core serializer configuration and forwarding

- All 14 serializer-configuration obligations and both duplicate forwarding obligations have exact
  terminal dispositions under their source owner.
- Fifteen replacement facts, one empty-pipe hardening fact, the forwarding-expiration boundary, and
  deterministic envelope-time cases pass through ordinary xUnit/MTP execution.
- Both inherited fixtures are removed only after focused source-owner builds and runs, the complete
  UnitArchitecture 937/937 profile, unchanged LocalIntegration 3/3 profile, and targeted one-cause
  mutations passed with zero warnings, failures, or skipped tests.
- Common envelope metadata is encoded once in Abstractions and consumed identically by the retained
  System.Text.Json and MessagePack serializers; serializer-specific code owns payload encoding only.

## Accepted: Testing observation primitives

- All nine inherited asynchronous-list obligations and all four inactivity-observer obligations
  have exact terminal dispositions under the source-mirrored `Testing` owner.
- Seventeen deterministic xUnit facts cover existing and later messages, include/exclude/pattern
  filters, cancellation, asynchronous and synchronous virtual deadlines, forced and observed
  inactivity, repeated source queries, visible source failure, monitor-free callback execution, and
  null-provider rejection.
- Existing constructors remain source-compatible and default to `TimeProvider.System`; explicit
  provider-aware overloads make every owned timeout deterministic without a custom product clock.
- Eleven independent one-cause mutations fail for their intended reasons. Core passes 429/429,
  UnitArchitecture passes 954/954, and LocalIntegration passes 3/3 with no failures or skipped tests.
- `AsyncMessageList_Specs.cs` and `InactivityObserver_Specs.cs` are removed only after their complete
  terminal mappings and the inherited project compile check passed.

## Accepted: observer pipelines and message-flow diagnostics

- All 27 inherited observer and message-flow obligations have exact terminal replacement mappings.
- Seventeen native facts plus one strengthened existing timeline fact cover connection fan-out,
  send, publish, receive, consume, message-observer, mediator and diagnostic-flow boundaries beyond
  the inherited lower bound.
- `Connectable<T>` keeps its internal cached snapshot but exposes only defensive copies and observes
  synchronous, asynchronous and null-task fan-out failures without skipping later callbacks.
- Eight independent one-cause mutations fail for the intended reasons. Release builds have zero
  warnings and errors; Core is 533/533, Abstractions 139/139, UnitArchitecture 1063/1063, and
  LocalIntegration 3/3, with no failures or skipped tests.
- `MessageFlow_Specs.cs`, `Observer_Specs.cs`, `PublishObserver_Specs.cs`,
  `ReceiveObserver_Specs.cs`, and `SendObserver_Specs.cs` are removed only after full closure.

## Accepted: type relationships and readable-property reflection

- All 37 inherited type-extension/property identities have exact terminal dispositions against
  source-owner tests; the four replaced fixtures and their resulting empty directory are removed.
- Generic relationship operations now distinguish interface assignability, any closed match,
  deterministic complete enumeration and exactly-one selection. Ambiguity is explicit, no
  MassTransit compatibility alias remains, and every product consumer uses the new API.
- Readable instance/static discovery is named by intent, filters each property's own getter,
  traverses base/interface graphs deterministically and de-duplicates interface diamonds.
- Runtime Future registration rejects unrelated and wrong-state types at the public argument
  boundary. Open generic names format without activation. All three type-keyed caches avoid rooting
  collectible assemblies.
- Thirteen independent one-cause mutations fail for their intended reason. Abstractions passes
  191/191, Core 618/618, UnitArchitecture 1200/1200 and LocalIntegration 3/3 with no failure or skip;
  Release builds have zero warnings and errors, including all 22 retained source projects and the
  remaining inherited Core test project.
- A separate path-complete decision about non-public read/write-property policy and public
  `Internals` ownership is recorded in `TODO.md`; no test encodes that unresolved inherited policy.

## Accepted: middleware coordination and resilience

- All 19 inherited identities from the eleven middleware fixtures have exact terminal replacement
  mappings; all eleven files are removed and no empty inherited middleware directory remains.
- Source-derived tests cover latest-value visibility, recoverable single-flight setup, exact
  cancellation and abandoned-fault observation, fork/join and nested-pipe ordering, partition
  isolation, repeated rate/concurrency adjustment, observer order, rescue diagnostics, distinct
  retry budgets and the imported circuit-breaker resource ownership that was subsequently
  superseded by the greenfield circuit-breaker cohort.
- Product defects were corrected at their owners, including lost cache-cleanup scheduling,
  non-recovering setup state, permit rollback, null routing keys, process-clock timers and competing
  circuit transitions. Tests were not relaxed to preserve any defect.
- Seven one-cause mutations fail for their intended reason. Abstractions passes 201/201, Core
  643/643, UnitArchitecture 1235/1235 and LocalIntegration 3/3; no test fails or skips and affected
  Release builds have zero warnings and errors.

## Accepted: middleware routing, limits and scope

- All eight inherited circuit, scope, concurrency, dispatch and dynamic-router identities have exact
  replacement mappings; their five fixtures are removed.
- Scope payloads remain parent-readable but local-write isolated. Typed and keyed dynamic routing
  fan out only to compatible connections, disconnect exactly, and continue the input pipe once.
- Missing collaborators, invalid converter results, null keys and incompatible output contexts now
  fail at stable product boundaries rather than through latent null/reflection failures.
- Eight one-cause mutations fail for their intended reason. Abstractions passes 203/203, Core
  649/649, UnitArchitecture 1243/1243 and LocalIntegration 3/3; the inherited Core project and final
  Engineering solution build with zero warnings and errors.

## Accepted: middleware retry

- All 17 inherited retry identities have exact terminal executing dispositions. `Retry_Specs.cs`
  and its seven exclusive command/replacement-context support files are removed.
- Fifty-four source-mirrored cases cover exact observer lifecycles, nested and typed-dispatch budget
  ownership, filtering, large iterative budgets, deterministic delay, token identity, policy
  lifetime, validated schedules and both task/result retry execution.
- Product defects are fixed at their owners: recursion, implicit wall-clock delay, false-success
  cancellation, late/null collaborator failures, mutable schedules, incremental overflow and
  unstable per-attempt exponential jitter are absent.
- The unused `MessageRetryPolicyExtensions` duplicate is removed; task retry and configured message
  retry retain the capabilities through their single owning paths.
- Nine one-cause mutations fail for the intended reason. Abstractions passes 203/203, Core 703/703,
  UnitArchitecture 1297/1297 and LocalIntegration 3/3 with no failure or skip; all Release builds
  and the bounded .NET whitespace check pass with zero warnings or errors.

## Accepted: message and host retry integration

- The complete three-fixture source path and its 17 obligations are analyzed; upstream history
  confirms that host send retry accumulated a fixed one-second breather in addition to every
  transport's explicit retry policy.
- Product retry now has one configured delay owner, explicit `TimeProvider`, exact cancellation
  precedence and terminal-failure identity. Message-retry configuration validates every public
  collaborator, bus-level configuration binds the stop lifecycle, policy cancellation is
  registered once, and lifecycle resources cancel before disposal on every terminal path.
- All 17 inherited identities have exact terminal executing dispositions. Nineteen source-mirrored
  cases cover configuration, bus lifecycle, transport-host and polymorphic-dispatch boundaries; the
  three fully replaced inherited fixtures are removed.
- Four independent one-cause production mutations fail for their intended reason. Core passes
  722/722, UnitArchitecture passes 1316/1316 and LocalIntegration passes 3/3 without failure or
  skip. The native UnitArchitecture build, remaining inherited Core project and complete Engineering
  solution build in Release with zero warnings and errors.

## Accepted: configuration composition and validation

- All 29 inherited configuration obligations have exact terminal executing dispositions; the eight
  replaced fixtures and their resulting empty directory are removed.
- One shared fault-sticky observer lifecycle replaces duplicated delayed flags. Send and publish
  composition retain stable owner order, point-in-time validation, re-entry safety, late root
  propagation and recovery after failed initialization.
- Saga discovery is immutable and deterministic by semantic role and ordinal message identity. Both
  supported construction shapes remain; obsolete public metadata remnants are removed.
- Six source-derived hardening cases supplement the 28 newly materialized inherited-replacement
  cases. Ten independent one-cause mutations fail for their intended reasons.
- Abstractions passes 213/213, Core 746/746, UnitArchitecture 1350/1350 and LocalIntegration 3/3,
  all without failure or skip. All final Release builds complete with zero warnings and errors.

## Accepted cleanup: test-local message-group sample

- All three `Groups/Group_Specs.cs` obligations test only code declared in that test file.
- They have terminal non-product dispositions; no product capability or native test is removed.
- The inherited file is removed and the UnitArchitecture floor remains 1350.

## Accepted: pipe-context failure precedence

- All five inherited `PipeContextFailure_Specs.cs` identities have exact native xUnit/MTP
  replacements; the inherited fixture is removed.
- Three source-derived cases additionally prove lifecycle order, exact failure identity,
  cancellation-token propagation and the context-acquisition failure boundary.
- Eight isolated product mutations fail for their intended reasons. Core passes 754/754,
  UnitArchitecture 1358/1358 and LocalIntegration 3/3 without failure or skip.
- The Unit, remaining inherited Core and complete Engineering Release builds have zero warnings and
  zero errors. Only transport-independent product comments and diagnostics changed; operation
  semantics remain the tested A+ behavior.

## Accepted: timeout and cancellation

- All four inherited timeout/cancellation identities have exact native xUnit/MTP replacements; the
  two inherited root fixtures are removed.
- Thirty source-owner cases cover deterministic virtual deadlines, exact nested caller-token
  normalization, independent cancellation, completion/timer ownership, timeout faults,
  transport-stop suppression, every configuration projection, invalid durations and immutable
  compiled configuration.
- Product timeout code now uses context-owned or explicitly configured `TimeProvider`, preserves
  causal exception chains, publishes the correct timeout fault, awaits the complete consume
  lifecycle and hides implementation-only configuration and proxy types.
- Twelve independent one-cause mutations fail for their intended reasons. Core passes 784/784,
  UnitArchitecture 1388/1388 and LocalIntegration 3/3 without failure or skip; final Release builds,
  formatting and static closure gates pass.
- The complete profile exposed a pre-existing race in a saga test. It now waits for the explicit
  consume observation before reading saga state; the product was not changed.

## Accepted: in-memory delay and scheduled publish

- All five inherited delay-provider identities have exact native xUnit/MTP replacements; both old
  root fixtures are removed.
- The product now uses one injected standard `TimeProvider`, one timer and stable ordered deadlines;
  no delay channel, reader task, invalid duplicate comparer or delayed cancellation retention remains.
- `Advance` is applied synchronously, cancellation preserves its caller token, disposal is
  idempotent, and long deadlines respect the .NET timer range.
- Eighteen source-owner cases cover the full direct and real scheduled-publish path. A compiled
  state-machine assurance deterministically prevents the prior `Task.Run` registration race.
- Fourteen independent product/projection attacks fail for their intended causes. Core passes
  802/802, UnitArchitecture 1406/1406 and LocalIntegration 3/3 without failure or skip; all final
  Release builds and bounded formatting checks pass with zero warnings or errors.

## Accepted: bus health waiting

- The complete `BusControlHealthExtensions` owner uses one standard `TimeProvider` path, returns
  the complete successful observation and reports timeout through a typed diagnostic exception.
- Twenty-two ordinary xUnit/MTP facts cover exact and post-deadline behavior, deterministic polling,
  cancellation identity, public validation, single enumeration, concurrent collection startup and
  stable result order; all 22 have exact passive requirement carriers.
- Twelve independent product/projection attacks fail for their intended causes. Abstractions passes
  235/235, UnitArchitecture 1428/1428 and LocalIntegration 3/3 without failure or skip; every direct
  caller and the complete Engineering solution build in Release with zero warnings and errors.
- The retained inherited kill-switch fixtures were mechanically updated to the new API but not
  deleted. Their larger product state machine is the next independent source-owner cohort.

## Accepted: kill-switch lifecycle and recovery

- The complete source owner now uses one internal, synchronized and lifecycle-owned state machine;
  public legacy state types, detached tasks, raw timers and ambiguous threshold/time APIs are gone.
- `TimeProvider`, immutable settings, exact activation/ratio boundaries, one recovery owner,
  bounded pause/start retries, host-stop cancellation and owned log context are enforced.
- The useful but formerly bypassed restart-verification intent is restored as the internal
  `VerifyingRecovery` state: a matching failure re-trips immediately and exactly the configured
  successful population returns the endpoint to normal tracking.
- Twenty-six ordinary xUnit/MTP facts map one-to-one to the passive requirement projection; fifteen
  independent one-cause mutations fail for their intended reasons.
- The three Core inherited fixtures and resulting empty directories are removed; ActiveMQ and
  RabbitMQ fixtures remain for their real-broker cohorts.
- Core passes 828/828, UnitArchitecture 1455/1455 and LocalIntegration 3/3 without failure or skip.
  The complete serial Engineering Release build has zero warnings and zero errors.

## Accepted: fault diagnostics and host metadata

- `FaultExceptionInfo` now snapshots string-keyed, non-null diagnostic data with ordinal
  case-insensitive lookup; later source mutation cannot alter an existing fault and application
  values have explicit precedence.
- The dictionary JSON writer no longer has a self-recursive overload path. Both inherited data
  variants are received after real System.Text.Json transport serialization and exactly one fault
  publication is observed.
- `BusHostInfo` has one public wire constructor and one internal current-process factory based on
  current .NET runtime APIs. Current and empty snapshots remain stable cache-owned values.
- Eleven native cases have exact passive requirement carriers. Core passes 835/835 and Abstractions
  239/239 with zero failures and skips; both focused Release builds have zero warnings and errors.
- All three R0 obligations are replaced and the two inherited fixtures are removed. Seven isolated
  mutations are rejected; UnitArchitecture passes 1466/1466, LocalIntegration passes 3/3, and the
  complete Engineering Release build has zero warnings and zero errors.

## Accepted cleanup: duplicate initializer preservation path

- `OBL-R0-CORE-D-0415` maps exactly to two already accepted native initializer tests whose union is
  stronger than the inherited Guid/string/DateTime assertion set.
- The misleading `SendProxy_Specs.cs` fixture is removed; no product or native-test behavior changes.
- UnitArchitecture remains 1466/1466 and its predeclared floor remains 1466.

## Accepted: InMemory receive-endpoint concurrency

- `OBL-R0-CORE-D-0465` is replaced at the real InMemory endpoint boundary rather than inferred from
  the isolated `TaskExecutor` tests.
- One case admits all one hundred held deliveries; a second case proves that prefetch four with a
  concurrency limit of three keeps the fourth delivery queued until release.
- An isolated mutation that ignores `ConcurrentMessageLimit` fails with an observed maximum of four
  instead of three, then the restored focused pair passes 2/2.
- The inherited `Threading_Specs.cs` fixture is removed. The complete UnitArchitecture profile is
  green at its enforced 1468/1468 floor with zero failures and skips; the serial Release build has
  zero warnings and errors.

## Accepted: structured bus-probe endpoint inventory

- `OBL-R0-CORE-D-0199` is replaced against the real structured bus probe without reviving the old
  TestFramework JSON round trip.
- The probe reports the input, internal bus and dynamic endpoints exactly once; stopping the dynamic
  handle removes only its address and leaves the two persistent endpoints.
- A one-cause rename of the transport `address` probe field makes both native tests fail at the
  missing key; after restoration the focused pair passes 2/2.
- The inherited `Introspection_Specs.cs` fixture is removed. The complete UnitArchitecture profile
  passes its enforced 1470/1470 floor with zero failures and skips; the serial Release build has
  zero warnings and errors.

## Accepted: InMemory outbox fault isolation

- `OBL-R0-CORE-D-0294` is replaced at the real in-memory outbox boundary without the inherited
  300-ms absence window.
- The handler awaits a deferred response and then faults; the request fault is the deterministic
  completion barrier. The exact `Fault<OutboxRequest>` is present and the response snapshot empty.
- Changing only the catch path from discard to execute makes the focused test fail because the
  response escapes. After full-branch restoration, byte comparison and a non-incremental rebuild,
  the restored focused test passes 1/1.
- The inherited `Outbox_Specs.cs` fixture is removed and the enforced UnitArchitecture floor is
  raised to 1471. The complete serial Engineering Release build has zero warnings and errors,
  UnitArchitecture passes 1471/1471 and LocalIntegration passes 3/3, all without failure or skip.

## Accepted: native test source-layout enforcement

- Every native project's namespace/folder relation is derived from evaluated MSBuild `Compile` and
  `RootNamespace` values and parsed with Roslyn; no hand-maintained source list is involved.
- Nine pre-existing support-project mismatches are corrected by two explicit architectural root
  namespaces, without changing assembly identity or public API.
- A wrong-namespace source and, independently, a correct-plus-hidden second namespace each make
  exactly the new rule fail; after both probes are removed the project passes 85/85 with no skip.
- The complete serial Unit and Engineering Release builds have zero warnings and errors. The
  UnitArchitecture floor is raised to 1472 and passes 1472/1472; LocalIntegration passes 3/3,
  both without failure or skip.

## Lead-verified: greenfield circuit breaker

- PO-2026-08-25-04 is implemented as one validated `CircuitBreakerOptions` boundary, an immutable
  runtime snapshot and an internal timer-free compare-and-swap state machine. Public runtime states,
  router events and the inherited configurator API are removed.
- Exactly one caller owns the half-open probe. Success closes and resets bounded backoff; a
  classified failure reopens; caller cancellation and unclassified failures release the probe and
  remain observably half-open. Dependency cancellation remains a classified resource failure.
- Twenty-three ordinary xUnit/MTP facts cover configuration, public surface, both sides of inclusive
  thresholds, exact sampling/open boundaries, immutable filter capture, a real 33-caller probe race,
  failure and cancellation causality, retry/concurrency composition, exact low-cardinality OTel
  signals and observer-failure isolation.
- Thirteen isolated one-cause product mutations fail for their intended reasons: inclusive
  throughput, exact open-time expiry, Open-to-Half-open CAS ownership, backoff progression,
  cancellation causality, release without verdict, OTel meter version, classifier-failure release,
  observer isolation, retained-builder isolation, caller-owned type-array isolation, the `open`
  rejection tag and the `probe_in_progress` rejection tag. Changing the composed concurrency limit
  from two to three is the separate test-setup sabotage. Every mutated source and test setup was
  restored before the final build; the hash-bound raw results are under
  `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/CIRCUIT-BREAKER/`.
- UnitArchitecture passes 1494/1494 and LocalIntegration passes 3/3 with no failure or skip.
  The complete Engineering Release build, including benchmarks and retained transports, has zero
  warnings and zero errors. Bounded `dotnet format whitespace --verify-no-changes` passes.

## Lead implementation: optional message journal

- The inherited `Audit` surface and its replaced fixtures are removed. `MessageJournal` is one
  greenfield, default-off capability with no compatibility alias and no Suite-audit ownership.
- The mandatory policy receives an immutable serialized-envelope capture and is the sole selection
  and redaction owner. EF Core and Azure Table receive only immutable sanitized entries and enforce
  finite entry size, count and age transactionally on each append.
- Journal timeout, policy, clock, provider and OpenTelemetry failures cannot alter the original send,
  publish or consume result. No queue, retry carrier, background cleanup, query API or second outbox
  was introduced.
- The source-mirrored native suite currently passes UnitArchitecture 1557/1557 and LocalIntegration
  17/17 against ephemeral PostgreSQL and pinned Azurite, all with zero failure and zero skip. The
  transition-only Python inventory remains green at 206/206 after dispositioning only the replaced
  inherited audit obligations.

## Lead implementation: RabbitMQ address model

- The complete 588-line inherited `RabbitMqAddress_Specs.cs` fixture and all 46 frozen obligations
  were read and mapped one-to-one before retirement. The replacement is a source-mirrored signed
  xUnit 4/MTP 2 transport project; the old fixture remains available in Git history.
- Host and endpoint addresses are immutable, normalize scheme-specific ports, preserve encoded
  virtual hosts and credentials, own binding snapshots and reject unknown, duplicate, invalid or
  semantically conflicting options. Short addresses reject host settings that cannot take effect.
- TLS follows the scheme rather than a port number, lets the operating system negotiate enabled
  protocols, validates chains and names by default and uses the configured host as the default
  certificate server name. Explicit policy exceptions remain available through the SSL configurator.
- The focused RabbitMQ executable passes 108/108, UnitArchitecture passes 1699/1699 and
  LocalIntegration passes 17/17 against run-scoped PostgreSQL and Azurite, all without failure or
  skip. The complete serial Engineering Release build has zero warnings and zero errors.
- The first independent code and test reviews rejected the initial candidate. Their product-path
  counterexamples are fixed: raw query separators are retained, short names decode symmetrically,
  TTL is a numeric AMQP value, topology reads the final host configuration, and validation covers
  exchange, queue, alternate-exchange and binding inputs. Topology now treats direct and
  formatter-produced exchange names as data rather than URI control text. Eighteen product mutants
  and one requirement-projection sabotage are killed by their owning native tests and bound with
  exact machine-readable recipes, baseline, mutant, raw-result and post-restore hashes.
- The full Unit run exposed a pre-existing race in the TelemetryMonitor test barrier: `PostReceive`
  precedes the product span stop and idle-timer restart. The test clock now observes that causal timer
  restart before advancing; the product timeout was neither increased nor bypassed. The focused
  telemetry cohort now proves the exact due time and just-before boundary, passes 4/4, and the
  complete stationary Unit profile passes 1699/1699.
- The same unfiltered validation exposed a missing causal barrier in a pre-existing Saga test. It
  now waits for actual consumption before reading state written during `Consume`; the focused test
  passed 10/10 consecutive runs without timeout, retry or product-semantic changes.

## Lead implementation: retired native-replaced legacy test projects

- The inherited Analyzer and SignalR NUnit/VSTest projects are retired atomically with their root
  solution entries, legacy CI jobs and expected-identity files. Their source remains available in
  Git history. No product source or native `tests2` source changed.
- The inherited verification model no longer invents runs for those retired projects or for the
  already retired Abstractions project. It classifies the retained product projects exactly once as
  native-test-owned capabilities, and now gives the retained OrderWorkflow sample an explicit
  compile-proof owner.
- The terminal disposition evidence remains complete: Abstractions 70 executable replacements plus
  eight non-product or non-executing obligations, Analyzer 115/115 with 143 replacement bindings,
  and SignalR 26 executable replacements plus two non-executing inherited methods.
- The transition-tool self-tests pass 253/253 under `tools/ci` and 103/103 under `tools/identity`.
  The focused native suites pass Analyzer 114/114, Analyzer CodeFixes 29/29 and
  SignalR 32/32, all without skips.
- A locked static-graph restore and the complete Root and Engineering Release builds pass; both
  builds have zero warnings and zero errors. The unfiltered UnitArchitecture profile remains at its
  enforced 1699/1699 floor with zero failures and zero skips.
- The independent review found that the verification model checked only its internal job-to-selection
  map, not the command executed by the required workflow. Every verifying job is now bound to exactly
  one unconditional canonical `verify.py` step, with wrong, missing, duplicate, conditional,
  continue-on-error and shell-composed calls rejected. Required CI executes the model validator.
- The removed Abstractions text fixture is no longer a legal-format exception. The four retained
  commentless or binary files now carry explicit baseline-source-to-current-target bindings, including
  normalized solution and transport-test paths; `NOTICE` and `MODIFICATIONS.md` agree exactly.
- The second review rejected job-level default shells, additional shell steps and a self-derived
  provenance oracle. Verification jobs now have exactly four allowed steps: pinned checkout, pinned
  SDK setup, the canonical selection call and pinned result upload. An independent literal legal
  contract checks all four unique source-to-target pairs and the exact NOTICE/MODIFICATIONS sections.
- The workflow reader accepts only the canonical unquoted plain top-level keys used by this repository.
  Workflow-level defaults and quoted, escaped, tagged, complex, anchored, merged or BOM-prefixed key
  forms are rejected before job parsing. The complete header also binds the required push, pull-request
  and manual triggers, read-only permission and exact global environment; its SDK value is derived from
  `global.json`. Every modeled Verify job independently enforces its exact plain-key structure, runner,
  timeout, optional fixture environment and steps. The required tooling, build and pack jobs bind every
  executable line, dependency and pinned action. YAML spelling or a no-op job therefore cannot hide or
  replace any required gate.

## Lead implementation: native core pipeline closure

- All 34 executed inherited obligations `OBL-R0-CORE-B-0426` through `0459` have one terminal,
  machine-readable replacement disposition. The thirteen retired NUnit Pipeline files are removed;
  no generic replacement Pipeline folder or second verdict mechanism exists.
- Source-owned xUnit 4/MTP 2 tests now cover dynamic consumer, instance and handler connections,
  completed disconnects, typed and untyped observer lifecycle, synchronous and asynchronous context
  filtering, exact retry attempts and cancellation causality, consumer/send/publish configuration
  layering, partition conventions, and transaction configuration, scope and retry ownership.
- Product boundaries fail fast for invalid handler, context-filter and transaction collaborators.
  Retry distinguishes an equal but unrequested cancellation token from caller cancellation.
  Transaction scopes enable asynchronous flow by default; an external transaction remains externally
  owned, while every retry receives a fresh internally owned transaction context. The BCL adapter is
  internal and the public capability remains the neutral `TransactionContext` contract.
- The first one-cause retry mutation proved the initial-attempt guard but exposed that the active
  retry-attempt branch had no matching token oracle. A dedicated retry-context policy now supplies
  that exact unrequested token; the new native test reaches a third successful attempt only when the
  active-attempt guard remains causal.
- Every newly written asynchronous harness boundary uses the central operation timeout and the MTP
  cancellation token. Concurrency releases are guaranteed in `finally`; no sleep, delay, stopwatch,
  random scheduling, negative wait or absence-until-timeout oracle was introduced.
- Locked restore is current. The final focused Release build has zero warnings and zero errors and its
  executable passes 954/954. The complete UnitArchitecture profile passes 1735/1735 and
  LocalIntegration passes 17/17 against run-scoped PostgreSQL and Azurite, all with zero failure and
  zero skip. The complete Engineering Release build has zero warnings and zero errors.

## Lead implementation: Quartz scheduling integration

- All 87 inherited Quartz obligations `OBL-R0-PER-0200` through `0286` have one terminal,
  machine-readable disposition. Eighty-five are carried by executable native tests; the two
  non-product rows are explicitly classified as upstream Quartz compatibility and an invalid
  duplicate inherited test rather than counted as local behavior.
- The replacement is a signed, source-mirrored xUnit 4 executable on Microsoft Testing Platform 2.
  It covers scheduler registration and lifecycle, immutable options, one-time and recurring
  controls, payload/header/transport-property restoration, trace and expiration propagation,
  redelivery scopes and intervals, courier, outbox, missing-saga, nested request, saga scheduling
  and the complete retained job-service lifecycle.
- Product corrections remove the process-global Quartz clock hook, use the standard `TimeProvider`
  boundary for outgoing expiration, deserialize persisted System.Text.Json metadata correctly,
  centralize recurring trigger identities, preserve raw-serializer trace headers, recreate a
  deleted durable job and distinguish requested cancellation from a dependency-thrown
  `OperationCanceledException` during scheduled redelivery.
- Runtime configuration is validated and frozen into immutable settings before bus start. The test
  harness uses causal completion barriers and the central operation timeout; no skip, sleep, delay,
  stopwatch threshold, direct `DateTime.Now/UtcNow`, blocking wait or assertion inside an
  asynchronous consumer remains. Early assertions cannot strand concurrency-controlled jobs.
- The inherited Quartz NUnit/VSTest project, its expected-identity file and its old workflow job are
  removed; the product root solution no longer compiles any inherited `tests/**` project. Empty
  retired directories and the misplaced empty root `TestResults` directory are gone.
- The focused Release build has zero warnings and zero errors and the native Quartz executable passes
  85/85 with zero failure or skip. The complete serial UnitArchitecture profile passes 1822/1822
  with zero failure or skip. The sandbox-specific MTP NamedPipe failure is not treated as product
  evidence: focused runs use the built MTP executable directly and the full CLI run executes outside
  the sandbox, as recorded by TLP-027.
- The first scheduled-redelivery cancellation mutant survived because the end-to-end handler wrapped
  the exception before the `RedeliveryRetryFilter` boundary. The owner test now injects the same
  unrequested token directly in that filter's downstream pipe; the unmodified product redelivers and
  the exact guard mutation fails at the bounded operation timeout. This was a test correction only.
- Twelve byte-exact product mutations now prove the configured clock, leading-prefix semantics,
  immutable scheduler, hosted-service and endpoint settings, public prefetch validation,
  application-owned clock, persisted header and transport-property deserialization, raw trace
  propagation, durable-job recreation and redelivery cancellation causality. All twelve build with
  zero error, fail at their intended native owner and restore the frozen bytes exactly. The final Engineering Release
  build is 0-warning/0-error, UnitArchitecture is 1822/1822, focused Quartz is 85/85 and
  LocalIntegration is 17/17 against run-scoped PostgreSQL and Azurite, all with zero skip.
- The independent product review found that `ScheduledMessageJob` converted a causally requested
  Quartz execution cancellation into an immediate-refire `JobExecutionException`. The product now
  propagates requested context cancellation unchanged while retaining retry classification for an
  unrequested dependency cancellation. Two direct send-boundary tests raise the Quartz floor to
  87 and the UnitArchitecture floor to 1824.
- The correction is frozen at technical commit `64e88ba5b79a67234bc61f4d65468864f3c3fd46`.
  Its final Engineering Release build has zero warnings and zero errors; UnitArchitecture is
  1824/1824, focused Quartz is 87/87 and LocalIntegration is 17/17, all with zero failure or skip.
  M13 removes only the cancellation boundary: the requested-cancellation owner fails while the
  unrequested-dependency control remains green, and the product source restores byte-identically.

## Lead implementation: Entity Framework Core persistence and outbox foundation

- Technical commit `8ea356043316861f0ac983de5fa8470410e2f9ca`, tree
  `3d645d11219a32a05280f70310a72f5dc8f9afaa`, is the frozen implementation subject.
- SQLite, PostgreSQL and SQL Server/Azure SQL remain supported; unrequired MySQL and Oracle adapter
  APIs are removed. Provider selection is explicit and runtime configuration is immutable.
- SQL generation resolves every table, schema, property and sort column from the exact EF model,
  quotes provider identifiers and does not freeze the first model in a process-global cache.
- Saga load/query and transaction behavior uses one configured strategy. Insert-race recovery is
  cancellation-safe and accepts only the exact failed saga entry followed by the exact existing
  identity. SQLite optimistic concurrency and real PostgreSQL pessimistic locking are both proved.
- Outbox writes have one scoped coordination owner and pure envelope factory. Public EF execution
  strategies govern retry; message-text heuristics and concrete-strategy type checks are gone.
  Concurrent inbox deliveries deduplicate effects while preserving the actual outer-delivery count.
- Outbox notification uses the injected `TimeProvider`, retains a signal delivered before waiter
  registration and rejects a second concurrent waiter without replacing the first.
- Locked Engineering restore passes. The complete Engineering Release build has zero warnings and
  zero errors. UnitArchitecture passes 1867/1867 and LocalIntegration passes 28/28 against
  run-scoped PostgreSQL and Azurite, with zero failure and zero skip.
- Three exact one-cause mutations independently prove retained pre-wait notification, SQL Server
  sort-column quoting and exact EF failed-entry classification. Each mutant builds, fails only its
  intended native owner with exit code 2 and restores the frozen source hashes exactly.
- Full validation is recorded under
  `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/ENTITY-FRAMEWORK-CORE/`.
- The second Entity Framework slice proves persistent saga lifecycle, transaction policy, read-only
  state-machine events, same-correlation PostgreSQL row-lock serialization and required two-level
  navigation graphs through the real repository path. Read-only behavior, repository lifecycle and
  concurrency have separate source-mirrored test owners rather than one mixed fixture.
- A real configuration defect was found and corrected: `SetOptimisticConcurrency()` bypassed the
  isolation policy used by the `ConcurrencyMode` property and therefore retained the pessimistic
  `Serializable` default. Both public configuration paths now share one policy owner and produce the
  intended `ReadCommitted` optimistic default while retaining explicit caller configuration.
- The complete locked Engineering restore and Release build pass with zero warnings and zero errors.
  The unfiltered UnitArchitecture profile passes 1868/1868 and LocalIntegration passes 35/35 against
  fresh run-scoped PostgreSQL and Azurite, all with zero failure and zero skip. The inherited EF test
  project remains unchanged until all 90 assigned obligations can be retired atomically.
- The second EF subject is frozen at technical commit `f1096d531e00e64f2683d4e41542630df2af4550`.
  Four isolated one-cause probes independently kill a bypass of the optimistic isolation policy, a
  writable read-only event, omitted pessimistic query customization and omitted PostgreSQL row
  locking. Every changed byte restored to its recorded baseline hash before the detached worktree
  was closed.
- The third EF tranche reconstructs `OBL-R0-PER-0018..0032` as fifteen independent semantic owners
  plus one new rollback-hardening owner. Its source-mirrored LocalIntegration tests cover exact
  publish/send scopes, typed constraint faults and endpoint recovery, request-saga fault identity,
  single terminal fault after retry, OTel and scoped endpoint proxies, deterministic delayed
  responses, EF-to-Quartz commit ordering, reliable consumer/saga rollback, real send-pipeline
  recovery and persisted routing keys.
- Success and retry of the reliable consumer are separate facts because they carry independent R0
  obligations; no theory row claims both. The inherited `VSB-Fail-Delivery` header and product
  branch are removed. The replacement failure is raised by a test-owned normal `ISendObserver` at
  the actual serialized transport-send boundary, with two delivery attempts, one saga attempt and
  one terminal state transition.
- The complete locked Engineering restore passes and the complete Engineering Release build has
  zero warnings and zero errors. The unfiltered native UnitArchitecture profile passes 1,868/1,868;
  LocalIntegration passes 51/51 against fresh run-scoped PostgreSQL and Azurite resources
  (`vicione-e96131c6c3d1`); both have zero failure and zero skip. The inherited EF test project is
  retained unchanged until the full 90-obligation assignment can be retired atomically.
- The third EF subject is frozen at technical commit
  `b95faaeff0be853baa4747df1366f15049d3ec0b`, tree
  `9598ce52248094920b1c59c646b50bb8f37d7288`. Its fifteen inherited dispositions and one separate
  rollback-hardening owner are bound under the EF evidence root. Six exact one-cause probes cover
  routing-property restoration, EF-to-Quartz commit ordering, send-failure retry, delayed request
  identity, originating publish scope and first-attempt rollback; all six restore byte-identically.

## Lead closure: Entity Framework Core retry state and inherited-project retirement

- This section supersedes the earlier mutable EF status paragraphs above. They remain append-only
  history only. In particular, the former statements that the inherited EF project remains retained
  and that `ENTITY-FRAMEWORK-CORE/` is the current evidence root are no longer active.
- The final technical subject is commit `6bf471d954fb50138473cb1f2c0275362df5aebe`, tree
  `098f6dcb9bae0c69f626d3cecbeb41cd91327738`. Its evidence is bound by direct child commit
  `fc7a1fef03c898a243d29139ae72e0fc5ae0059e`, tree
  `4afc7becf1bb1bb73dec21e1fe2b9bfa35e6d2dc`, under
  `ENTITY-FRAMEWORK-CORE-CORRECTION-02/`.
- All 90 inherited EF obligations expand to 156 execution identities and have one explicit final
  disposition: 77 execute natively, 39 are consolidated only where behavior is genuinely
  provider-neutral, and 40 SQL Server/Azure SQL identities remain visible as `EXTERNAL_PENDING`.
  Pending identities are not counted green. The inherited EF test project is retired; its source
  remains available in Git history.
- Failed EF retry attempts clear stale tracker state and roll back only the attempt-owned in-memory
  outbox tail. If rollback cleanup itself fails, provider re-entry is blocked and the exact original
  operation failure is rethrown; cleanup failure is logged separately and cannot cause a duplicate
  business attempt.
- In-memory outbox checkpoints are opaque and owner-bound. Batch checkpoints compose the parent and
  every child context. Scheduler rollback is exercised through the real outer checkpoint API, and a
  scheduled item is removed only after cancellation succeeds, so failed cleanup remains recoverable.
- The locked Engineering restore and complete Engineering Release build pass with zero warnings and
  zero errors. UnitArchitecture passes 1,886/1,886; focused EF UnitArchitecture passes 55/55;
  LocalIntegration passes 70/70 against fresh PostgreSQL and Azurite; focused EF LocalIntegration
  passes 59/59 against fresh PostgreSQL. Every run has zero failure and zero skip.
- Five byte-exact one-cause probes M07 through M11 independently protect scheduler delegation,
  retry blocking after cleanup failure, retention after failed unschedule, child checkpoint capture
  and child rollback. Every mutant builds cleanly, fails its exact xUnit 4/MTP 2 owner with exit 2,
  and restores the frozen source bytes exactly. Thirty evidence files are SHA-256 complete.
- The repeatable execution rule is explicit: local integration requires
  `VICIONE_TESTS__Profile=LocalIntegration`; the CI process-tree tests must execute outside the
  filesystem sandbox because they deliberately invoke `ps`; and builds sharing one `artifacts/sdk`
  tree run serially to avoid artificial output contention.

## Lead correction: EF retry failure identity and composed scheduler rollback

- This section supersedes the retry-failure and scheduler-rollback conclusions in the immediately
  preceding EF closure section. The two independent reviews of that subject found that a real EF
  execution strategy could wrap the authoritative operation failure after the retry guard blocked
  re-entry, and that the composed outer checkpoint-to-scheduler failure path was not yet protected
  by one executable owner.
- The final technical subject is commit `36fd3460421b45e475cd190873667afca5697a29`, tree
  `50205a748f4ea8a1f971c8c163129b87ff7f45a5`. Its complete raw evidence is bound by direct child
  commit `119a427938012ef8d5b20de989a0fb9ab2fe72a1`, tree
  `ec3aef62e61a49e1dc9fb20c828f177e4bad7f60`, under
  `ENTITY-FRAMEWORK-CORE-CORRECTION-03/`.
- Once outbox rollback fails, the strategy delegate receives a private non-transient stop signal.
  Outside the provider strategy, the stored exception-dispatch information rethrows the exact
  original operation failure even if the provider wraps the stop signal in
  `RetryLimitExceededException` or consumes it and returns. No second business attempt can run.
- The integrated owner uses the real `InMemoryOutboxConsumeContext`, outer `OutboxCheckpoint`,
  scheduler context and failed `CancelScheduledSend` path. It proves one business attempt, blocked
  provider re-entry, exact top-level exception identity, retained recoverable scheduled work and a
  successful final cleanup. The requested-cancellation case independently preserves the original
  `OperationCanceledException` instance and caller token across a wrapping strategy.
- The locked Engineering restore and complete Engineering Release build pass with zero warnings and
  zero errors. UnitArchitecture passes 1,886/1,886; focused EF UnitArchitecture passes 55/55;
  LocalIntegration passes 70/70 against fresh PostgreSQL and Azurite; focused EF LocalIntegration
  passes 59/59 against fresh PostgreSQL. Every run has zero failure and zero skip.
- M12 removes only the post-strategy projection of the original failure. M13 swallows only the
  scheduler-cancellation failure at the outer checkpoint boundary. Both mutants build with zero
  warning and error, fail the exact integrated owner with exit code 2 for their distinct causal
  reason, and restore the technical source hashes exactly. The evidence binds exact patches,
  mutant hashes, commands, CTRF results, binary logs, raw positive logs and restore hashes.
- Two operator errors were fail-closed and are retained as diagnostics: a direct LocalIntegration
  invocation without the fixture runner was rejected for missing run-scoped credentials, and a
  fixture-runner invocation without `VICIONE_TESTS__Profile=LocalIntegration` was rejected by the
  central profile guard. Neither result is counted as product evidence.
- Independent product and test/evidence re-reviews of this exact correction remain required before
  the active product worktree may advance.

## Lead correction: consumed EF stop sentinel and retained cache cleanup signal

- This section supersedes the final-review conclusion immediately above. The preceding correction
  remains append-only history; its reviewers correctly found that the post-success retry-guard
  projection had no executing owner.
- The final technical subject is commit `2a3926b0bb906e7b89c5001a3301634414ac9acc`, tree
  `41657e4d9ee0f472c5af5261f4c0e7f8c829dff6`. Its direct evidence child is commit
  `d3a59f0b6a3e69d3658940574568db7e23290519`, tree
  `9f26bfa4a31e70891967de28a15e0d5ce88ae0b0`, under
  `ENTITY-FRAMEWORK-CORE-CORRECTION-04/`.
- A deliberately consuming custom `IExecutionStrategy` now proves that incomplete outbox rollback
  cannot be turned into success: one business attempt and one rollback attempt occur, the cleanup
  failure remains observable in the consumed sentinel and the exact original business exception
  crosses the outer boundary. M14 removes only the post-return projection and the exact owner fails.
- The unfiltered gate then found a real pre-existing `GreenCache` race. A capacity signal arriving
  while the single cleanup was already queued could be lost, leaving the cache permanently above
  capacity if no later operation arrived. The tracker now retains one follow-up signal and reserves
  it atomically while preserving single-flight cleanup. A deterministic internal scheduler seam
  proves convergence without sleeps, wall-clock timing or a further cache operation. M15 discards
  only that retained signal and the exact owner fails in 41 ms.
- The final locked Engineering restore and complete Engineering Release build pass with zero
  warnings and zero errors. UnitArchitecture passes 1888/1888; focused EF UnitArchitecture passes
  56/56; LocalIntegration passes 70/70 against fresh PostgreSQL and Azurite; focused EF
  LocalIntegration passes 59/59 against fresh PostgreSQL. Every accepted run has zero failure and
  zero skip.
- The inherited EF disposition is unchanged: 90 obligations expand to 156 execution identities;
  77 execute, 39 are provider-neutral and 40 SQL Server/Azure SQL identities remain visibly
  `EXTERNAL_PENDING`. The complete EF mutation set is now M01 through M15.
- Two failed full runs are retained only as fail-closed diagnostics: the first rejected a stale CI
  floor and the second exposed the lost cache cleanup signal. Neither is counted as positive
  evidence; both causes are corrected in the final technical commit.
- Independent product and test/evidence reviews of this exact technical/evidence pair remain the
  final acceptance gate. No active product worktree or remote reference has advanced yet.
