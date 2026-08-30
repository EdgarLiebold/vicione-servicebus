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

## Lead correction: cache ring, lifecycle and concurrency safety

- This section supersedes correction 05 as the active cache conclusion. Earlier EF/cache sections
  remain immutable history; the EF 90-to-156 disposition is unchanged at 77 executing identities,
  39 provider-neutral consolidations and 40 explicit SQL Server External identities.
- The final technical subject is commit `9a3b9fb98f8ad1669570c16a2d18ef9642520d92`,
  tree `52385f1b94f8d5cbc384f6690c9a765294b6815f`, parent
  `f43db446c915d6554510a9eed60e019876693205`. Its active raw evidence is
  `ENTITY-FRAMEWORK-CORE-CORRECTION-06/`.
- Cache cleanup remains single-flight and cannot lose ownership across scheduler rejection,
  synchronous callback-then-throw, reentrancy, observer failure or follow-up handoff. Ring
  segmentation cannot overwrite live buckets, and every eviction cause obeys MinAge.
- Rebucket and source-count transfer are atomic under the tracker lock. Bucket and node state have
  explicit volatile/interlocked publication; stale same-key index events cannot remove or overwrite
  a newer generation. Clock, observer, usage-event and disposal callbacks execute outside the
  tracker lock. Reset disposal is non-blocking and starts only after reset/add publication.
- The public observer contract remains explicitly concurrent and unordered. The rejected unbounded
  publication-queue experiment is absent. A separate A+ design for fail-closed internal index
  projection and bounded external notification is preserved in `TODO.md`.
- Locked Engineering restore and the complete Engineering Release build pass; the build has zero
  warnings and errors. UnitArchitecture passes 1914/1914 and LocalIntegration passes 70/70 against
  fresh run-scoped PostgreSQL and Azurite, all with zero failures and skips. Each of the four new
  cache owners also passes independently with a bound CTRF.
- Eight exact one-cause probes M16 through M23 build successfully and each fails its owning test
  with exit 2 for its own reason. A ninth exploratory current-bucket predicate mutation is logically
  equivalent and is explicitly excluded from the mutation score.
- Both independent technical reviews passed the frozen technical commit with no BLOCKER, MAJOR or
  MINOR. The direct Evidence-child and its independent Evidence-only review are the remaining
  acceptance step.

## Azure Table native persistence closure

- The active technical subject is commit `f0a18299aa849b261d948c92aef2c8dfaa408313`, tree
  `545682603c03a16f6913caaab4665bfde463b248`, authorized by architecture commit
  `2c8efb961dc28b45906eaedd92853ecb4997c18b`. Its evidence root is `AZURE-TABLE/`.
- The 39-row R0 set is fully and honestly disposed: 26 native executing replacements, 10
  PO-superseded suite-audit rows and 3 explicit External-pending cloud obligations. Cosmos DB for
  Table compatibility/429, Entra ID authentication/token refresh and real Azure service limits are
  not claimed from Azurite and remain bound in `TODO.md`.
- The product now has one key-validation authority, exact caller-cancellation and ETag semantics,
  409-only duplicate classification, fail-closed entity conversion and fail-fast configuration
  boundaries. Tests do not compensate for the inherited product defects.
- The complete locked Engineering restore and Release build pass with zero warnings/errors.
  UnitArchitecture passes 1946/1946 and LocalIntegration passes 87/87 against fresh PostgreSQL and
  Azurite, all with zero failure and zero skip. Azure Table contributes 43 focused UnitArchitecture
  cases and 25 LocalIntegration cases.
- Twelve exact one-cause product mutations M01 through M12 build successfully and each fails its
  owning native test with exit 2 for its own documented reason. Post-restore source hashes match the
  technical commit exactly; the final focused Azure Table rerun passes 43/43.
- The complete obsolete `tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests` project is removed
  after closure; all 18 files are gone and no empty directory remains. The unfiltered gate also
  exposed and closed an inherited KillSwitch test-driver lost-wakeup race; this was a test-infra
  correction, not an Azure Table product workaround.
- Independent frozen product/code and test/evidence reviews remain the final acceptance gate before
  this branch is merged or the next persistence cohort begins.

## Lead correction: Azure Table cancellation, API, atomicity and DI closure

- This section supersedes the mutable Azure Table conclusion immediately above. The accepted R0
  disposition remains unchanged: 26 native executing replacements, 10 PO-superseded suite-audit
  rows and 3 explicit External cloud obligations. No External result is counted green.
- The corrected technical subject is commit `497a7cd5570969bee13f35abb46bb37a7372dbee`, tree
  `8e25a9d98d39bd71e1afc05e73e6b18b462d932d`, parent
  `1faa2710dfeb0b961110fdc67818c34bd1f04377`. Its correction evidence is under
  `AZURE-TABLE/CORRECTION/` and is authorized by architecture commit
  `2c8efb961dc28b45906eaedd92853ecb4997c18b`.
- Requested caller cancellation now survives update/delete even when the Azure SDK reports default
  or linked exception tokens; non-requested dependency cancellation remains a saga failure. The
  public composition API uses `TableClientFactory` exclusively, legacy CloudTable provider names are
  removed, and the implementation-only provider stays internal.
- Default Microsoft DI resolves both repository contracts through the internal provider-backed
  concrete factory. A real `ServiceCollection` regression owner protects that mapping.
- MessageJournal capacity is protected by a real eight-writer Azurite ETag barrier with exactly one
  successful append and seven HTTP 412 conflicts. A separate hermetic owner proves one ordered
  entity-group transaction containing lease update, exact pruning and add; neither test substitutes
  for the other.
- Locked Engineering restore and complete Engineering Release build pass with zero warnings and
  errors. UnitArchitecture passes 1953/1953; LocalIntegration passes 87/87 against fresh run-scoped
  PostgreSQL and Azurite; focused Azure Table Unit passes 50/50. Every accepted run has zero failure
  and zero skip.
- Sixteen exact one-cause mutations M01 through M16 build cleanly and each fails its owning xUnit 4 /
  Microsoft Testing Platform test with exit 2. M13 protects alternate-token cancellation, M14 the
  real ETag lease, M15 the complete atomic action list and M16 the DI projection. The HTTP-500 M05
  now fails at its intended no-exception assertion without a helper-induced secondary failure.
- Real Azure was not executed. Cosmos DB for Table behavior, Entra ID and service-limit evidence stay
  visibly External in `TODO.md`. The product branch remains unpushed until both independent read-only
  reviewers accept the exact technical/evidence pair.

## Lead evidence correction: Azure Table M14 fixture lifecycle

- The technical subject remains byte-identical at `497a7cd5570969bee13f35abb46bb37a7372dbee`;
  no product, test, build or CI file changed after its freeze.
- The independent technical review found no BLOCKER, MAJOR or MINOR. The test/evidence review found
  one MINOR only: M14 had a causal CTRF and exact command, but no bound wrapper/fixture lifecycle log.
- `AZURE-TABLE/CORRECTION-02/` repeats only M14 against a fresh run-scoped Azurite fixture and binds
  wrapper stdout/stderr, endpoint projection, broker log, empty teardown findings, CTRF, exact command
  and byte-identical post-restore source. This section supersedes only that missing-evidence statement;
  all technical conclusions and the explicit External cloud boundary remain unchanged.

## AWS native closure preparation

- Architecture assignment `PO-2026-08-27-02` authorizes analysis and an immutable pre-edit plan from
  accepted product baseline `427894348e992551c8d2ae15095c416d2cc1b329`, tree
  `ae33fd04f6a3957b00ccb4ee59889d91383678e5`. No product, test, build, fixture or CI file has been
  edited in this preparation phase.
- The complete SQS/SNS, DynamoDB and Amazon S3 product/test boundary and all 111 selected R0 rows are
  read. The exact carrier projection contains 101 locally executable replacements, nine explicit
  External-pending real-AWS proofs and one PO-superseded raw-secret API row. It is stored in
  `.testagent/aws-native-obligation-map.tsv` and has no missing, duplicate or extra obligation ID.
- The Greenfield decisions are fixed before implementation: credentials are AWS-SDK-chain or an
  explicit `AWSCredentials` object, never public raw secret strings; LocalStack is test-owned; and
  ViciOne's distinct `_error`/`_skipped` transports reject a simultaneous native receive-queue
  RedrivePolicy instead of allowing two settlement owners.
- The next permitted action after this preparation commit is architecture hash binding. Product and
  test edits remain blocked until that binding names the exact research, plan and 111-row map bytes.

## AWS native closure: accepted infrastructure foundation

- Architecture commit `d284f7a` binds the corrected research/plan checkpoint and opens the exact
  implementation scope. The correction records the measured LocalStack licensing boundary rather
  than silently introducing a cloud auth token.
- LocalStack Community 4.14.0 is pinned at multi-architecture digest
  `sha256:3ebc37595918b8accb852f8048fef2aff047d465167edd655528065b07bc364a`.
  The canonical runner started SQS, SNS, DynamoDB and S3 on Docker-selected loopback port 33506,
  projected fresh standard AWS provider-chain credentials only to the child, verified all four
  services and removed the container/network afterwards. The rejected 2026.08.0 run is retained only
  as research evidence that current unified images require an account-bound license.
- The single typed test configuration now owns non-secret LocalStack host, port, region and account
  coordinates. The configuration project passes 81/81; CI tool tests pass 257/257; UnitArchitecture
  and LocalIntegration solution graphs build Release with zero warnings and zero errors.
- Six xUnit 4/MTP v2 project boundaries and their locked package closures are present: SQS/SNS,
  DynamoDB and S3 each have separate UnitArchitecture and LocalIntegration owners. No NUnit, VSTest,
  adapter or framework-specific ArchUnit package enters any new closure. The projects are structural
  owners only at this checkpoint; no empty project is counted as a test result or AWS behavior proof.
- The repeatable local execution diagnosis is now durable in `.testagent/research.md`: an MTP
  NamedPipe `SocketException (13)` and process-tree tests denied `ps` are sandbox boundary failures;
  identical commands pass outside that boundary. Restore network stalls are handled the same way,
  without modifying tests, runner selection or package truth.

## AWS native closure — technical candidate, not final acceptance

- The 111 frozen AWS obligations are still unique and complete. Their current dispositions are
  exactly 100 native executing replacements, nine real-AWS `EXTERNAL_PENDING` rows, one invalid
  duplicate retired row and one PO-superseded raw-secret API row. No External result is counted
  green.
- SQS/SNS now has one immutable host/configuration boundary, no public raw-secret path, exact
  address/name/range/equality rules, explicit product-owned error/skipped settlement, provider-failure
  continuity, cancellation ownership, single-flight lifecycle operations and causal visibility
  renewal. Its native projects pass 46/46 Unit and 48/48 LocalStack cases.
- DynamoDB now freezes and validates configuration, creates a fresh SDK context per operation,
  forwards cancellation, preserves provider exceptions, restores failed-update versions, rejects
  overflow before I/O, writes numeric TTL from injected time and conditionally protects both update
  and delete. Its native projects pass 6/6 Unit and 9/9 LocalStack cases, including a real
  four-writer saga convergence test and stale-delete protection.
- S3 retains its accepted real-client boundary and now canonicalizes only the complete product-owned
  lifecycle rule while preserving foreign rules/actions. It passes 6/6 Unit and 5/5 LocalStack.
  Arbitrary per-message TTL and real service-controlled lifecycle deletion remain explicit product /
  External work in `TODO.md`.
- The obsolete inherited SQS project (31 files) and DynamoDB project (7 files) are removed only after
  carrier closure; their empty directories are gone. The native source tree mirrors product owners,
  including the corrected SQS deploy-topology contract folder.
- Locked UnitArchitecture and LocalIntegration restores pass. Release builds are zero-warning and
  zero-error. The final unfiltered local runs pass UnitArchitecture 2032/2032 and LocalIntegration
  149/149 against fresh run-scoped PostgreSQL, Azurite and LocalStack, with zero failure and zero
  skip. The complete Engineering locked restore and Release build also pass with zero warnings and
  errors. Verification-model classification passes.
- This is a technical candidate only. The worktree is not yet the final frozen technical/evidence
  pair; exact one-cause mutation evidence, clean commit binding, independent read-only review and
  remote backup remain required before final acceptance. Real AWS was not executed.

## AWS native closure — frozen technical and evidence candidate

- This section supersedes only the unfinished-evidence statement immediately above. The frozen
  technical subject is commit `e18fcc071ba5bd42d51237a8be8cf5785f00e5c9`, tree
  `52702c9b1012aa1d98d403ec5702a54c96086f35`, and is remotely backed up at
  `origin/feature/aws-native-closure`. Its accepted baseline, architecture authority and exact
  195-path technical projection are bound under `AWS-NATIVE-CLOSURE/`.
- The unfiltered UnitArchitecture solution passes 2,032/2,032. Seventeen independently named CTRFs
  sum to the same 2,032 results. The unfiltered LocalIntegration solution passes 149/149 against one
  fresh PostgreSQL/Azurite/LocalStack fixture; six independently named CTRFs sum to the same 149.
  Every accepted result has zero failure, skip, pending or other status.
- Unit, LocalIntegration and complete Engineering Release builds finish with zero warnings and
  errors and have bound MSBuild binlogs. The positive broker fixture has no teardown finding and
  binds all three broker-log digests without committing its run-scoped ownership token.
- Exact one-occurrence mutations M01–M14 were reconstructed from the frozen technical bytes. All 14
  build successfully and each owning xUnit 4 / Microsoft Testing Platform test exits 2 with one
  causal failure and zero skip. M11 and M12 additionally bind a fresh LocalStack lifecycle from
  wrapper start through endpoint projection, broker log and clean teardown. Every source restores
  byte-identically; the disposable mutation worktree is clean at the technical commit.
- The 111-row disposition is unchanged and honest: 100 executing replacements, nine real-AWS
  External rows, one invalid duplicate retired row and one PO-superseded raw-secret API row. Real AWS
  was not executed and no cloud-only result is counted green.
- The evidence candidate is ready for its separate commit, remote backup, architecture binding and
  independent read-only acceptance reviews. No such independent PASS is claimed in this section.

## AWS native closure — remote freeze and review boundary

- This section supersedes only the final pre-commit sentence immediately above. The technical
  subject remains `e18fcc071ba5bd42d51237a8be8cf5785f00e5c9`, tree
  `52702c9b1012aa1d98d403ec5702a54c96086f35`. Its direct evidence child is
  `a3d32c07a084b7f6996467beb8db4ed3282d1675`, tree
  `e7803344e323e9e0ff76688df1caddf248d53b39`; the additive wording correction is
  `5cac5a391a435b651afe58e9fcc08b109ac22c21`, tree
  `4c79119beefa84516cdc3134ef3446a9a952408c`.
- The complete chain is backed up on `origin/feature/aws-native-closure`. Architecture commit
  `5982fe32b27ef3c8dd2bbb3c385894f3df2d19ed` binds the exact technical, evidence and correction
  identities, the measured result set and both deliberately open boundaries.
- The local execution and evidence package are stationary and complete. Final acceptance still
  requires two independent static read-only PASS reviews. Real AWS remains a separate External
  release gate; neither condition is represented as locally green.

## AWS native closure — correction candidate after targeted failure-boundary audit

- This section supersedes only the earlier AWS technical subject and its 2,032 Unit count. The
  correction technical subject is commit `d0043bae8dccfee74d8ae223b5e8031409a2784d`, tree
  `3380c74e8594c5cca510df15e1c89cea40f068af`, direct child of the remotely bound AWS evidence
  state `0ff3e84e966d469a1b77048da6f84b52040ed23a`. Its exact technical delta contains 18 paths.
- DynamoDB now freezes the registered context-factory delegate and fails closed on four persisted
  corruption forms: JSON null, foreign row key, foreign payload correlation identity and payload /
  persisted-version mismatch. SQS now loses an expired maximum-renewal lock, disposes both owned
  clients even when one fails, preserves sole exception identity and deterministic aggregate order,
  and binds provider `SentTimestamp` to exact Unix-millisecond UTC semantics without a wall-clock
  test oracle.
- Frozen positive execution passes UnitArchitecture 2,043/2,043 and LocalIntegration 149/149 against
  one fresh PostgreSQL/Azurite/LocalStack fixture, everywhere with zero failure or skip. Unit,
  LocalIntegration and complete Engineering Release builds finish with zero warnings and errors.
  Focused counts are SQS 51 Unit + 48 LocalStack, DynamoDB 12 Unit + 9 LocalStack and S3 6 Unit +
  5 LocalStack.
- Exact one-occurrence mutations M15–M23 all build cleanly. Their nine owning MTP runs execute 26
  cases: 11 causal failures and 15 control passes, zero skip, with every mutation run exiting 2.
  All mutated source files restore byte-identically and the disposable mutation worktree is clean at
  the correction technical commit.
- The 111-row disposition is unchanged: 100 executing, nine real-AWS External, one invalid duplicate
  retired and one PO-superseded. Real AWS was not executed. The correction is not accepted until its
  separate evidence child is committed and remotely backed up, architecture binds both hashes, and
  two independent static read-only reviews return PASS.
- The local execution diagnosis is now durable in `.testagent/research.md`: restricted-sandbox
  NamedPipe/process boundaries and orphaned MSBuild nodes caused silent tooling stalls. The unchanged
  tests pass outside that boundary after targeted build-server shutdown with an isolated CLI home,
  disabled node reuse/shared compilation and the explicit MTP profile. No assertion, package or
  runner rule was weakened.
- The generated Apache-2.0 `CHANGELIST.md` matches its 8,309-entry candidate exactly, and all 103
  identity-tool self-tests pass. The separate full historical `identity_gate.py scan` is not claimed
  green: its persisted whole-fork mapping predates the many intentionally retired/moved legacy paths
  and currently reports 59,494 stale baseline/API bindings. That existing repository-governance debt
  is outside this 18-path AWS correction and requires a separately authorized regeneration/review;
  it is not hidden inside or counted as an AWS test result.

## ActiveMQ native closure — product-boundary checkpoint

- The hash-bound research checkpoint is `2afee3f3d734c405479d0d7bd00d9711573b1c2e`; the native
  test/configuration foundation is `b9728b00c54b06bd4958de777ee9eefd633a4642`. The selected R0
  closure contains 113 unique obligations and 179 historical execution identities. This checkpoint
  is not a terminal disposition or final evidence claim.
- ActiveMQ configuration now has no implicit broker or credentials, bus construction owns an
  immutable settings snapshot, failover endpoints are typed URIs, and address parsing rejects
  secrets, ambiguous options, protocol/host/port/virtual-host redirection and entity-name injection.
  Consumer topology identity includes the complete shared-subscription contract.
- Generic forwarding expiry and ActiveMQ scheduling use the owning context's `TimeProvider`; an
  already expired generic send is marked before transport serialization. Connection, session and
  temporary-entity cleanup preserve later cleanup attempts and retry ownership after an earlier
  failure. The obsolete product-shipped ActiveMQ test harness and dead connection hook are removed.
- The focused Release build has zero warnings and zero errors. The native xUnit 4 / Microsoft
  Testing Platform project passes 88/88 with zero failure and zero skip. The supported .NET 10 MTP
  command form passes runner options directly after the project options; inserting the legacy
  VSTest `--` separator causes a reproducible zero-discovery exit 5 and must not be treated as a
  product or sandbox failure. Restricted sandbox process/network failures remain diagnosed
  separately as documented above.
- After adding the ActiveMQ product and test projects to their exact profile graphs, the complete
  Release UnitArchitecture build passes with zero warnings and zero errors. The subsequent
  unfiltered, serial Microsoft Testing Platform run passes 2,150/2,150 with zero failure and zero
  skip using the repository's canonical command and minimum-test floor. The locked
  LocalIntegration restore and complete Release build also pass with zero warnings and zero errors;
  no broker-backed result is claimed by this checkpoint.
- Broker-backed replacement carriers, complete R0 disposition, inherited-project retirement,
  one-cause mutation evidence, full solution gates and independent read-only acceptance remain open.

## ActiveMQ native closure — hermetic configuration, body and recovery carriers

- Twenty-one previously frozen obligations now have three source-derived, executing xUnit 4 / MTP
  carriers: endpoint prefetch/concurrency precedence, provider message-body access-order semantics and
  the endpoint-bound recovery sequence. Every carrier uses external exact values or direct state
  snapshots; none waits for the absence of an event.
- The message-body work exposed a product defect instead of accommodating it: an ActiveMQ bytes
  message could return the correct body once and then re-read the provider cursor as zero bytes from
  another accessor. `ActiveMqMessageBody` now takes one thread-safe body snapshot and derives length,
  bytes, text and read-only streams from that single truth. Unsupported provider message kinds fail
  consistently from every accessor.
- The focused project build passes with zero warnings and errors and its full cohort passes 91/91,
  zero failure and zero skip. The complete UnitArchitecture Release build also passes with zero
  warnings and errors; the unfiltered serial solution run passes 2,153/2,153 with zero failure and
  zero skip.
- That first focused run also reproduced a pre-existing file-protocol race: both sides trusted a
  lossy `FileSystemWatcher` notification after only one filesystem check. The control client now
  derives completion from the atomically renamed result file under its injected `TimeProvider`, and
  its test controller observes the already-published request through a deterministic internal
  boundary. Ten consecutive focused runs of all 15 outage-protocol cases pass after the correction.
- This is a technical checkpoint, not final ActiveMQ acceptance. Broker-backed carriers, the full
  113-obligation closure, inherited-project retirement, mutation evidence and independent reviews
  remain open.

## ActiveMQ native closure — Classic/Artemis broker cohort

- Sixteen inherited broker obligations now have executing source-mirrored LocalIntegration carriers
  for connection/protocol selection, send/receive, handler invocation, request/response and publish.
  Both ActiveMQ Classic and Artemis use pinned, run-scoped fixtures with loopback-only endpoints and
  generated credentials; no external broker or fixed developer port is assumed.
- The provider run exposed three product defects, and the tests were not weakened around them.
  Artemis shared subscriptions now use a queue-less named shared topic consumer instead of creating
  an ANYCAST queue for MULTICAST traffic; topology probing handles that legitimate queue-less
  binding; and AMQP topic lookup reuses Artemis' broker-generated temporary topic while OpenWire
  preserves the canonical VirtualTopic address. The protocol distinction is explicit and covered by
  hermetic counterexamples.
- The complete Release UnitArchitecture run passes 2,156/2,156 and the complete Release
  LocalIntegration run passes 164/164 against fresh PostgreSQL, Azurite, LocalStack, ActiveMQ Classic
  and Artemis fixtures. Both have zero failure and zero skip; locked restores and Release builds
  finish with zero warnings and zero errors. The focused ActiveMQ unit cohort passes 94/94 and the
  new broker project passes 15/15.
- The complete Engineering build initially exposed one real stale contract in the ActiveMQ benchmark:
  `ActiveMqOptionSet` did not project the required `VirtualHost` setting. The benchmark now delegates
  that value to its concrete immutable host settings, and the repeated Engineering Release build
  passes with zero warnings and zero errors. The failed diagnostic command used a nonexistent
  `ViciOne.ServiceBus.Tests.Engineering.slnx`; the only canonical Engineering solution is
  `ViciOne.ServiceBus.Engineering.slnx`.
- Local .NET process rule: a sandboxed build can hang silently and ignore Ctrl-C at the restricted
  process boundary. After a bounded no-output interval, inspect and terminate only the exact process,
  then rerun the identical command outside the sandbox with the repository SDK and isolated CLI
  environment. Never start a duplicate concurrent build and never reinterpret this environment
  failure as a product or test failure.
- This remains a technical checkpoint. The remaining ActiveMQ obligations, complete terminal
  disposition, inherited-project retirement, mutation evidence and final independent acceptance are
  still open.

## ActiveMQ native closure — redelivery, endpoint configuration, error transport and outbox checkpoint

- The thirty inherited obligations `OBL-R0-BRK-0396` through `OBL-R0-BRK-0425` are mapped exactly
  once to executing native carriers. The genuinely overlapping two-message delayed-redelivery cases
  `0396` and `0401` share one stronger carrier across OpenWire, AMQP and Artemis; all other distinct
  behaviors retain a dedicated carrier. The mapping has 30 rows, 30 unique identifiers and no gap.
- Redelivery tests bind both the configured action count and the causal mechanism: exact redelivery
  sequences, exact retry delays observed through the retry contract, two broker scheduling signals,
  two explicit `Defer` callbacks and third-delivery completion. They use bounded operation waits and
  broker/handler state rather than wall-clock thresholds, sleeps or static mutable fixtures.
- Endpoint precedence is now checked both at the immutable settings boundary and through the public
  built-bus probe without opening a broker connection. Inherited bus values, direct overrides and
  endpoint-definition prefetch/concurrency combinations all have exact independent values.
- The inherited valid-envelope/nested-type-mismatch path and the raw no-envelope NMS path remain
  separate. A further unreadable-envelope case exposed a product defect: provider-owned
  `NMSMessageId` is replaced by ActiveMQ, so a body too damaged to expose envelope metadata lost the
  caller's service-bus identity. The send transport now also writes the logical
  `MessageHeaders.MessageId`; both Classic protocols preserve it in the resulting `ReceiveFault`.
  Receive-fault publication is exactly once, and the serialization-error route is verified against
  the real durable run-scoped broker queue after its acknowledgement barrier: enqueue 1, dequeue 1,
  queue size 0, with the complete original metadata retained.
- Message-scoped publish, message-scoped send and endpoint-scoped in-memory outbox configuration are
  exercised over OpenWire and AMQP. Failed retry/redelivery attempts release no output; the successful
  attempt releases exactly one output. Exact retry/redelivery coordinates and post-stop transport
  counts prevent a superficially green first-delivery assertion.
- The final focused ActiveMQ runs pass 94/94 UnitArchitecture and 46/46 LocalIntegration, with zero
  failure and zero skip. The complete Engineering Release build finishes with zero warnings and zero
  errors. The final unfiltered serial solution runs pass UnitArchitecture 2,156/2,156 and
  LocalIntegration 195/195 against one fresh run-scoped PostgreSQL/Azurite/LocalStack/ActiveMQ/Artemis
  fixture (`vicione-7c0775b2d141`), again with zero failure and zero skip.
- This remains an intermediate technical checkpoint. The remaining ActiveMQ obligations beginning at
  `0426`, final 113-obligation disposition, inherited-project retirement and complete mutation /
  acceptance evidence remain open; no terminal closure is claimed here.

## ActiveMQ native closure — terminal local implementation and evidence candidate

- The intermediate ActiveMQ sections above are historical checkpoints and are superseded by this
  section. Technical commit `fa938ae508bbc422bd281d82ae5d14ba995517d8` with tree
  `4f0e47743584c33e62035fe3bdc6f801aac28867` is the frozen local implementation subject.
- All 113 selected R0 obligations are terminally `REPLACED_EXECUTING`, accounting for all 179
  historical execution identities. The committed map has 40 UnitArchitecture and 73
  LocalIntegration owners and SHA-256
  `05e3a90bd09a8303eb5ca0ec68c06b1251655a348bc230af1bcbe5317dddc66a`.
- The inherited ActiveMQ project is atomically retired: 34 C# files plus four tracked project/config
  files, its expected-list and its legacy CI/model selection are absent. No empty inherited folder
  remains; Git retains the exact parent blobs.
- The frozen positive runs pass Engineering Release with 0 warnings and 0 errors,
  UnitArchitecture 2158/2158 and LocalIntegration 244/244, each with zero failure and zero skip.
  LocalIntegration used one fresh run-scoped PostgreSQL/Azurite/LocalStack/ActiveMQ/Artemis fixture
  with the ActiveMQ outage-control channel enabled.
- Sixteen byte-exact product mutations and one workflow-gate sabotage are each killed by their own
  focused carrier. Every compiled mutant builds cleanly, every targeted run exits 2 for the expected
  reason, M15 is broker-backed on both Classic and Artemis, and every product/workflow target is
  restored to its baseline SHA before this evidence child is assembled.
- Repository checks pass on the complete staged evidence set: 257/257 CI-tool tests, 103/103
  identity-tool tests, the verification model and the generated 8427-entry CHANGELIST. The known
  sandbox restriction was handled by repeating the process-group tests outside the sandbox; no test
  or product rule was weakened.
- This is the terminal local implementation/evidence candidate, not self-acceptance. Two independent
  static read-only reviews of the same technical/evidence freeze remain mandatory. GitHub workflows
  remain manually disabled and no cloud/external ActiveMQ follow-up is required.

## ActiveMQ native closure — A+ correction and final evidence candidate

- The immediately preceding ActiveMQ evidence candidate is historical and superseded by this
  section. Independent review found concrete protocol/options, failure-preservation,
  provider-scheduling, terminal-drain and wall-clock-test gaps. They were fixed in product/tests,
  never hidden by weakening an assertion or reclassifying an executing obligation.
- Frozen technical commit `c2a0cfeda3466029eddd75033fe2220ffbb9a612`, tree
  `9b9d032a8bd80ee02f9376ab3fdf443cdfbadeeb`, parent
  `3057025b5c02c1ce034a9633baf87cb24a24ba92`; the complete correction from `72e788b2` contains
  exactly 40 paths. Architecture order `b325ebd` binds this technical identity and the full scope.
- ActiveMQ options now require typed OpenWire/AMQP protocol plus an explicit non-zero port whenever
  options configure a host. Cleanup attempts every connection/session/auto-delete stage and
  preserves sole failure identity or deterministic aggregate order. AMQP keeps broker-owned
  auto-delete; OpenWire retains the supported manual deletion path.
- Provider scheduling is proven through broker state, exact delivery and post-delivery empty state:
  Artemis uses `NMSDeliveryTime`; ActiveMQ Classic uses `AMQ_SCHEDULED_DELAY` over OpenWire and AMQP.
  Scheduling, Quartz and shared-subscription carriers stop/drain before exact terminal counts and
  bind broker enqueue/dequeue/rest statistics. ActiveMQ LocalIntegration contains no wall-clock wait;
  the Roslyn gate scans real invocation syntax.
- Positive frozen runs: Engineering restore/build exit 0 with 0 warnings and 0 errors; ActiveMQ Unit
  122/122; ActiveMQ LocalIntegration 95/95; unfiltered UnitArchitecture 2185/2185 across 18 CTRFs;
  unfiltered LocalIntegration 244/244 across seven CTRFs and fresh PostgreSQL/Azurite/LocalStack/
  ActiveMQ/Artemis fixture `vicione-19a2b33ee70b`. Every run has 0 failed and 0 skipped.
- Twelve byte-exact attacks M18-M29 cover protocol/port, cleanup propagation and identity,
  auto-delete continuation/provider ownership, Artemis and Classic provider scheduling, terminal
  duplicate delivery and the no-wall-clock source gate. Every compiled mutant builds 0/0, every
  behavioral/gate run exits 2 for its own cause, all targets restore to the technical SHA and the
  isolated mutation worktree ends clean.
- The terminal R0 closure remains exactly 113 obligations / 179 historical identities with 40 Unit
  and 73 LocalIntegration owners. No cloud ActiveMQ boundary is deferred. This section is the final
  evidence candidate, not self-acceptance; independent read-only review remains mandatory before
  acceptance and remote publication.

## ActiveMQ native closure — command-binding precision correction

- The technical subject and every raw result remain unchanged. A post-freeze evidence audit found
  that the mutation execution table omitted its build/test working directories, represented several
  canonical evidence destinations as relative paths from the detached mutation checkout, and did
  not reproduce the supplied `--evidence-dir` argument for M26-M28.
- `MUTATION_EXECUTION.tsv` now binds both working directories and shell-escaped argument sequences.
  It distinguishes the detached technical checkout from the canonical fixture-runner checkout and
  names the canonical raw-result destinations unambiguously. No test, build, broker or mutation was
  rerun, and no result or technical claim changed.
- This additive evidence correction supersedes only the earlier command-string rendering. The
  independent acceptance review must bind the final correction child, not the preceding evidence
  candidate.

## ActiveMQ native closure — explicit public protocol and benchmark finalization

- The preceding ActiveMQ technical/evidence candidates remain immutable historical checkpoints and
  are superseded by this section for public host API, benchmark configuration, hostile URI inputs,
  non-positive provider timing and the wall-clock source gate.
- Frozen technical commit `3d191610111d30df4bf3e724a614306e16eb3049`, tree
  `e80eefbed5698f29d9c652fff931549ffdc26634`, parent
  `c9b0cd2cd0cdec1018ec3985f2a6b2d423e575a9`; the complete authorized correction from
  `7c140863d042c0b7c8ce04057c6e98c0022222fe` contains exactly 21 paths.
- Every public host entry point requires typed OpenWire/AMQP protocol and an explicit port. Tests
  prove both API shape and the actual configured scheme/host/port for both protocols. The raw
  string-scheme constructor is internal and no protocol-less Host overload remains.
- The benchmark no longer defaults to localhost/OpenWire/61616. Host, named protocol and port are
  mandatory, provider selection is exact, numeric enum text is rejected, repeated parsing cannot
  retain coordinates, and credentials/TLS/canonical logical addressing are proven.
- Hostile credential/query/fragment URIs carry valid explicit ports, so their own rejection remains
  causal. Classic and Artemis both remove absent/non-positive scheduling state. The syntax gate
  rejects direct, aliased, fully qualified and using-static Delay/Sleep calls in the ActiveMQ test
  tree.
- Final positive runs at this exact technical commit: locked Engineering restore/build exit 0 with
  0 warnings and 0 errors; UnitArchitecture 2215/2215 across 19 CTRFs; LocalIntegration 244/244
  across seven CTRFs and fresh five-provider fixture `vicione-62668da77f43`; every profile has zero
  failure and zero skip. The modules include ActiveMQ Unit 130/130, Benchmark 16/16, Architecture
  124/124 and ActiveMQ LocalIntegration 95/95.
- Eleven byte-exact attacks M30-M40 (ten product/API mutations and one test-gate sabotage) build
  cleanly and each fail only at the designated carrier with Exit 2 and zero skips. Exact unified
  patches, baseline/mutant/restore hashes, argv, cwd, logs, binlogs and CTRFs are bound under
  `ACTIVEMQ-NATIVE-CLOSURE-CORRECTION-02`.
- The 113-obligation / 179-historical-identity closure remains exactly 40 UnitArchitecture plus 73
  LocalIntegration owners, all `REPLACED_EXECUTING`; the retired legacy test project remains absent.
  GitHub workflows remain globally manual/disabled as authorized. This is the final local evidence
  candidate, not self-acceptance; two independent read-only reviews of this exact technical and
  evidence freeze remain mandatory.

## ActiveMQ native closure — final compiler-input and API-boundary closure

- The preceding ActiveMQ technical/evidence candidates are immutable historical checkpoints and
  are superseded by this section. The final technical subject is commit
  `d3afdfa386909317f20894e47a6137601b215f68`, tree
  `4249d72bfb541c27d8d5b14dda55b0a898383ab3`, parent
  `f5dd3c5e584c1a25a190f70c3b73605fd4aabc42`.
- The public host boundary now rejects unknown typed protocols, relative URIs, invalid typed hosts
  and ports, URI-invalid host characters and the unavoidable default-struct state at construction
  or projection. The Benchmark option parser requires all connection coordinates on every parse,
  clears credentials/TLS/effective settings before reparsing, and remains an internal sealed
  implementation detail.
- The Unit solution contains the complete Release-evaluated Benchmark product closure. The ActiveMQ
  no-wall-clock guard evaluates the real Release `Compile` set in one Roslyn compilation with SDK
  global usings, aliases/static imports, target-framework defines and language version; it also
  fails closed if that real source set is empty before the synthetic using tree is added.
- Final positive execution at the exact technical commit is Engineering locked restore/build Exit 0
  with 0 warnings and 0 errors; UnitArchitecture 2228/2228 across 19 CTRFs; LocalIntegration 244/244
  across seven CTRFs and fresh five-provider fixture `vicione-751339deadb8`; all runs have 0 failed
  and 0 skipped. Included modules are ActiveMQ Unit 133/133, Benchmark 21/21, Architecture 129/129
  and ActiveMQ LocalIntegration 95/95. CI tools pass 257/257, Identity tools 103/103, and the
  verification model exits 0.
- Thirty-three byte-exact attacks M30-M62 were reconstructed and executed against this final tree.
  Every mutant builds with Exit 0, every designated focused MTP run exits 2 for its own assertion
  with zero skips, every target restores to its exact frozen SHA and the detached mutation worktree
  ends clean. M61 specifically empties only the evaluated `Compile` projection and is killed by the
  new non-empty-source assertion.
- Raw commands, patches, metadata, logs, binlogs, CTRFs, broker state, controlled outage/recovery
  results and hashes are staged under
  `ACTIVEMQ-NATIVE-CLOSURE-CORRECTION-03`. The inherited closure remains 113 R0 obligations / 179
  historical execution identities with 40 UnitArchitecture and 73 LocalIntegration owners, all
  `REPLACED_EXECUTING`. This is a final local evidence candidate, not self-acceptance; two independent
  read-only reviews of the exact technical/evidence freeze remain mandatory before remote push.

## Transactional bus capability split — technical candidate

- The inherited `ITransactionalBus` shape is replaced by the truthful public capabilities
  `IAmbientTransactionBus` and `IBufferedBus`. The ambient bus follows `Transaction.Current` and
  owns no manual flush member; the buffered bus exposes explicit FIFO `FlushAsync`. All concrete
  adapters are internal, and the retired public interface, concrete names, registration methods and
  intentionally throwing ambient release surface are absent without a compatibility shim.
- Explicit buffering preserves every publish/send overload behind one serialized snapshot drain.
  It proves FIFO, empty/repeated flush, concurrent flush, work added during a drain, exact failure
  and cancellation identity, no replay of an attempted action and retention of only an unattempted
  tail. Ambient dispatch proves immediate no-transaction behavior, transaction-owned lifetime after
  enlistment, exact commit/rollback/in-doubt cleanup, isolated concurrent transactions and original
  transport failure causality. The volatile prepare bridge is documented as best-effort, never as a
  durable or crash-atomic outbox.
- Core scoped consumers retain both publish and send boundaries for default and generic multi-bus
  registrations, including a secondary buffered bus resolved inside a default-bus consumer scope.
  The durable Entity Framework bus outbox and both lightweight capabilities fail fast in either
  registration order because each owns the same scoped publish/send provider. Idempotent same-owner
  registration and independent multi-bus owners remain valid.
- The eleven inherited R0 obligations `OBL-R0-CORE-C-0430` through `0440` are uniquely mapped to
  stronger native carriers and the three inherited NUnit files are removed. The source-owned native
  cohort contains 35 Transaction facts; three additional PostgreSQL-backed EF facts cover rollback,
  commit and explicit flush, and one four-case EF Theory covers both conflicting capability pairs in
  both registration orders. The Unit floor is therefore exactly 2267; LocalIntegration remains 244.
- Final local execution on the technical candidate: UnitArchitecture 2267/2267 and LocalIntegration
  244/244, both with zero failure and zero skip. The latter used fresh run-scoped PostgreSQL,
  Azurite, LocalStack, ActiveMQ Classic and Artemis resources under `vicione-53b27152e147`.
  LocalIntegration and complete Engineering Release builds both finish with zero warnings and zero
  errors; the Engineering restore succeeds in locked mode.
- Compilation diagnosis is reproducible and belongs to the execution environment, not the source:
  inside the managed sandbox the same Release builds stall silently, while three identical focused
  builds outside it complete in roughly 24 seconds and the final complete builds succeed. The
  sandbox also rejects the .NET 10 MTP solution orchestrator's local named-pipe bind with
  `SocketException (13): Permission denied` before discovery. The exact same `dotnet test` argv
  outside the sandbox passes 2267/2267. The stable recipe is the isolated CLI environment rooted at
  `/private/tmp/vicione-dotnet-transactional`, build/test server reuse disabled, compilation and MTP
  orchestration outside the sandbox, and no change to tests, floors or product behavior.
- The completed transactional-bus debt is removed from `TODO.md` and the breaking Greenfield API is
  recorded in `CHANGELOG.md`. GitHub workflows remain manually disabled as authorized. Byte-exact
  one-cause mutations, technical/evidence freezes, independent read-only reviews, final architecture
  binding and remote fast-forward remain open; this section is not self-acceptance.

## Transactional bus capability split — frozen technical and evidence candidate

- The technical subject is frozen at commit `1e8d0b4c6ad2e4b10cf90f8cfef9c3d16fc70407`, tree
  `66344039fd960905a1e1e69f7cd7dd883c503289`, parent
  `afb8edd119e83834c6838ed151eec3a345ef7d74`. Its complete parent delta contains 38 paths and is
  bound as `TRANSACTIONAL-BUS-CAPABILITY-SPLIT/TECHNICAL_DIFF.patch.gz`.
- Positive execution was repeated at those exact bytes and is now raw/hash-bindable: locked
  Engineering restore Exit 0; Engineering Release build Exit 0 with 0 warnings and 0 errors;
  UnitArchitecture 2267/2267 across 19 CTRFs; the source-owned transaction namespace 35/35; the
  four EF ownership-order cases 4/4; and LocalIntegration 244/244 across seven CTRFs. Every test
  profile reports zero failure and zero skip.
- The LocalIntegration proof uses fresh run-scoped PostgreSQL, Azurite, LocalStack, ActiveMQ Classic
  and Artemis resources under `vicione-68ff634d8cda`. Endpoint projection, five broker logs, two
  ActiveMQ outage/restore cycles and an empty fixture-findings receipt are copied into the evidence
  package; the run-root ownership token and all generated credentials are deliberately excluded.
- Seventeen buildable one-cause mutations M01-M17 cover public API separation, truthful DI lifetime,
  consume-scope routing, publish double-buffering, publish/send overload parity, FIFO, pre-flush
  isolation, exact failure identity, tail retention, cancellation ownership, rollback disposal,
  transaction-state cleanup, EF outbox ownership, null guards and exact-once dispatch. All 17 Release
  builds pass, every focused MTP owner exits red with zero skips, and every target restores to its
  exact technical SHA-256. M15 intentionally leaves two independent outbox-first controls green while
  killing the two capability-first cases whose guard was removed.
- `MUTATION_MANIFEST.json` contains byte-exact old/new recipes, occurrence counts and indices,
  baseline/mutant/restore hashes, project/owner/command construction and raw result paths. The
  package's `verify_evidence.py` independently rebuilds every mutant from `git show`, verifies all
  positive/negative CTRFs, broker-log digests and the exact checksum inventory.
- The earlier sandbox diagnosis is confirmed rather than worked around: MTP solution orchestration
  fails before discovery when the sandbox denies its named-pipe bind, while byte-identical commands
  in the isolated CLI environment pass. No product path, assertion or floor was changed for the
  execution environment.
- This is the immutable local evidence candidate. Its evidence commit, generated CHANGELIST, final
  architecture binding, remote fast-forward and two separate read-only acceptance reviews remain
  mandatory; this section still does not self-accept the slice.

## Transactional bus capability split — review correction and final evidence candidate

- The two preceding transactional-bus sections are immutable historical checkpoints and are
  superseded by this section. Independent review found three concrete A+ gaps: recursive same-instance
  flush deadlocked, generic MultiBus conflict registration was not proven in both orders, and the
  implementation-type visibility oracle covered only three of nine concrete transaction adapters.
- Frozen technical correction commit `3c1bfbafeb326047a86a2ec6573f6f516e2bd230`, tree
  `6a0eb15d26288f269feb73ee35dc5ff44d654970`, parent
  `477c600e84104f25936761dbb67f85fa42421c41`; architecture correction scope
  `17a0791b563a99e81c3f589a5d63214e59330d99`, tree
  `151861503f841e55e2dfd1f4a93dc3c95e879161`, authorizes the additional reflected
  generic-registration product boundary.
- Recursive `FlushAsync` from an action drained by the same `BufferedBus` now fails before waiting on
  its own semaphore. A mutable operation-local frame prevents inherited child execution contexts from
  remaining poisoned after the action ends; external concurrent flushes retain their serialized FIFO
  behavior and the unattempted tail remains usable.
- Generic Ambient+Buffered registrations on the same typed bus fail in either order. The reflected
  generic registration boundary preserves and rethrows the original `ConfigurationException` instead
  of exposing `TargetInvocationException`. The public-surface carrier closes the exact nine-type
  internal implementation set and each type's sealed/abstract shape.
- Three new requirement variants expand to four xUnit cases because the typed-bus order carrier is a
  two-row Theory. The fail-closed UnitArchitecture floor is consistently 2271 in workflow,
  architecture gate, README, build guide and active plan; LocalIntegration remains 244.
- Positive execution at the exact technical bytes: locked Engineering restore/build Exit 0 with
  0 warnings and 0 errors; UnitArchitecture 2271/2271 across 19 CTRFs; focused transaction namespace
  39/39; LocalIntegration 244/244 across seven CTRFs. Every profile has zero failed and zero skipped.
  LocalIntegration used fresh PostgreSQL/Azurite/LocalStack/ActiveMQ/Artemis resources under
  `vicione-ff320fbbc2ff`, including two controlled ActiveMQ outage/restore cycles.
- Six new byte-exact attacks M18-M23 are independently bound. They cover recursive deadlock, stale
  inherited reentrancy state, both typed-bus registration directions, concrete-adapter publicization
  and reflection-wrapper leakage. Every Release mutant builds 0/0, each designated MTP owner exits 2
  for its own causal assertion with zero skips, and every target restores exactly. Together with the
  immutable parent evidence, cumulative mutation closure is 23/23.
- Raw commands, unified patches, baseline/mutant/restore hashes, build logs/binlogs, CTRFs, positive
  fixture outputs and an executable verifier are staged under
  `TRANSACTIONAL-BUS-CAPABILITY-SPLIT/CORRECTION-01`. The sandbox diagnosis remains environmental:
  byte-identical .NET/MTP commands pass outside the sandbox; no product/test adaptation was made.
- This is the final local evidence candidate, not self-acceptance. Two independent read-only reviews
  of the exact technical/evidence freeze, remote fast-forward and final architecture acceptance remain
  mandatory.

## Transactional bus capability split — stack-provenance correction candidate

- The preceding correction remains immutable. Its product/API review passed without BLOCKER, MAJOR or
  MINOR. The separate test/evidence review found one MINOR: the reflection carrier proved original
  exception type and object identity, but a direct `throw exception.InnerException` could still erase
  the callback stack while remaining green.
- The additive technical correction is frozen at `f1f64a385a8e1f0f3bf5d647be6cbeff8bdd532e`, tree
  `a49611eb84e1e680b04c63f0641f6d42574e7563`, parent and preceding evidence
  `8b02142d50a7e085c60aac40a1d53bcef4ad70cb`. Its exact delta is four assertion lines in the existing
  `TypedBusReflectionBoundary_PreservesTheOriginalConfigurationFailure` Fact; product/API bytes,
  requirements and floors are unchanged.
- The carrier now proves the original `ThrowingBusInstanceCallback.GetResult` frame in addition to
  exact `ConfigurationException` identity. M24 changes only the EDI rethrow to a direct inner-exception
  throw: the Release build remains 0 warnings/0 errors, while the one-test MTP run exits 2 precisely
  because that callback frame is absent. The product target restores to its exact technical SHA-256.
- Full positive execution was repeated at the new technical bytes: locked Engineering restore and
  Engineering Release build exit 0 with 0 warnings/0 errors; UnitArchitecture 2271/2271 across 19 CTRFs;
  focused transaction namespace 39/39; LocalIntegration 244/244 across seven CTRFs; zero failures and
  zero skips throughout.
- LocalIntegration used fresh run-scoped PostgreSQL, Azurite, LocalStack, ActiveMQ Classic and Artemis
  resources under `vicione-0fda85f6124b`, with dynamic loopback endpoints, five broker logs, two
  controlled ActiveMQ outage/restore cycles and empty fixture findings. The ownership token and
  generated credentials are excluded.
- Exact commands, working directories, positive results, M24 patch/baseline/mutant/restore hashes,
  binlogs, raw logs, CTRF and an executable verifier are collected under
  `TRANSACTIONAL-BUS-CAPABILITY-SPLIT/CORRECTION-02`. Cumulative mutation closure is 24/24.
- This remains a local candidate rather than self-acceptance. A fresh independent targeted re-review,
  the Evidence commit, remote fast-forward and final architecture acceptance remain mandatory.

## Transactional bus capability split — frozen stack-provenance evidence status

- The preceding candidate sentence is a pre-commit checkpoint and is superseded by this append-only
  status correction. The stack-provenance evidence child is frozen locally at commit
  `f644adc021ec4a308401b8eb18b312cea6387ca8`, tree
  `67139e9e19e7db3e3c404eaf02e34be55463447c`, with direct parent technical commit
  `f1f64a385a8e1f0f3bf5d647be6cbeff8bdd532e`.
- Product/API review of this exact correction is PASS without BLOCKER, MAJOR or MINOR. The separate
  test/evidence review confirms the technical change, M24, all positive executions, 60/60 checksums,
  fixture scope and secret scope as PASS; its only finding was the now-superseded pre-commit status
  wording.
- Remaining acceptance is limited to a targeted read-only verification of this status-only child,
  remote fast-forward and final architecture acceptance. This section does not self-accept the slice.
## AWS native closure — independent review correction started

- Independent product/API and test/evidence reviews of `d0043bae` plus `9195e5fc` agree that the
  stored execution evidence is internally consistent but incomplete for five source-derived product
  boundaries: typed DynamoDB multi-saga factory ownership, immutable/secret-free low-level SQS host
  settings, atomic SQS/SNS pair construction, per-host immutable client-cache options and lock loss
  after failed `Complete`. The normal connection-disposal EDI stack is also strengthened in the same
  ownership correction.
- Architecture commit `c7a8b28` authorizes exactly this follow-up. Work proceeds on isolated branch
  `feature/aws-native-review-correction` from remotely backed-up commit `2afee3f3`; GitHub workflows
  stay disabled, real AWS remains External and no cloud resource or credential is used.
- This is a pre-implementation checkpoint, not an acceptance claim. Product/test changes, local
  execution, mutation evidence, separate technical/evidence freezes, two independent PASS reviews,
  remote backup and final architecture acceptance remain mandatory.

## AWS native closure — review-correction technical candidate

- All five independently reproduced product boundaries are corrected in the isolated worktree.
  DynamoDB owns one immutable context factory per saga type; the SQS low-level API accepts only a
  sealed snapshot from the typed builder; user info/query/fragment are rejected centrally; queue and
  topic caches receive immutable per-host capacity/max-age/clock options; SQS/SNS partial creation
  preserves primary/cleanup causality; failed or cancelled completion loses the receive lock.
- The non-packable benchmark no longer implements arbitrary host settings and instead consumes the
  same sealed snapshot. Architecture commit `ad85105` binds this necessary two-path consumer scope.
- Focused Release results are SQS 61/61 and DynamoDB 14/14, both with zero failure/skip. The complete
  UnitArchitecture profile is 2055/2055 and the local run-scoped PostgreSQL/Azurite/LocalStack
  profile is 149/149, everywhere with zero failure/skip. Unit, LocalIntegration, Benchmark and full
  Engineering Release builds finish with zero warnings/errors.
- The first fresh-worktree Unit run exposed missing generated MTP props for six not-yet-restored
  LocalIntegration projects. After the required locked LocalIntegration restore, the unchanged
  architecture test and complete Unit profile passed. `.testagent/research.md` retains this distinct
  build-state diagnosis; no test or product rule was weakened.
- This remains a technical candidate, not acceptance. Exact one-cause mutations, generated
  CHANGELIST, a separate evidence child, remote backup, two independent read-only PASS reviews and
  final architecture acceptance remain open. Real AWS and GitHub Actions remain untouched.

## AWS native closure — review-correction evidence candidate

- Technical Commit `1d6016f7`, Tree `62a2bbb1`, is frozen and remotely backed up. Fresh positive
  execution against exactly that commit is UnitArchitecture 2,055/2,055 and LocalIntegration
  149/149, with zero failure/skip. Unit, LocalIntegration and Engineering Release builds each finish
  with zero warnings/errors; the local broker runner uses only PostgreSQL, Azurite and LocalStack on
  dynamically allocated loopback ports.
- Eleven exact one-cause mutants M24-M34 cover the typed DynamoDB owner, URI security, sealed/frozen
  host snapshot, partial client cleanup/stack/aggregate order, queue/topic cache clock propagation,
  failed-completion lock loss and fully built connection-disposal stack. All build and every owning
  MTP invocation is causally red: 19 failed cases, seven green controls, zero skips. The disposable
  mutation worktree is byte-clean at the Technical Commit after restoration.
- A second diagnostic full LocalIntegration repeat left every AWS module green but observed the
  inherited EF-only `InboxOutboxConcurrencyTests.ConcurrentRedeliveries_EnterTheConsumerOnceAndCommitOneEffectSet`
  once at counter 2 instead of 3. The unchanged carrier passed immediately in isolation against a
  fresh PostgreSQL fixture. This is transparent non-AWS follow-up evidence, not an AWS acceptance
  count and not authorization to change the EF slice here.
- The Evidence Child is not frozen or accepted yet. Generated CHANGELIST/SHA closure, two independent
  read-only PASS reviews, remote backup and final architecture acceptance remain mandatory. Real AWS,
  credentials and GitHub Actions remain untouched.

## AWS native closure — final host-input correction candidate

- The first product review of Technical `1d6016f7` and Evidence `7aa0ec82` returned PASS. The second
  review confirmed the DynamoDB owner closure but found one remaining product-input gap: the central
  address accepted an absolute hostless `amazonsqs:/scope` URI. It also found that M32 used the
  correct occurrence index and mutant bytes but incorrectly described a repeated exchange line as a
  one-occurrence literal.
- The central address now rejects a missing URI host. Two direct public theories bind hostless and
  relative URI inputs through both `AmazonSqsHostAddress` and `AmazonSqsHostConfigurator`, plus
  null/empty/whitespace string-host inputs through the public string constructor. All five focused
  cases pass after a zero-warning, zero-error Release build; the UnitArchitecture floor is 2,060 and
  the focused SQS count is 66.
- This remains a correction candidate. A new Technical freeze, full positive verification, three
  separate host-input mutants, corrected unique M32 recipe, regenerated Evidence child, two new
  independent read-only PASS reviews, remote backup and architecture acceptance remain mandatory.
  Real AWS, credentials and GitHub Actions remain untouched.

## AWS native closure — final host-input freeze

- Technical `fba681ade996b48877497d71165426e1e92d4b0f`, Tree
  `f5a7f4d2851595b605c9a6649b2945ae986d3267`, is frozen as the direct child of the preceding
  reviewed Evidence. Its direct Evidence child is `2335df2ba241fbe44ac132d12467bcfba1e3a09e`,
  Tree `7932fd9477731c04f6ab0f064eb889602d24e1a5`.
- Fresh execution at the exact Technical freeze is UnitArchitecture 2,060/2,060 and
  LocalIntegration 149/149, both with zero failure/skip. Unit, LocalIntegration and Engineering
  locked restores and Release builds complete with zero warnings/errors. The SQS module is 66/66.
- M35-M37 independently kill hostless URI, relative URI and null/empty/whitespace string-host
  regressions. The re-bound M32 uses one unique full `Complete` method literal and kills both
  failed/cancelled settlement axes. Four mutation builds pass; seven causal cases fail, two controls
  pass and none skip. All targets restore byte-identically.
- The Evidence directory contains 47 files with 46/46 nonmanifest hashes; CHANGELIST matches 8,424
  generated entries. The only remaining local acceptance work is two independent read-only PASS
  reviews, remote fast-forward and final architecture acceptance. Real AWS, credentials and GitHub
  Actions remain untouched.

## AWS native closure — independent final review result

- Independent product/API and test/evidence red-team reviews both return PASS for Technical
  `fba681ade996b48877497d71165426e1e92d4b0f`, Evidence
  `2335df2ba241fbe44ac132d12467bcfba1e3a09e` and the status freeze
  `ea49fef68d74fdfcf69ab64bb97c864be639e6a4`; neither review reports a BLOCKER, MAJOR or MINOR.
- Both reviews independently confirm the five public invalid-host cases, the 2,060/149/66 positive
  counts, the corrected unique M32 recipe, M35-M37 causality, byte-identical restoration and the
  complete 47-file/46-hash Evidence closure.
- A remote fast-forward was attempted only after both PASS results. The managed security gate
  rejected repository-content egress because the exact payload and destination require explicit PO
  approval. No workaround was attempted; the local branch remains four commits ahead of its remote.
- The technical and evidence review is complete locally. Remote backup and final architecture
  acceptance remain open; real AWS, credentials, costs and GitHub Actions remain untouched.

## AWS native closure — remote backup completed

- Following explicit PO approval, `origin/feature/aws-native-review-correction` was fast-forwarded
  from `1d6016f78b2f903c691d12f6e8d4efa6929ba227` through the independently reviewed completion
  status `6a8e0510c79fc24b74023ffe543a895120b8b904`.
- This additive status-only child records the completed ServiceBus backup. After its own fast-forward,
  only the final architecture acceptance and architecture-branch backup remain.
- No real AWS resource, credential, cost-bearing operation or GitHub Actions run was introduced.

## ServiceBus native closures — canonical branch integration freeze

- PO assignment `PO-2026-08-29-01`, bound by architecture commit `ac424ac`, authorizes the
  reintegration of the independently accepted Transactional/ActiveMQ and AWS lines in the persistent
  checkout `repositories/vicione-servicebus`. The integration merge is frozen at
  `8dcd3bcecc07222f89f70b3b04268d01bc5642b7`, tree
  `29c8aa84fbfb268943702c07e7f2c59f04ecb178`, with exact parents
  `a5f39ef947a548472958c09a1a67a2a155a2d213` and
  `bd5164b9d583fce58c566b302af45c27803f364c` and common base
  `2afee3f3d734c405479d0d7bd00d9711573b1c2e`.
- The eight overlapping files were resolved as an additive union. The fail-closed floors are 2,288
  UnitArchitecture and 244 LocalIntegration; all four GitHub workflow files remain manually
  disabled. No real cloud resource, credential or cost-bearing operation was used.
- Local static gates at the exact merge tree pass: verification model Exit 0, identity self-tests
  103/103, CI tooling self-tests 257/257 and generated CHANGELIST 9,291 entries. Locked Unit,
  LocalIntegration and Engineering restores pass; the Engineering Release build completes with
  zero warnings and zero errors. UnitArchitecture passes 2,288/2,288 with zero failures and skips.
- The first complete fresh-fixture LocalIntegration run produced 243/244: only the AMQP row of
  `ActiveMqRequestResponseTests.ConcurrentRequests_ReturnExactResponsesAcrossConfigurationPaths`
  reached its unchanged operation timeout. The test and its complete ActiveMQ product path are
  byte-identical to the accepted first parent, and the fixture reported no finding. No timeout,
  assertion or product rule was changed.
- A diagnostic repeat of the complete ActiveMQ module against fresh Classic/Artemis fixtures passed
  95/95. A subsequent unchanged full fresh-fixture run `vicione-621ce4125503` passed 244/244 across
  PostgreSQL, Azurite, LocalStack, ActiveMQ Classic and Artemis, with zero failures and skips. The
  original isolated timeout is therefore retained as a transparent non-reproduced fixture/runtime
  event rather than hidden or converted into a product change.
- This integration is locally complete, not self-accepted architecture. The remaining authorized
  work is the status/CHANGELIST child freeze, remote backup of the canonical integration branch and
  final architecture acceptance. The legacy parallel branch names remain only as immutable history;
  the persistent checkout is the canonical working location.

## ServiceBus native closures — canonical remote backup completed

- Following the PO's explicit branch-and-payload approval, the canonical integration status child
  `6c16b1878c143950fb7b7c06675ed5f183d26663` was pushed without force to
  `origin/integration/servicebus-native-closures`. A read-only remote lookup returned that exact
  commit; the local persistent checkout and its upstream therefore matched byte-for-byte.
- This additive status child only records the completed backup. Product, test, requirement,
  workflow, Evidence and CHANGELIST bytes are unchanged; GitHub Actions remain manually disabled.
  The remaining governance step is the corresponding final architecture-status update and remote
  verification.
## 2026-08-29 — F-REP-02 local technical freeze and candidate Evidence

- The source-derived identity/provenance tooling is frozen locally at Technical commit
  `1090e11ecc250441e2ecf0357a8e114b84b94001`, tree
  `02053909b2340685137b46deccc8504e6650adad`, with direct parent
  `1c52dd2d4d5942f087a725a6256d6b6f41b0ba4c`.
- The full candidate scan against separately generated Evidence reports `PASS`, zero findings,
  5,654 terminal baseline records, 5,650 changed-or-retired records and 35,055 public declaration
  records. The terminal partition is 20 `MAPPED_EXISTING`, 4,186 `MOVED_EXACT` and 1,448
  `RETIRED_DELETED`; the public projection is 23,301 present, 710 modified-or-removed and 11,044
  retired-path records.
- The historical policy is limited to 53 exact paths, 5,772 counted UTF-8 identity contexts and
  seven full binary blobs. Active product source has no general exception. Five generated contract
  files are independently bound by `GENERATED_SHA256SUMS`, currently 5/5 verified.
- Focused hostile Identity tests are 55/55 green; the complete Identity self-test suite is 120/120
  green after deterministic CHANGELIST regeneration to 9,304 entries. Three injected one-cause counter-mutations for
  context-inventory bypass, binary-digest bypass and ignored external Evidence root each made its
  dedicated test red and were immediately restored.
- No .NET, MSBuild, container or network process was required. The Python syntax check initially hit
  the macOS cache sandbox; setting `PYTHONPYCACHEPREFIX=/private/tmp/frep02-pycache` is the verified
  unchanged-command remedy.
- The default in-repository scan now also reports `PASS` with zero findings. This is not final
  acceptance: Evidence commit/tree binding and two independent read-only reviews remain mandatory.
  No F-REP-02 commit has been pushed.

## 2026-08-29 — F-REP-02 retirement-tree correction

- Post-commit reconstruction found that the first candidate Evidence child made all 1,448 retired
  mappings stale because `retirementTree` had been derived from the moving branch `HEAD`. Evidence
  commit `46d5747f25a1ccb3a6cd0cc8777333597cf7ec21` and its validation are therefore explicitly
  superseded and are not an acceptance result.
- Correction Technical commit `65f0d9b9741fab60cab39cef4fa7357e7ef1c2cc`, tree
  `b5bfe36a69525a2991f1284954a89d567139aaa0`, resolves each retirement tree from its immutable
  full deletion commit instead. The 1,448 retired paths bind 69 deletion commits and 69 trees.
- The independent constructor test rejects symbolic `HEAD`, malformed and uppercase commit IDs and
  verifies the exact `<deletion-commit>^{tree}` Git query. The M04 counter-mutation restoring
  `HEAD^{tree}` is causally red and the source file was restored byte-for-byte.
- Candidate correction Evidence again reports `PASS`, zero findings, 5,654 terminal baseline paths
  and 35,055 public declarations. Focused Identity tests are 57/57 and the complete Identity suite is
  122/122 green; CHANGELIST is exact at 9,312 entries. A direct correction Evidence child, then a
  post-commit reconstruction and independent reviews remain mandatory. Nothing is pushed.

## 2026-08-29 — F-REP-02 post-Evidence audit

- Correction Evidence commit `3ed6a617ffbcfe9a04b9029e383a58be0b37c1ec`, tree
  `985f466056650ae00d1b2ad2a62841a48a5c00e3`, is the direct child of Technical
  `65f0d9b9741fab60cab39cef4fa7357e7ef1c2cc`.
- A full scan executed after that commit is `PASS` with zero findings, 5,654 baseline paths, 5,650
  changed-or-retired paths and 35,055 public declaration records. Its raw bytes are identical to
  `SOURCE_IDENTITY_GATE.json` at SHA-256
  `29f82fe838ec9c43936786c55e2f03eed6de507b0a105a1e13a15d1b8c71044f`.
- Independent post-commit mapping reconstruction reports zero generated findings and zero persisted
  drift, retaining 1,448 retired records across 69 deletion commits and 69 deletion trees.
- The remaining gates are the audit-child hash/CHANGELIST closure and two independent read-only
  reviews. F-REP-02 is still local only and has not been pushed.
- The audit-child CHANGELIST is now exact at 9,316 entries. Its four post-commit logs are present;
  only final manifest binding, commit/tree freeze and the two independent reviews remain.

## 2026-08-29 — F-REP-02 final mutation-closure correction

- Independent Test/Evidence review found one Evidence-only MAJOR: M01-M03 were still byte-bound to
  superseded Technical `1090e11ecc250441e2ecf0357a8e114b84b94001`, while final Technical
  `65f0d9b9741fab60cab39cef4fa7357e7ef1c2cc` changed the same target file for stable retirement
  provenance. The old red runs remain historical but are not the final mutation closure.
- M01-M03 were repeated against the final `identity_gate.py` baseline SHA-256
  `f2f1614155110bb642eeb10c763189522bf7ed942387b0c1d3aa3644c7646338`. Each replacement occurs
  exactly once; their final mutant hashes are respectively `69ca5e80...`, `61135d02...` and
  `cded946d...`; each focused test is causally red and the target restores byte-identically.
- `FREP02_FINAL_COUNTER_MUTATIONS.json` now binds M01-M04 together to the final Technical commit,
  final baseline, final mutant bytes, raw failures and post-restore hash. The active correction
  validation and SHA manifest supersede the older counter-mutation manifest only for final closure.
- This additive correction changes no product, API, C# test, Solution, Workflow, Floor, package or
  provider byte. A clean Evidence-correction commit, CHANGELIST regeneration and two final read-only
  reviews remain mandatory. No F-REP-02 commit has been pushed.

## 2026-08-29 — F-REP-02 Git-identity and current-API correction

- The immediately preceding 65f0 mutation-correction paragraph was an uncommitted work-in-progress
  checkpoint and is superseded by this section. Both earlier committed chains are also terminally
  superseded: Technical/Evidence `1090e11e...` / `46d5747f...`, and Technical/Evidence/Audit
  `65f0d9b9...` / `3ed6a617...` / `f3b08ef5...`. Neither chain is an acceptance result.
- Final Technical commit `f6ba0c7f45cbabc16746018348d7aca6ffad563a`, tree
  `466c40d8aaa893739d12ae6dabbc33909d72618e`, has direct parent
  `f3b08ef5515e8154fc34d7bb96cbbd6e2dd1bd1c` and changes only the Identity gate and its Python tests.
- All 4,206 live baseline targets now bind content SHA-256, Git mode and Git blob oid. All 20,126
  current public declarations in `src/**` possess an exact reverse binding; 958 declarations without
  a baseline owner are explicit `CURRENT_ADDED` records. The complete projection has 36,013 records.
- Candidate Evidence is `PASS` with zero findings, 5,654 terminal baseline paths, 1,448 retired paths
  across 69 deletion commits/trees, 53 exact historical-policy paths, 5,772 text contexts and seven
  binary blobs. The full Identity suite is 127/127 green.
- M01-M06 are byte-bound to the final `identity_gate.py` SHA-256 `bf455efc...`; every replacement
  occurs once, every focused test is causally red and the post-restore hash is byte-identical.
- A direct final Evidence child, post-commit reconstruction and two independent read-only reviews
  remain mandatory. Product, API, C# tests, Solutions, Workflows, Floors and packages remain unchanged.
  No F-REP-02 commit has been pushed.

## 2026-08-29 — F-REP-02 final Evidence post-commit audit

- Final Evidence commit `a05cf3a7078f7190a10f49f32e3afe5c36f5dd86`, tree
  `f402ff15bc44198c06fa87d970012e97ff813241`, is the direct child of final Technical
  `f6ba0c7f45cbabc16746018348d7aca6ffad563a`.
- The committed-tree full scan is `PASS` with zero findings. Independent reconstruction reports
  zero mapping findings, zero Public-API findings, zero persisted Mapping drift and zero persisted
  Public-API drift while retaining 5,654 baseline records, 4,206 live Git identities, 36,013 API
  records, 20,126 current product declarations and 958 explicit `CURRENT_ADDED` records.
- The audit-child CHANGELIST contains 9,334 entries. Final SHA closure, the audit commit/tree and two
  independent read-only reviews remain mandatory. F-REP-02 remains open and local until those gates
  pass; no F-REP-02 commit has been pushed.

## 2026-08-29 — F-REP-02 Git-candidate and structured C# API correction

- Independent final review rejected the preceding `f6ba0c7f...` / `a05cf3a7...` / `3fb511aa...`
  chain: an ignored filesystem file could be treated as live Git identity, and the one-line public
  regex omitted implicit interface/enum members and multiline modifiers. That chain is explicitly
  superseded for acceptance, while its immutable bytes remain historical evidence.
- Correction Technical commit `f0afbc916d654e28b19ed852d1af2f2c8334315d`, tree
  `7c52237921980ac772e78f0930e8effc7255ad4c`, has direct parent
  `3fb511aaf5f954f0de591a9178aeeeb317eec465` and changes only the Identity gate and its Python tests.
- Baseline live/retired classification, change notices, refactor checks and API target lookup now use
  one tracked-plus-non-ignored-untracked Git-candidate snapshot. An existing ignored retired target
  remains retired and cannot acquire a synthetic Git blob identity.
- The current API projection tokenizes C# while excluding comments and literal contents, normalizes
  declaration whitespace, and binds explicit multiline/reordered public declarations plus the
  language-defined implicit public members of public interfaces and enums.
- Candidate Evidence is `PASS` with zero findings: 5,654 baseline paths, 4,206 live Git identities,
  1,448 retirements, 40,475 API records, 23,972 current product declarations and 985 explicit
  `CURRENT_ADDED` records. Identity self-tests are 130/130 and CI tool self-tests are 257/257 green.
- M01-M10 are byte-bound to baseline SHA-256 `3aef6742...`; ignored-candidate, implicit-interface,
  multiline-modifier and implicit-enum regressions are independently and causally red. A direct
  Evidence child, post-commit reconstruction and two fresh read-only reviews remain mandatory.
  No F-REP-02 commit has been pushed.

## 2026-08-29 — F-REP-02 structured-identity post-Evidence audit

- Evidence commit `e4ddc742b3e4c66b6e7f0a2ece92d6a554e39231`, tree
  `90074a6acf5532591d0d3256d77b48b9c65cf822`, is the direct child of Technical
  `f0afbc916d654e28b19ed852d1af2f2c8334315d`.
- The committed-tree full scan is `PASS` with zero findings and reproduces 5,654 baseline records,
  4,206 live Git identities, 1,448 retirements, 40,475 API records, 23,972 current product
  declarations and 985 explicit `CURRENT_ADDED` records.
- Independent candidate/API reconstruction reports zero live Git-identity drift, zero retired
  candidate resurrection, zero C# projection findings and zero current public-API set drift.
- Final audit hashing, CHANGELIST closure and two fresh independent read-only reviews remain. This
  chain is local and F-REP-02 remains open; no F-REP-02 commit has been pushed.

## 2026-08-29 — F-REP-02 conservative declaration-source and candidate closure

- The preceding `f0afbc91...` / `e4ddc742...` / `335f131f...` chain is superseded for acceptance:
  final review found truncated secondary base types, unbound accessor/protected declaration shape,
  symlink dereferencing and an ignored-artifact package inventory.
- The final correction range is `335f131f...` through `92ec4d496a916ab5597bfb301beacd4060497770`
  and `0451de02425d6faab3bf8ef50ee1cc271264596e` (tree
  `8096461f34fd0b9e3bfe390e903793e389678d59`). It changes only
  `tools/identity/identity_gate.py` and `tools/identity/test_identity_gate.py`.
- The declaration contract is now explicitly conservative source identity, not a claim that Python
  reproduces MSBuild/compiler evaluation: every conditional branch is retained with its directive
  context. Full base lists, multiple declarators, accessor shapes, public/protected declarations and
  implicit interface/enum members are bound.
- All scan/API/legal/package consumers use Git-candidate bytes. C# symlinks fail closed. Ignored
  projects and ignored build artifacts cannot enter `PACKAGE_INVENTORY.json`; build artifacts remain
  owned by the separate build-bound `ARTIFACT_GATE.json`.
- The full gate is `PASS` with zero findings: 5,654 baseline records, 4,206 live Git identities,
  1,448 retirements, 43,173 declaration records, 24,756 current product declarations and 1,364
  `CURRENT_ADDED` records. Identity tests are 136/136. M11-M19 are independently red and restore the
  exact Technical SHA-256.
- A direct Evidence child, committed-tree replay and two new independent read-only reviews remain
  mandatory. This correction is local; no F-REP-02 commit has been pushed.

## 2026-08-29 — F-REP-02 FINAL3 Evidence replay

- Evidence commit `5711faceb40c6a4512bb606e5768e6032a449141`, tree
  `bf63917f0698e83531b58bbc62bfdb2e960642a2`, is the direct child of final Technical
  `0451de02425d6faab3bf8ef50ee1cc271264596e`.
- Committed-tree reconstruction is exact: 43,173 declaration records with zero findings, 64 package
  source records with zero ignored artifacts, five generated contracts without manifest drift, and
  all M11-M19 replacements with one occurrence and the declared mutant hash.
- The remaining gates are the final audit hash/CHANGELIST child and two independent read-only PASS
  reviews. F-REP-02 remains local and open; no commit in this correction chain has been pushed.

## 2026-08-29 — F-REP-02 FINAL4 owner and authority closure

- The preceding FINAL3 chain ending at audit `e1600d0e10062caa7ff30f4907ebde4d7ebdc21b`
  is superseded for acceptance: final review found declaration-owner, conditional-branch-provenance
  and semantic-authority symlink gaps.
- The final Technical range is `e1600d0e...e0a7f66142bf9b5a7b6cb2872af0ff6447929abd`
  (tree `9b1e0b54aff0fa3fb8b39334f1d86b04c1dce190`) and changes only
  `tools/identity/identity_gate.py` and `tools/identity/test_identity_gate.py`.
- Namespace, containing-type and declaration-attribute identity, complete `if/elif/else` provenance,
  comment-aware directives and pre-tokenization identity mapping are closed. Historical policy,
  NOTICE, MODIFICATIONS, LICENSE and project inventory consume regular Git-candidate bytes and reject
  symlink authority.
- The full projection has 5,654 baseline records, 4,206 live Git identities, 43,520 declaration
  records, 24,756 current product declarations, 1,711 `CURRENT_ADDED` records and zero findings.
- A direct Evidence child, committed-tree replay and two independent read-only reviews remain
  mandatory. This closure is local and has not been pushed.

## 2026-08-29 — F-REP-02 FINAL4 post-Evidence audit

- Evidence commit `abc1a368728b59d608d0dcc057b3f675e2ede410`, tree
  `c45e286f7c1077326aa3b92034153414a8a23c38`, is the direct child of final Technical
  `e0a7f66142bf9b5a7b6cb2872af0ff6447929abd`.
- Committed-tree reconstruction is exact: 43,520 declaration records with zero findings, 64 package
  source records with zero ignored artifacts, five generated contracts without manifest drift and
  all M11-M27 replacements at one occurrence with the declared mutant hashes.
- Final audit hashing, CHANGELIST closure and two independent read-only reviews remain. F-REP-02 is
  still local and open; no commit in this FINAL4 chain has been pushed.

## 2026-08-29 — F-REP-02 FINAL5 declaration-edge closure

- FINAL4 through audit `8cfa46231e126fe615263c171f99a7b24197f36f` is superseded for acceptance:
  final review found accessor and conditional-attribute identity gaps plus unstable same-line pairing.
- Technical `e044f1acba55b917e23fa9a2698c31bfb28120f3`, tree
  `6f42c023d8417a6e328713becd885e1a6abe6229`, changes only the two identity-tool paths.
  Accessor attributes, conditional declaration attributes and same-line traversal order are bound;
  conditionally unbalanced structural branches receive a conservative full-token identity.
- Correct empty-string tokenization exposes the previously swallowed declaration tail: the exact
  projection is now 44,081 records, 24,987 current product declarations, 1,702 `CURRENT_ADDED` and
  zero projection findings. Product, policy, legal and package bytes are unchanged.
- Identity self-tests and M11-M32 are being frozen in the direct Evidence child. Independent delta
  reviews, architecture acceptance and an explicitly authorized non-force remote push remain open.

## 2026-08-29 — F-REP-02 FINAL5 post-Evidence audit

- Evidence `13a27ec786c27275db438f9e4fcb23e3fe72c5e1`, tree
  `45ad6abfae5b5a76e1873e84b42038848db023ee`, is the direct child of Technical
  `e044f1acba55b917e23fa9a2698c31bfb28120f3`.
- Committed-tree reconstruction is exact: 44,081 API records with zero findings, 64 package records
  with zero artifacts, the exact Technical patch, and M11-M32 at one occurrence with matching mutant
  and post-restore hashes. Identity self-tests are 147/147.
- Only the two independent read-only delta reviews and architecture acceptance remain before asking
  for fresh remote-push authorization. Nothing in FINAL5 has been pushed.

## 2026-08-29 — F-REP-02 FINAL6 conditional-accessor closure

- FINAL5 through `dd5122977173e307f3a7f1c27e122a9da97022d9` is superseded for acceptance:
  final review found that conditional context was bound to declarations but not to each accessor.
- Technical `39a4acaff1a06f0462d829f332b9fc25b3941a6f`, tree
  `061b4236b36646ff2331a271c6b82a6ae0cdd3fb`, binds complete branch provenance to property and event
  accessors. The API projection remains 44,081 records / 24,987 current / 1,702 `CURRENT_ADDED` with
  zero findings; 148/148 identity tests and M11-M33 are bound.
- The direct Evidence child, committed replay and two final read-only delta reviews remain. FINAL6 is
  local and has not been pushed.

## 2026-08-29 — F-REP-02 FINAL6 post-Evidence audit

- Evidence `06acb2f1275a05608cf379f0f27dfc15dc97ba72`, tree
  `51c408bee8c1fad85b7128b105e3e930c4d8e7b1`, is the direct child of Technical
  `39a4acaff1a06f0462d829f332b9fc25b3941a6f`.
- Committed replay is exact: 44,081 API records, zero findings, unchanged 64/0 package inventory,
  byte-identical Technical patch and M11-M33 with exact occurrence, mutant and restore hashes.
- Only two final independent delta reviews and architecture acceptance remain. No FINAL6 commit has
  been pushed.

## Diagnostics native closure — pre-Evidence-commit checkpoint (2026-08-29)

- Technical `ae2ae679e3b1d157d10e0b40cdc733eca9bd53f0`, tree
  `233eb7777ea001738bb921c2d4e8935abb8b5cfd`, replaces all 44 inherited Diagnostics identities
  `OBL-R0-SML-0222..0265` with 44 unique xUnit 4/MTP v2 carriers plus one requirement-projection
  Fact. The inherited three C# files, NUnit/VSTest project, lock file and empty `tests/Tools`
  directory are removed.
- The final focused run is 45/45 and the complete UnitArchitecture run is 2,335/2,335 with zero
  failures or skips. The complete Engineering Release build ends with zero warnings and errors;
  the verification model passes and 257/257 CI tool self-tests pass outside the restricted sandbox.
- M01-M05 independently bind fake-time budget completion, snapshot ordering, exactness, fallback
  delivery and quiescence interpretation. All five build successfully and make only their named
  carrier red in under one second; every mutated file is restored byte-identically.
- This is explicitly the pre-Evidence-commit checkpoint. The Evidence child, independent read-only
  acceptance, architecture binding and remote push remain pending and are not self-approved here.

## Diagnostics native closure — post-Evidence CHANGELIST correction (2026-08-29)

- Evidence `b47aa04b367f853e3b6e4e302664f8218b233dce` is the direct child of Technical `ae2ae679e3b1d157d10e0b40cdc733eca9bd53f0`.
- The 17 hash-bound raw logs are now tracked explicitly; the generated CHANGELIST therefore closes at 9,439 entries. Product, test, mutation and result bytes are unchanged.

## SQL transport native closure — pre-Evidence-commit checkpoint (2026-08-30)

- Technical `99665084c190f2affc9540c383a9d11172824ce0`, tree
  `765093f8acc43ca997e88f377c890293cebd985e`, closes all 135 SQL obligations:
  134 are `REPLACED_EXECUTING` and only obsolete runtime dialect resolver
  `OBL-R0-SQL-0054` is explicitly retired.
- The inherited 40-file NUnit/VSTest SQL project and its empty directories are absent. Native SQL
  execution is 45/45 hermetic, 71/71 PostgreSQL and 60/60 SQL Server. Complete UnitArchitecture is
  2,399/2,399 and LocalIntegration is exactly 244 + 71 + 60 = 375/375, all without skips.
- Locked Engineering restore and Release build pass; the build has 0 warnings and 0 errors. The
  verification model passes and the CI tool suite passes 257/257 outside the restricted sandbox.
- M01-M05 independently bind transient provider selection, run-scoped database identity, address
  rejection, concurrent schema uniqueness and delivery identifiers. Every mutant builds cleanly,
  is killed by its named behavioral carrier and is restored to its exact Technical-tree SHA.
- This is explicitly the pre-Evidence-commit checkpoint. The direct Evidence child, regenerated
  identity/CHANGELIST closure, architecture binding and remote push remain pending. No SQL closure
  commit in this local range is self-approved or newly pushed.

## Core transform native closure — pre-Evidence-commit checkpoint (2026-08-30)

- Technical `901e6d6d854b1a8231224dc468a50e1816b2a9e8`, tree
  `03b05696fefa122f3f746f11912c456437034c56`, replaces all nine transform obligations
  `OBL-R0-CORE-C-0441..0449` with nine unique xUnit 4 / MTP v2 carriers.
- The three fully replaced inherited Transform files and their now-empty directory are removed.
  No empty directory remains below `tests/ViciOne.ServiceBus.Tests`.
- Focused execution is 9/9, requirement projection is 1/1 and the full UnitArchitecture profile is
  2,408/2,408 with zero failures or skips. Locked Engineering restore and Release build pass with
  zero warnings and errors; CI self-tests pass 257/257 outside the restricted process sandbox.
- M01-M04 independently kill replacement, send-filter, consume-filter and class-construction
  regressions. Every mutant builds cleanly, fails only its named carrier and is restored exactly.
- This is explicitly the pre-Evidence-commit checkpoint. The direct Evidence child, final
  CHANGELIST/identity fixed point, architecture binding and any future remote push remain pending.

## Message-fabric and topic-routing native closure — pre-Evidence-commit checkpoint (2026-08-30)

- Technical `266ae0d4369c9ffa6860db4e5a8f2f8fded18be1`, tree
  `5a28b0f2f52ab4a6addb4b7c458e31bd2a4b634c`, replaces the exact five inherited identities
  `OBL-R0-CORE-C-0450..0454` with five source-mirrored xUnit 4 / MTP v2 carriers.
- The two fully replaced inherited Transports C# files and their now-empty directory are removed.
  A repository check below `tests/` finds no remaining empty directory.
- Focused execution is 5/5, requirement projection is 1/1 and complete UnitArchitecture is
  2,413/2,413 with zero failures or skips above the independent 2,408 floor. Locked Engineering
  restore and Release build pass with zero warnings and errors; CI tool tests pass 257/257 outside
  the restricted sandbox and the verification model passes.
- M01-M04 independently bind cycle validation, hash routing and both terminal/intermediate wildcard
  branches. Every mutant builds cleanly, makes only its named carrier red and restores the exact
  Technical-tree file SHA before the final positive build and tests.
- This remains a pre-Evidence-commit checkpoint. Final identity/CHANGELIST regeneration, the direct
  Evidence child, architecture binding and any remote publication remain pending and are not
  self-approved here.

## Core send-topology native closure — pre-Evidence-commit checkpoint (2026-08-30)

- Technical `a23e9f58bcb7b849a3f36116df16faab3c61d161`, tree
  `8e7ccae6aa39493438d9e8ca2249630498637283`, replaces the exact two inherited identities
  `OBL-R0-CORE-C-0428..0429` with two source-mirrored xUnit 4 / MTP v2 carriers.
- The two fully replaced inherited Topology C# files and their now-empty directory are removed.
  A repository check below `tests/` finds no remaining empty directory.
- Focused execution is 2/2, requirement projection is 1/1 and complete UnitArchitecture is
  2,415/2,415 with zero failures or skips above the independent 2,410 floor. Locked Engineering
  restore and Release build pass with zero warnings and errors; CI tool tests pass 257/257 outside
  the restricted process sandbox and the verification model passes.
- M01-M03 independently bind global and interface correlation selectors plus the message-specific
  Raw-JSON serializer. Every mutant builds cleanly, makes only its named carrier red and restores
  the exact Technical-tree file SHA before the final positive build.
- This is explicitly a pre-Evidence-commit checkpoint. Final identity/CHANGELIST regeneration, the
  direct Evidence child, architecture binding and any remote publication remain pending and are not
  self-approved here.

## Consumer-factory middleware retirement — pre-Evidence-commit checkpoint (2026-08-30)

- Technical `5937ebeb0b86e38ca420a482fb5b152e68b701d8`, tree
  `f43f69958d562f488056fea3ef159066cc793ca4`, binds `OBL-R0-CORE-D-0032` to the already stronger
  native two-case ConsumerMessageConfiguration theory instead of adding a duplicate test.
- The fully replaced inherited `ConsumerFactoryMiddleware_Specs.cs` is removed; the non-empty legacy
  project root is retained and no empty directory remains below `tests`.
- Focused execution is 2/2, requirement projection 1/1 and UnitArchitecture 2,415/2,415. Locked
  Engineering restore and Release build pass with zero warnings/errors; CI tool tests pass 257/257
  outside the restricted process sandbox and the verification model passes.
- M01 removes consumer pipe-specification forwarding: only ConsumerFactory turns red (3 instead of 4
  layers), Instance remains green, and the product source is restored byte-identically.
- This remains a pre-Evidence-commit checkpoint. Identity/CHANGELIST fixed point, the direct Evidence
  child, architecture binding and any remote publication remain pending and are not self-approved.

## Consumer request/response retirement — pre-Evidence-commit checkpoint (2026-08-30)

- Technical `1e86d84b46a0b6ca2febd3dec4002f82e1a59e30`, tree
  `85587426fd17b1fd3e30abc19dfebcb24ee99244`, binds `OBL-R0-CORE-D-0033` to the already stronger
  native DI-harness request/response Fact instead of adding a duplicate test.
- The fully replaced inherited `Consumer_Specs.cs` is removed; the non-empty legacy project root and
  every unrelated inherited test remain. No empty directory remains below `tests` or `tests2`.
- Focused execution and requirement projection are each 1/1; current complete UnitArchitecture is
  2,415/2,415. The unchanged 21 CTRFs remain hash-bound in direct ancestor Evidence `d717536d`.
  Locked Engineering restore and Release build pass with zero warnings/errors; CI tool tests pass
  257/257 outside the restricted process sandbox and the verification model passes.
- M01 removes endpoint attachment for the scoped consumer: the product still builds cleanly, the
  Request endpoint has zero consumers and the exact native carrier turns red on request timeout.
  The product source is then restored byte-identically.
- This is explicitly a pre-Evidence-commit checkpoint. The direct Evidence child, final generated
  CHANGELIST, architecture binding and any remote publication remain pending and are not self-approved.

## In-memory dual-host retirement — local Evidence freeze (2026-08-30)

- Technical `47f73aa331a58328b133ccf9d105cd1d824ffccd`, tree
  `4e279274d7365b1285457738b4c64b5197baca78`, replaces the exact dual-host obligation
  `OBL-R0-CORE-D-0175` with one stronger source-mirrored xUnit 4 / MTP v2 carrier.
- Direct Evidence child `7c11ae93977bc788533614fc760459fea43d1fb0`, tree
  `eb37206fcacd49221c72f25bbc67efb50cc4b6d6`, binds 32 nonmanifest artifacts with 32/32
  SHA-256 closure, the byte-exact Technical patch and two independently reconstructible mutants.
- Focused carrier and requirement projection are each 1/1. Complete UnitArchitecture is
  2,416/2,416 with zero failures or skips; focused and Engineering Release builds have zero
  warnings/errors; CI self-tests are 257/257, identity self-tests 148/148 and the verification
  model passes.
- The final identity gate passes with zero findings over 5,654 baseline paths and 24,987 current
  product declarations; generated CHANGELIST closes at 9,810 entries. No empty test directory
  remains. No remote push was performed.
- Independent read-only acceptance, architecture binding and any future remote publication remain
  pending and are not self-approved by this local freeze.

## In-memory outbox filter retirement — local Evidence freeze (2026-08-30)

- Technical `917f54f60a5c237b1d6be477c383d4c800d91c4e`, tree
  `b68e12b805b89b8ae8a07ecb7bc8d1ccd622d1f6`, replaces `OBL-R0-CORE-D-0176..0181`
  with six source-mirrored xUnit 4 / MTP v2 filter carriers.
- Direct Evidence child `cd3c141aebd25fd9e0b72b0d684e6e3a5a7ea3c6`, tree
  `6eef0b7450e5cd63dcdb560d10a9fc46f9ea6b85`, binds 37 nonmanifest artifacts with
  37/37 SHA-256 closure, the byte-exact Technical patch and four independently reconstructible
  product mutations.
- Focused execution is 6/6 and requirement projection 1/1. Complete UnitArchitecture is
  2,422/2,422 with no failures or skips; focused and Engineering Release builds have zero
  warnings/errors; format verification changes zero files; CI self-tests are 257/257, identity
  self-tests 148/148 and the verification model passes.
- The full identity Evidence scan passes with zero findings over 5,654 baseline paths and 24,987
  current product declarations; generated CHANGELIST closes at 9,848 entries. Both fully replaced
  inherited files are removed, their non-empty project remains, and no empty test directory exists.
- The next migration unit is deliberately enlarged to `OBL-R0-CORE-D-0182..0199`; Full Build,
  UnitArchitecture, Identity and Evidence are run once for the complete package instead of per
  individual carrier. No remote push was performed.
- Independent read-only acceptance, architecture binding and any future remote publication remain
  pending and are not self-approved by this local freeze.

## Core obligations 0182–0199 — large-package local Evidence freeze (2026-08-30)

- Technical `d74650ff20469fe3e244ca63ae5d5215aaeb1830`, tree
  `fbde8d428755178821056310e299c085b68da49b`, closes the contiguous range
  `OBL-R0-CORE-D-0182..0199`: five new executable cases carry eight obligations and ten obligations
  bind to already stronger executing native carriers.
- Four fully replaced inherited test files are removed atomically. The technical package adds 453
  lines and deletes 507, so the test estate becomes smaller by 54 lines while closing 18 obligations.
- Direct Evidence child `c60943efccd9df48b12eaf33e1b8b02cde8a4785`, tree
  `9ebadede182c42be8069a23c5f225deb230408f3`, binds 45/45 nonmanifest SHA-256 entries, the
  byte-exact Technical patch, complete raw/binlog evidence and five independently reconstructible
  product mutations.
- The five new cases are 5/5 and requirement projection is 1/1. Complete UnitArchitecture is
  2,427/2,427 with zero failures or skips above the 2,422 floor; focused and post-restore builds are
  0 warnings / 0 errors; CI self-tests are 257/257 and identity self-tests 148/148.
- M01–M05 independently bind failed-attempt outbox discard, consumer-factory interception,
  all-interface dispatch, consumer-message redelivery and endpoint redelivery. Every mutant builds,
  the intended carrier turns red, controls remain green where applicable, and all source hashes are
  restored exactly.
- Generated CHANGELIST closes at 9,897 entries; no empty legacy test directory remains. No remote
  push was performed. Independent read-only acceptance, architecture binding and any future remote
  publication remain pending and are not self-approved here.

## Core Job Service obligations 0201–0227 — large-package local Evidence freeze (2026-08-30)

- Technical `b2aa104c3bea11d0ff33df16271a97bf31073fc0`, tree
  `7a5dce633444615cbd412b712974bf62b33bc406`, closes all 27 consecutive Job Service obligations
  `OBL-R0-CORE-D-0201..0227` with 18 source-mirrored xUnit methods and 28 executable cases.
- Five fully replaced inherited files and 1,697 legacy test lines are removed atomically. The complete
  Technical delta adds 1,604 lines and deletes 1,709, so this package reduces the repository by 105
  lines while increasing the executable UnitArchitecture floor from 2,422 to 2,450.
- Direct Evidence child `848ae1ba1cbeb88af05dede4b63c491364e41d93`, tree
  `412648c7b5ba0f87998704ced0c95cce5bc18715`, binds 50/50 nonmanifest SHA-256 entries,
  the byte-exact Technical patch, 21 complete-profile CTRFs and six independent mutation CTRFs.
- Focused Job Service execution is 28/28. Complete UnitArchitecture is 2,455/2,455 with zero failed,
  skipped, pending or other cases; full and post-restore Release builds have zero warnings/errors;
  scoped format verification is green.
- M01–M06 independently bind stale-attempt generation, registration-context outbox, heartbeat drain,
  admission closure, retry generation and containerless outbox. Every mutant builds, its intended
  carrier turns red, and all product-source hashes are restored exactly.
- Verification Model, 257/257 CI self-tests, 148/148 identity self-tests and the generated 9,950-entry
  CHANGELIST are green. No empty legacy test directory remains and no cloud fixture was required.
- No remote push was performed. Independent read-only acceptance, architecture binding and any future
  remote publication remain pending and are not self-approved by this local freeze.
