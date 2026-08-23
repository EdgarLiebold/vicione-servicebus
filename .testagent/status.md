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
- UnitArchitecture: 882 total, 882 passed, 0 failed, 0 skipped;
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
