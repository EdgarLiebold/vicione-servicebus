# A+ remediation test status

## Iteration 1

Iteration 1 is complete and ready for Git capture. The tests use xUnit 4 on Microsoft Testing Platform v2 and introduce no sleeps, ignored/skipped cases, swallowed exceptions, or assertion-free test bodies.

## Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| Entity Framework abandonment timestamp | 1 failed | 1 passed |
| Receiver configuration forwarding | 1 failed | 1 passed |
| In-memory `AutoStart` | 1 failed | 1 passed |
| Endpoint registration inclusion | 1 failed | 1 passed |
| Composite filter shape and semantics | 1 failed | 1 passed |
| Azure message-session query | 3 failed with `NotImplementedException` | 3 passed |
| Azure message-session state-write cancellation | 2 of 3 failed | 3 passed |
| Job lifecycle cancellation | 6 of 7 failed | 7 passed |
| Static `NewId` façade | 1 of 2 failed | 2 passed |

## Test quality

- Persistence is asserted after reopening the store with a fresh Entity Framework context and compares the exact supplied timestamp.
- Configuration tests observe the authoritative downstream owner instead of relying only on setter round trips.
- Azure query tests cover matching and non-matching predicates, identity, count, and pre-cancellation.
- Cancellation tests compare exact token identity at each relevant provider, transport-send, and progress-buffer boundary.
- Reflection assertions constrain the intended public API shape and are paired with behavioral tests.
- `DispatchProxy` is limited to protocol-boundary doubles where a full broker connection would obscure the unit contract.

Nine one-cause mutation groups were executed and restored byte-for-byte: Entity Framework timestamp persistence, receiver forwarding, in-memory auto-start, endpoint inclusion, composite exclusion semantics, Azure query predicate evaluation, Azure state-write cancellation, job notification cancellation, and static `NewId` mutability. Every mutation was killed by its owning test project. The Azure mutation run also established that the owning test project must be rebuilt because rebuilding only a referenced product project can leave a stale copied assembly beside the Microsoft Testing Platform executable.

## Full validation

- Release unit-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,728 passed, 0 failed, 0 skipped across 21 assemblies.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering-solution whitespace verification: passed after correcting one indentation finding in the new Azure test.
- Engineering-solution style verification at warning severity: passed.

The previously measured whole-product baseline remains 70.1% line coverage and 55.6% branch coverage. Coverage will be recollected after the remaining remediation iterations so the final report represents the final code rather than an intermediate snapshot.

## Iteration 2

Iteration 2 resolves the SQL URI materialization and topology-name collision findings.

### Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| PostgreSQL and SQL Server URI materialization | 10 failed, 4 passed | 14 passed |
| Core bounded temporary names | 3 failed | 3 passed |
| Azure subscription naming | 4 failed, 6 passed | 11 passed |

The SQL contract now round-trips relative and absolute `Uri` instances accepted by the write path and rejects language null, `DBNull`, blank text, malformed text, and non-string values explicitly. Topology shortening is owned by one internal implementation using SHA-256 and a 13-character Base32 suffix, providing 65 suffix bits while retaining a readable prefix and each provider's exact maximum length.

Five isolated mutations were killed and restored: PostgreSQL relative-value rejection, SQL Server relative-value rejection, reduction of the shared hash suffix from 13 to six characters, removal of the Azure public parameter guard, and removal of the Core minimum-length guard.

### Full validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,751 passed, 0 failed, 0 skipped across 21 assemblies.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace verification: passed.
- Engineering style verification at warning severity: passed.

A repeat full-profile run exposed an existing observation-test race: the handler completion signal could precede publication into the consumed-message observer list. The test now awaits the public observation signal before taking a deliberately non-waiting snapshot. The formerly failing test passed ten isolated repetitions and the final complete profile. The private async iterator in the same file was also renamed from `Empty` to `EmptyAsync`, closing the previously recorded bidirectional async-naming exception.

## Iteration 3

Iteration 3 closes the direct behavior gaps around all 29 properties on `SendOptions`, `PublishOptions`, `ScheduleOptions`, and `RequestOptions`, their application entry points, the generic request-client wrapper, consume-context outgoing operations, and the reliable-messaging read and reference partitions.

### Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| Unsupported application partition key | silently ignored | explicit failure before context mutation |
| Explicit request identity | request timed out because response matching retained a generated identifier | exact request/response identifier round trip |
| Reliable inbox query boundary | four invalid inputs reached the provider | all invalid inputs rejected before provider I/O |
| Reliable scheduler options | options overload rejected by the reliable scheduler | all supported envelope metadata persisted and replayed |

The tests exercise direct `ISendEndpoint`, `IPublishEndpoint`, `IMessageScheduler`, `IRequestClient<T>`, `IOutgoingMessages`, `ConsumeContext` response, generic request-client, and `IReliableMessagingOperations<TBus>` entry points. Every application options property is asserted independently. Defaults, nulls, exact cancellation-token identity, an injected-clock deadline boundary, unsupported partition capability, both reliable-reference kinds, and complete/incomplete cursor shapes are included.

Four isolated mutation groups were killed and restored: reintroducing silent partition-key discard, removing the request-identifier assignment from the common options pipe, bypassing facade-level inbox-query validation, and dropping scheduled envelope metadata before durable persistence. The original explicit-request-identity red run additionally timed out before the request handle itself was corrected to own the configured identity.

### Test quality

The new tests contain no sleeps, wall-clock polling, skipped cases, broad exception catches, tautological assertions, or assertion-free test bodies. Protocol-boundary doubles record exact objects and cancellation tokens; in-memory integration tests independently prove round trips through the public application surface. The common inbox-query validator remains internal and therefore does not enlarge the public provider API.

### Full validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,767 passed, 0 failed, 0 skipped across 21 assemblies.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace verification: passed.
- Engineering style verification at warning severity: passed.

## Iteration 4

Iteration 4 makes physical source navigation deterministic across every evaluated product and native-test compile item. Public production types and test classes now live in files named for their declared type. Multi-type declaration groups and partial implementation fragments are guarded by exact path-to-type manifests rather than permissive path exceptions.

The architecture guard rejects stale manifest entries, unexpected type identities, declarations hidden in infrastructure files, secondary test classes including qualified xUnit attributes, and generated-file suffixes without an actual generated-code header. A repository-wide declaration comparison found no removed top-level types, public API symbols, or public parameters. Pure moves and renames were verified byte-for-byte.

Four isolated mutation groups were killed and restored: a wrong single-type filename, a changed cohesive group, a changed partial-fragment owner, and misuse of both a global-usings file and a qualified `[Xunit.Fact]` secondary test class. The complete profile subsequently passed with the restored sources.

### Full validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,768 passed, 0 failed, 0 skipped.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace verification: passed.
- Engineering style verification at warning severity: passed.

The Red Team work in this iteration was an internal adversarial review. It found and caused the correction of overly broad manifest, infrastructure-file, generated-file, and qualified-test-attribute handling. It is engineering evidence, not independent acceptance.

## Iteration 5

Iteration 5 modernizes the `NewId` formatting/parsing boundary to read-only spans with exact input validation, removes the custom `NotImplementedByDesignException` contract, removes 105 obsolete binary-serialization attributes from 103 product files, and removes the former state-machine product identity. The entity-name shortener explicitly truncates its SHA-256 digest to the formatter's 128-bit contract while preserving its existing 65-bit public suffix policy.

### Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| Managed-object identifier signatures | `in string` / `in byte[]` exposed | exact `ReadOnlySpan<char>` / `ReadOnlySpan<byte>` surface |
| Formatter byte boundary | inconsistent or unchecked | all four formatters reject 15 and 17 bytes |
| Null custom alphabet | `NullReferenceException` | `ArgumentNullException` with exact parameter |
| Unsupported capability identity | custom public exception at nine source sites | standard `NotSupportedException`; custom type removed |
| Binary serialization metadata | 105 attributes in 103 current files | none in evaluated product sources |
| Former state-machine identity | four product occurrences | none in source or package metadata |

Five isolated mutations were killed and restored byte-for-byte: accepting a 17-byte formatter input, restoring an `in string` public parameter, restoring the custom unsupported-capability exception, adding a serialization attribute, and restoring the former package identity.

### Full validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Repeated complete Unit/Architecture profile: 3,777 passed, 0 failed, 0 skipped.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace and warning-level style verification: passed.
- One unrelated nested-request integration timeout from the first complete run passed 10/10 isolated repetitions and the repeated complete profile; it remains recorded for later load-sensitivity hardening.

## Iteration 6

Iteration 6 closes the mutable-global-state and RabbitMQ cluster-node parsing findings. Message, type, property, and consumer metadata now expose stable read-only collections; array-dependent internal boundaries receive defensive copies. No-argument routing-slip activities use a private read-only sentinel. `ClusterNode` now follows the standard string/span parsing pattern, canonicalizes IPv6 with brackets, treats a missing port distinctly, and rejects malformed hosts and ports outside `1..65535`.

### Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| Message metadata cache immutability | 2 failed, 348 passed | 350 passed |
| Consumer cache and routing-slip isolation | 3 failed, 4 passed | 7 passed |
| Core metadata facade | added as a direct regression guard | 1 passed |
| RabbitMQ cluster-node standard parsing | test project failed to compile on the missing API | 19 focused cases and all 184 RabbitMQ tests passed |

Five isolated regressions were killed and restored byte-for-byte: direct cached-array exposure, a mutable shared Courier dictionary, a public `NoArguments` field, reversed nullable-port formatting, and acceptance of TCP port zero.

### Full validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,802 passed, 0 failed, 0 skipped.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace verification: passed.
- Engineering style verification at warning severity: passed.
- Requirement manifests and Git whitespace: passed.

## Iteration 7

Iteration 7 removes runtime capability probing from the application-facing interfaces. `ISendEndpoint`, `IPublishEndpoint`, `IMessageScheduler`, and `ConsumeContext<T>` now declare their application operations as compile-time requirements. Advanced interfaces provide exact options and scheduled-cancellation adapters only where their lower-level capabilities implement the full contract.

The compiler identified every affected production implementation. Completing the shared untyped consume-context and base-context contracts closed 18 typed forwarding-context errors coherently; the complete unit-solution build then identified the only two minimal test doubles requiring explicit application-option behavior.

### Red/green and mutation evidence

- The baseline architecture test reported nine runtime-default members across send, publish, scheduling, and typed consume contracts.
- After remediation, the focused architecture rule and all existing direct options tests passed.
- Reintroducing an `ISendEndpoint` default body was killed by the exact reflection guard.
- Dropping the advanced send options pipe was killed by the in-memory envelope assertion.
- Both controlled mutations were restored before final validation.

### Full validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,803 passed, 0 failed, 0 skipped.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace and warning-level style verification: passed.
- Requirement manifests and Git whitespace: passed.

## Iteration 8

Iteration 8 makes application outgoing options stable before asynchronous work. The four public options records use immutable empty header defaults; send, publish, schedule, request, and nested durable-schedule paths copy caller-owned headers into a frozen snapshot before awaiting an endpoint. A message lifetime must now be greater than zero and fails at options-pipe construction or request entry before provider I/O.

The only product C# file absent from every evaluated compile graph was an unused, unimplemented, and semantically stale `IJobSagaOptionsConfigurator` source artifact. It was not part of any current assembly and provided no runtime feature; it was removed rather than reintroduced as a misleading compatibility-only contract. A complete architecture guard now requires every physical product source file to have exactly one evaluated compile owner, preventing both silent source loss and duplicate type ownership.

### Red/green and mutation evidence

- Before implementation, the outgoing-options class had 6 failures among 12 cases and the request-options partition had 3 failures among 4 cases.
- After implementation, all focused cases and the complete profile passed.
- Retaining caller-owned headers was killed by all three send/publish/schedule snapshot tests.
- Allowing a zero lifetime was killed by the exact zero-boundary test.
- Replacing one immutable default with a mutable dictionary was killed by the default-shape test.
- Adding a temporarily unowned product source was killed by the compile-ownership guard, which reported the exact path and zero owners.
- Every mutation was removed before final validation.

### Full validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,813 passed, 0 failed, 0 skipped.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace and warning-level style verification: passed.
- Requirement manifests, exact compile ownership, and Git whitespace: passed.

## Iteration 9

Iteration 9 enables central transitive pinning for all 76 centrally managed projects and refreshes
the affected lock graphs. All 599 `CentralTransitive` entries now resolve the exact centrally
declared minimum, and the central catalog rejects unused declarations. Provider-testing projects
depend on DI abstractions, while the only test project requiring the concrete container declares it
directly. The EF Core and state-machine visualizer capability graphs no longer contain their
unnecessary direct edges.

The package gate now packs all 19 journey dependencies and executes three isolated provider-testing
consumers. Each consumer has exactly one direct ViciOne package reference, so another package cannot
mask a missing delivery dependency. The package-only public API inventory was also corrected to
exclude nondeterministic archive/binary hashes; two independent complete runs produced byte-identical
21,456-line inventories for 17 assemblies at SHA-256
`9ab6338dc80bde415609ac282c141f4c006168d6a9e10b964aea7c2137f44c27`.

### Red/green and mutation evidence

- The initial 215-test architecture profile had seven failing dependency, package, and documentation contracts; the corrected profile passed 215/215.
- Four isolated mutations were killed: disabled transitive pinning, a mismatched central-transitive lock version, a full-container testing dependency, and an extra direct ViciOne consumer dependency.
- A pre-correction double pack proved NuGet archive hashes differed; the corrected structural API inventories were byte-identical across two complete fresh-package runs.
- Every mutation was removed before final validation.

### Full validation

- Engineering locked restore: passed for the complete 76-project solution graph.
- Release Engineering build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,818 passed, 0 failed, 0 skipped.
- Package-consumer gate: 18 journeys, 19 packages, and 3 isolated consumers passed.
- Shipping solution pack: 26 packable artifacts produced from 30 solution projects.
- Current NuGet advisory inventory: 0 findings and 0 unresolved paths.
- Engineering whitespace, warning-level style, architecture manifests, and Git whitespace: passed.

## Iteration 10

Iteration 10 expands the fresh-package gate from nineteen journey dependencies to the complete
thirty-package delivery catalog and makes the packed API inventory enforceable. A dedicated
package-only consumer directly restores all twenty-nine runtime packages, and the reflector now
loads the ASP.NET Core shared framework needed by SignalR. The resulting 24,000-line contract is
tracked at `docs/api/packed-public-api.txt`; normal runs compare it byte-for-byte, while deliberate
API changes require the explicit `--update-public-api-contract` operation.

### Red/green and mutation evidence

- The initial architecture contract failed because the tracked contract and complete package
  consumer were absent.
- The first complete run exposed and then closed the SignalR shared-framework resolution gap.
- Two independent complete package runs produced the identical SHA-256
  `28e7a84a58a2cbdbac689f41e193c9f54cc827dcaef80d93701da341458fb0c6`.
- Four isolated mutations were killed: disabled comparison, one missing runtime consumer package,
  one missing expected package artifact, and a one-line tracked-contract drift.
- Every mutation was restored before final validation.

### Full validation

- Release Unit/Architecture build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,818 passed, 0 failed, 0 skipped.
- Complete Architecture profile: 215 passed, 0 failed, 0 skipped.
- Release Engineering build with warnings as errors: passed, 0 warnings and 0 errors.
- Fresh-package comparison gate: 18 journeys, 30 packages, 3 executable provider consumers, and
  all 29 runtime package APIs passed.
- Engineering whitespace, warning-level style, and Git whitespace: passed.

## Iteration 11

Iteration 11 preserves caller cancellation across timeout wrappers, job shutdown, ActiveMQ
destination cleanup, and state-machine harness polling. Generic and non-generic timeout helpers now
retain the exact caller token both before entry and while waiting. Job cancellation distinguishes a
caller-aborted wait from expected job-owned cancellation. ActiveMQ propagates cancellation through
bounded executor admission for queue and topic deletion, and the test harness no longer creates a
non-cancelable polling delay.

### Red/green and mutation evidence

- The initial focused profiles failed in all eight new behavior partitions: four timeout cases, one
  job-handle case, two ActiveMQ cases, and one state-polling case.
- Five isolated mutations were killed: pre-cancellation translated to timeout, in-flight
  cancellation translated to timeout, an omitted job wait token, an omitted ActiveMQ admission
  token, and a non-cancelable state-poll delay.
- Every mutation was restored before final validation.

### Full validation

- Release Unit/Architecture build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,826 passed, 0 failed, 0 skipped.
- Release Engineering build with warnings as errors: passed, 0 warnings and 0 errors.
- Fresh-package comparison gate: 18 journeys, 30 packages, 3 executable isolated consumers, and
  all 29 runtime package APIs passed against the unchanged 24,000-line contract.
- Engineering warning-level format verification and Git whitespace validation: passed.

## Iteration 12

Iteration 12 makes asynchronous naming and public cancellation shape executable repository-wide
contracts. The Roslyn gate scans evaluated product and native-test sources, including local
functions, while preserving externally imposed interface names and explicit asynchronous delegate
configuration. Two misleading synchronous SignalR `AsyncCore` methods and four completed-task test
helpers now expose synchronous names and signatures.

The complete request-handle factory family now follows the .NET final-token convention:
`RequestTimeout` precedes `CancellationToken`. Interfaces, implementations, mediator and DI
adapters, call sites, XML parameter order, package consumers, and the packed public API contract
were updated together. Two argument-recording tests prove exact typed-message and initializer-value
forwarding through the generic DI wrapper.

### Red/green and mutation evidence

- The cancellation scan separated forty-eight genuine request declarations from four intrinsic
  extension-receiver or dual-token shapes and passed only after every genuine signature changed.
- The bidirectional naming scan rejected both synchronous SignalR `AsyncCore` methods; the
  adversarial inventory also removed four synchronous helpers that manufactured completed tasks.
- Three isolated mutations were killed: a restored `AsyncCore` name, a token moved before timeout,
  and a silently dropped timeout in the generic request wrapper.
- Every mutation was restored before final validation.

### Full validation

- Release Engineering build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,830 passed, 0 failed, 0 skipped.
- Fresh-package comparison gate: 18 journeys, 30 packages, 3 executable isolated consumers, and
  all 29 runtime package APIs passed.
- Packed public API contract: 24,000 lines, SHA-256
  `36a9b02c2417bfe12abf7be4858236cc23604afffa0fadb7fe38972217f510ec`.
- Requirement projections, architecture manifests, warning-level repository format verification,
  and Git whitespace validation: passed.

## Iteration 13

Iteration 13 removes redundant or convenience-only source directives and replaces the embedded
7,377-line expression compiler with centrally versioned `FastExpressionCompiler` 5.4.1. Core,
sagas, and MessagePack are its only direct owners, and the repository guard forbids the embedded
source from returning.

The iteration also corrects confirmed false API documentation: 253 transport-neutral send/publish
contracts no longer promise broker acknowledgement, 96 relative delays and 40 absolute schedule
times are distinguished, receive-start handle returns are accurate, RabbitMQ documents both
publisher-confirmation branches, and the Amazon SQS renewal floor is identified as a library policy.

### Red/green and mutation evidence

- Baseline hygiene tests rejected the inherited directives, markers, and historical narrative.
- The syntax-aware guard found two residual Azure Service Bus schedule summaries after the first
  mechanical pass; both were corrected.
- A controlled nullable/TODO/false-contract mutation caused exactly three owner-test failures.
- Removing one compiler import caused the core build to fail at `CompileFast`.
- Every mutation was restored before final validation.

### Full validation

- Release Engineering build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,835 passed, 0 failed, 0 skipped.
- Fresh-package gate: 18 journeys, 30 packages, three isolated provider consumers, and all 29
  runtime package APIs passed.
- Packed API: unchanged at 24,000 lines and SHA-256
  `36a9b02c2417bfe12abf7be4858236cc23604afffa0fadb7fe38972217f510ec`.
- Warning-level format verification, requirement projections, and Git whitespace: passed.

Generic XML documentation, empty elements, signature-order mismatches, and remaining historical
narrative are still open and form the explicit next iteration; iteration 13 makes no final A+ claim.

## Iteration 49

Iteration 49 turns Azure Table into one coherent greenfield capability without removing saga,
Job Service, futures, Courier, or bounded message-journal behavior. Eleven intentional public types
now occupy `ViciOne.ServiceBus.Azure.Table`; all provider mechanics are internal and physically
grouped by responsibility. Composition uses one `UseAzureTable` vocabulary, options and formatter
contracts are explicit, and public concurrency failures retain provider error identity.

Every product file and both owning test projects were read before the source, XML documentation,
type names, namespaces, and physical layout were changed. No comment generator modified source.
The package has no convenience directives, maintenance markers, dummy implementations, obsolete
Azure SDK vocabulary, redundant capability directory, global-usings shim, JetBrains suppression,
or empty source directory.

### Red/green and mutation evidence

- The first complete post-remediation Azurite profile failed four Job Service cases and revealed a
  shared non-generic formatter registration; a type-specific formatter provider fixed the defect,
  and a two-saga DI test prevents recurrence.
- Seven isolated mutations were killed: omitted custom-key validation, disabled 412 mapping,
  broadened journal ownership, omitted constructor null validation, leaked implementation
  visibility, discarded saga-specific formatter binding, and disabled 409 save mapping.
- Every mutation was manually restored before a fresh sequential rebuild and final validation.

### Full validation

- Azure Table product, unit, local-integration, architecture, and complete Unit-solution builds:
  passed with 0 warnings and 0 errors.
- Azure Table unit profile: 69 passed, 0 failed, 0 skipped.
- Isolated real Azurite profile: 27 passed, 0 failed, 0 skipped; final run identity
  `vicione-b1776357b998`.
- Complete Unit/Architecture profile: 4,407 passed, 0 failed, 0 skipped.
- Architecture and documentation profile: 237 passed, 0 failed, 0 skipped.
- Fresh-package gate: 18 journeys, 30 packages, three executable isolated provider consumers, and
  all 29 runtime package APIs passed.
- Packed API: 21,719 lines and SHA-256
  `c0214b238c825361c24efd3f09af78e4e1ce94fbce2f9393d0791b47d2662dba`.
- Product/unit/local warning-level format verification and Git whitespace validation: passed.

## Iteration 50

Iteration 50 turns MessagePack into a self-contained serialization capability with an exact
two-type public API. Registration extensions and the advanced factory occupy the package root;
envelopes, bodies, serializer contexts, resolvers, and formatters are internal and live in matching
folders and namespaces. Hidden global imports, the namespace suppression, empty directories, and
optional Courier and Job Service product references are removed. Benchmarks use the public factory
instead of requiring friend access to implementation types.

Every product source and all owning tests were read before the code, comments, XML documentation,
namespaces, type visibility, and physical layout were changed. No source-comment generator was used.
Nested Job Service and Courier contracts retain their complete observed behavior through the generic
interface formatter. Mutable `ContentType` values no longer escape as shared process state, and lazy
resolver entries provide one formatter instance under concurrent first access.

### Red/green and mutation evidence

- The unchanged 60-test MessagePack profile was expanded to 68 tests covering the exact public API,
  null ownership, optional dependency boundary, production resolver selection, concurrent factory
  and formatter identity, independent content types, and nested Courier behavior.
- Seven isolated mutations were killed: an implementation visibility leak, a missing extension null
  guard, a shared mutable media type, an uncached interface formatter, trusted-data mode, a restored
  Courier product dependency, and a removed production ServiceBus resolver.
- Every mutation was manually restored before fresh sequential builds and final validation.

### Full validation

- MessagePack product, test, and benchmark Release builds: passed with 0 warnings and 0 errors.
- MessagePack profile: 68 passed, 0 failed, 0 skipped.
- Complete Unit/Architecture profile: 4,415 passed, 0 failed, 0 skipped.
- Architecture and documentation profile: 237 passed, 0 failed, 0 skipped.
- Fresh-package gate ran three times: 18 journeys, 30 packages, three executable isolated provider
  consumers, and all 29 runtime package APIs passed the update run and both comparison runs.
- Packed API: 21,659 lines and SHA-256
  `e74772a4a6f79f19054df5987fc24d685551688fc95c28ef4d019dd303860215`.
- Product, test, and benchmark format verification plus Git whitespace validation: passed.

## Iteration 77

Iteration 77 completes the bounded manual review of all 34 core caching and request-client source
files, their public contracts and consumers, and all directly owning tests. No generator authored
production code or comments. Cache disposal and synchronous index projection now share one active
lifetime, expiration observation is mode-specific, client-factory ownership is publicly
asynchronous and idempotent, request deadlines remain absolute across endpoint work, and terminal
cleanup is independent of an ambient synchronization context.

Client internals now live in responsibility-matching `Contexts`, `Endpoints`, and `Requests`
folders and namespaces. Exact architecture tests reject the former flat layout. Boundary tests
cover required contexts, addresses, messages, initializers, wrapper calls, readiness, disposal,
metadata, and forwarding before unintended dependency work.

### Red/green and mutation evidence

- The focused profile grew from 145 to 190 tests.
- Seventeen isolated mutations were killed across cache races, expiration observation, request
  boundaries, async lifetime, deadlines, TTL, synchronization scheduling, physical layout,
  readiness, disposal, cache forwarding, response forwarding, consume-pipe options, host metadata,
  and public API interface extraction.
- Every mutation was restored before fresh final validation.
- The API work exposed and fixed a baseline blind spot: direct interface relationships are now
  emitted and exactly guarded. Two independent package runs produced the same API hash.

### Full validation

- Complete core profile: 2,721 passed, 0 failed, 0 skipped.
- Complete architecture profile: 260 passed, 0 failed, 0 skipped.
- Bounded coverage: 92.46% line, 86.92% branch, no method above CRAP 30.
- Release solution build: 0 warnings and 0 errors.
- Fresh-package gate: 18 journeys, 31 packages, three isolated provider consumers, and all 30
  runtime API contracts passed.
- Packed API: 19,961 lines and SHA-256
  `7a63fd620a3dedc925a4a3409d419905171388458a0ca78ef482e2579466fb7a`.
- Locked restore, vulnerability inventory, format, JSON, preprocessor, empty-directory, and Git
  whitespace gates passed.

The repository-wide source goal remains active. The two global compatibility-named findings outside
this iteration are retained for their owning manual source reviews rather than being changed without
complete context.

## Iteration 92

StateMachineVisualizer remediation is complete from the remotely secured iteration-91 commit
`c98fc82eb12242770085d55a97bbdde1f30f082b`. All product files, comments, owning tests, requirements,
dependencies, API, filenames, namespaces, and physical placement were manually reviewed. The
package now owns deterministic Graphviz and Mermaid serialization without QuikGraph, retains its
exact two-type synchronous public API and all graph features, and safely renders every label using
canonical LF output.

The focused profile passes 29/29 at 100% package line and branch coverage; six isolated mutations
were killed and restored. The full Engineering build has zero warnings and errors, the complete
Unit/Architecture solution passes 5,955/5,955 with no skips, and Architecture passes 292/292.
Format, locked restore, JSON, preprocessor, empty-directory, dummy-marker, package-security, and
whitespace gates pass. Two fresh-package runs validate 18 journeys, 31 packages, three isolated
provider-testing consumers, and all 30 runtime APIs. The API contract remains 19,104 lines with
SHA-256 `34c7a90ef04451531e03134e0891e752a410996742627d4648941427f04aee27`.

All available direct stable dependency updates are applied, the Microsoft 10.0 family is coherent
at 10.0.12, and isolated consumer pins and locks match. Current online inventories contain no
outdated direct, known-vulnerable direct/transitive, or deprecated direct/transitive packages. The
iteration is ready for commit, annotated tag, normal remote push, and final remote verification.

## Iteration 96

Iteration 96 completes the bounded manual A+ review of the analyzer, code-fix, and analyzer-package
owner. Every original production file and every comment was read and checked against current
behavior without generator-authored source or documentation. The final public surface consists of
eight diagnostic analyzers and two code-fix providers; shared symbol mechanics are internal and
physically aligned, and legacy NuGet install/uninstall scripts plus the obsolete nullability
polyfill are gone.

Producer recognition, unobserved-task analysis, cancellation forwarding, recursive message
compatibility, `MessageData<T>`, header validation, consumer synchronization, configuration writes,
diagnostic metadata, concurrency safety, and package layout now have direct adversarial coverage.
The final manual reread caught and fixed private-getter serialization and timed `TryEnter` gaps
through red-first tests. Eight additional isolated mutations were killed and restored; two surviving
equivalent receiver mutations led to removal of redundant source rather than a false mutation claim.

### Full validation

- Analyzer tests: 164 passed, 0 failed, 0 skipped; CodeFix tests: 36 passed, 0 failed, 0 skipped.
- Analyzer coverage: 95.5538% line, 85.4072% branch, complexity 905, 174 methods, no CRAP score
  above 30.
- CodeFix coverage: 94.4915% line, 71.9697% branch, complexity 138, 29 methods; the sole CRAP 32
  carrier is a fully line-covered compiler-generated async state machine.
- Complete serial Unit/Architecture solution: 6,214 passed, 0 failed, 0 skipped.
- Complete serial Engineering Release build: all 77 projects, 0 warnings, 0 errors.
- Both full Roslyn format gates, locked restores, requirements JSON, preprocessor, dummy-marker,
  empty-directory, package-layout, and Git whitespace checks: passed.
- Fresh-package gate: 18 journeys, exactly 31 packages, three executed isolated provider-testing
  consumers, and all 30 runtime API assemblies match the 19,083-line contract at SHA-256
  `1e8055f4700954d11ab7cadd4be01251e01fa17fef1702366df7b9d10eb8c843`.

The final package uses the standard Roslyn `analyzers/dotnet/cs` layout and contains no legacy tools
scripts. `src/ViciOne.ServiceBus` remains the Core assembly owner; sibling capability assemblies and
the `Persistence`, `Scheduling`, and `Transports` provider groups are intentional and pass the
repository architecture and consumer gates. The protected `review/` and `TestResults/` trees were
not modified or staged. The complete source-wide A+ goal remains active after this owner is secured.

## Iteration 97

Iteration 97 completes the Saga runtime/capability remediation after a manual read of all 357
baseline product files and comments. The dispatch repository now exposes only dispatch, explicit
loadable/queryable repositories expose the additional operations they actually implement, and the
temporary, unsupported, and no-op repository fallbacks are removed. Saga registration fails closed
without an explicit persistence provider. Azure Table, DynamoDB, Entity Framework, in-memory, and
test-harness composition use the resulting capability model without feature loss.

Missing-instance redelivery now performs real scheduled delivery with preserved metadata and an
observable retry lifecycle. Faulted scheduling and state-machine execution preserve cancellation
identity through completion checks, dispatch, transitions, observers, nested scheduling, and
telemetry cleanup. Nineteen exact requirement cases were added; five isolated mutations were killed
and restored. The Core host passes 3,275 tests, and fresh Saga instrumentation records 62.4669% line
and 54.4440% branch coverage, reducing methods above CRAP 30 from 18 to 15.

The final serial Unit/Architecture solution passes 6,233/6,233 with no failures or skips, including
292 architecture cases. The Engineering Release build has zero warnings and errors. Package
validation passes 18 journeys, 31 fresh packages, three isolated provider consumers, and all 30
runtime API contracts; the 19,029-line contract SHA-256 is
`6870002dc25251fe785d4e0bbd51a0f66c15ce533a3be92beb78224a2fa28486`.

Whitespace, requirements, preprocessor, dummy-marker, empty-directory, and changed-test quality
checks pass. A non-mutating info-level style audit also identifies 47 historical unprefixed Saga
interface names as a separate Greenfield API decision; that bounded naming iteration remains next.
The physical project placement is accepted: Core owns only `src/ViciOne.ServiceBus`, independent
capabilities are sibling assemblies, and external providers are grouped under `Persistence`,
`Scheduling`, and `Transports`. The overall source-wide A+ goal remains active.

## Iteration 98

Iteration 98 completes the Saga interface and documentation normalization. All 47 Saga interfaces,
including three nested internal contracts, now use the .NET `I` prefix; 30 top-level filenames
match their primary types. Generic arity, variance, inheritance, members, attributes, concrete
implementations, and behavior are preserved across source, providers, tests, samples, analyzers,
reflection identities, and isolated consumers. A public API multiset audit found no unrelated
contract delta. Every affected declaration comment was manually reread and corrected.

The red-first `SagaInterfaces_UseTheDotNetInterfacePrefix` requirement originally reported exactly
47 violations and now passes. A deliberate `ICorrelatedBy` mutation was killed and restored. The
final Engineering Release build passes all 77 projects with zero warnings or errors. The complete
Unit/Architecture solution passes 6,234/6,234 with no skips; the direct architecture host passes
293/293, and the Core host passes 3,275/3,275. Both format gates, all requirements JSON, diff
whitespace, source hygiene, 18 developer journeys, 31 fresh packages, three isolated provider
consumers, and all 30 runtime API contracts pass.

Fresh Core-host coverage is 75.3365% line and 68.0573% branch. Saga coverage remains 62.4669% line
and 54.4440% branch; 15 of 2,523 methods exceed CRAP 30. These risks remain visible for subsequent
test strengthening. The packed 19,029-line API contract has SHA-256
`1a4fdef247c3b4ece1e5b8fed35dbe533f8409541a4aedf3be2dce9be8891be6`.

The source layout is deliberately ownership-based: `src/ViciOne.ServiceBus` is Core, independent
capabilities remain sibling packages, and cohesive external integrations are grouped below
`Persistence`, `Scheduling`, and `Transports`. The protected `review/` and `TestResults/` trees were
not changed or staged. The overall A+ goal remains active for the remaining source owners and final
completion audit.

## Iteration 99

Iteration 99 completes the eight-file `ViciOne.ServiceBus.Initializers` owner after a manual read of
every production file and comment. Its independent capability-project placement is retained. An
explicit `ViciOne.ServiceBus` root namespace and `Advanced`/`Initializers` source folders now mirror
its public namespace branches without nesting sibling projects below the Core project.

The two private one-implementation cache interfaces are replaced by sealed context types, the ID
variable's copied timestamp local name is corrected, and timestamp capture now has a deterministic
`TimeProvider` overload. Three new requirement-mapped tests cover default UTC capture, supplied and
missing clocks, and per-context sharing for both explicit variable kinds. Two red-first architecture
rules enforce .NET interface naming and namespace-aligned navigation.

The direct assertion audit finds no shallow or assertion-free case. One initially surviving
timestamp mutation exposed and produced the clock test; after remediation, four of four meaningful
isolated mutations are killed and restored. Fresh full-host coverage passes 3,278 tests, with
Initializers at 100% line and branch coverage. Overall host coverage is 75.3322% line and 68.0491%
branch.

The final Engineering Release build passes all 77 projects with zero warnings and errors. The
complete Unit/Architecture solution passes 6,239/6,239 with no skips, including 295 architecture
cases. Both full format gates, requirements JSON, source hygiene, empty-directory, and Git
whitespace checks pass. Package validation passes 18 journeys, 31 freshly packed packages, three
isolated provider-testing consumers, and all 30 runtime APIs. The intentional 19,030-line packed
contract has SHA-256 `a31b98d00ab15941a47bef08aa05447a24db4e445aba00d0838a0686ca85aa0a`.

The protected `review/` and `TestResults/` trees remain unchanged and unstaged. The overall A+ goal
remains active for the remaining complete source owners and the final repository-wide audit.

## Iteration 100

Iteration 100 completes the remaining Futures interface normalization and aligns the project tree
with its namespaces. `Get<TFuture>` is now `IGet<TFuture>` with no compatibility alias; its
correlation inheritance, generic constraint, public event consumer, and durable-result feature are
preserved. `ViciOne.ServiceBus.Futures` remains an independent capability project beside the Core
`ViciOne.ServiceBus` project. Within it, `Configuration/` and `Futures/` now reflect the existing
namespace branches relative to the explicit `ViciOne.ServiceBus` root namespace.

Two red-first architecture rules enforce the interface and folder decisions. A deliberate removal
of correlated identity fails six required compile sites. Full-suite execution additionally exposed
and closed one asynchronous observer race in the test harness and one stale hard-coded architecture
path after the move.

The Engineering Release build passes all 77 projects with zero warnings or errors. The complete
Unit/Architecture solution passes 6,241/6,241 with no skips. Fresh coverage is 75.3393% line and
68.0696% branch overall; Futures is 90.4990% line and 85.4839% branch. Both full format gates,
requirements, source hygiene, API identity/path scans, and Git whitespace pass. Package validation
passes 18 journeys, 31 fresh packages, three isolated provider-testing consumers, and all 30 runtime
APIs. The 19,030-line packed API contract has SHA-256
`a96d93cc091d97174baceb59fe5230228734c2b98a676431521e70a329cce9fa`.

The protected `review/` and `TestResults/` trees remain unchanged and unstaged. The overall A+ goal
remains active for the remaining complete source owners and final repository-wide audit.

## Iteration 101

Iteration 101 completes the JobService Greenfield interface and source-navigation normalization.
All 45 public and internal interfaces now use the .NET `I` prefix, their filenames match their
types, and no legacy compatibility alias remains. The independent JobService project stays beside
Core; inside it, all 170 production files now mirror their namespaces relative to an explicit
`ViciOne.ServiceBus` root. JobService-specific exceptions reside under `JobService/`, while only
project infrastructure remains at the project root.

Two red-first architecture rules enforce interface naming and path alignment. The complete host
also exposed and closed an existing root-layout violation and a primary-constructor parameter gap
in the public-documentation rule. The two new tests have two meaningful collection assertions and
no quality smell; four of four substantive observed counterchanges are killed.

The Engineering Release build passes all 77 projects with zero warnings and errors. The complete
Unit/Architecture solution passes 6,243/6,243 without failure or skip, including 299 architecture
tests. Fresh coverage is 75.3255% line and 68.0424% branch overall; JobService is 95.6189% line and
89.7257% branch. Both full format gates, analyzer, JSON, whitespace, source hygiene, interface,
folder, and empty-directory checks pass. Package validation passes 18 journeys, 31 freshly packed
packages, three isolated provider-testing consumers, and all 30 runtime APIs. The 19,030-line
contract SHA-256 is `f12d21461b1d4403c5ebed180f1c00d43a9a24b672745e0d3739452600091423`.

The protected `review/` and `TestResults/` trees remain unchanged and unstaged. The overall A+ goal
continues with the remaining complete source owners and the final repository-wide audit.

## Iteration 102

Iteration 102 completes Courier interface naming and namespace-relative navigation. Sixteen
interfaces and their filenames now use the .NET `I` prefix with no aliases; Courier remains an
independent sibling capability and its 137 production files now follow the explicit
`ViciOne.ServiceBus` root namespace. Dead registration overloads are removed, activity scanning is
split by responsibility, and generic plus runtime execute-only registration rejects compensatable
activities. Both guards kill a controlled mutation.

The Engineering build passes 77 projects with zero warnings/errors. Both full format gates make
zero changes. All 23 native hermetic hosts pass 6,250/6,250 without failure or skip, including 301
architecture and 3,283 Core tests. Core-host coverage is 78.3472% line and 70.6344% branch; Courier
is 88.6212% line and 75.3304% branch with zero of 617 methods above CRAP 30. Package/API validation
passes 18 journeys, 31 packages, three provider-testing consumers, and 30 runtime APIs; the
19,030-line contract SHA-256 is
`b81db7838a57f4205d2c10687643a8ce8853f85c6de4a96b6a07f843631b7d51`.

The protected `review/` and `TestResults/` trees remain unchanged and unstaged. The overall A+ goal
continues with the remaining source owners and the final repository-wide audit.

## Iteration 103

Iteration 103 completes the manual 28-file Mediator owner review. Mediator remains an independent
`src` sibling while its project-internal folders now mirror the explicit `ViciOne.ServiceBus` root
namespace. Required DI callbacks, non-null explicit base addresses, and one fluent limits convention
close the three public Greenfield API gaps. Ninety-one focused Mediator tests and two new permanent
architecture rules protect the behavior and navigation model.

The 77-project Engineering build and both format gates pass cleanly. All 23 native hosts pass
6,260/6,260 without failure or skip. Fresh Core-host coverage is 75.4858% line and 68.1517% branch;
Mediator is 90.7182% line and 77.5974% branch with no method above CRAP 30. Package/API validation
passes twice; the 19,030-line packed contract SHA-256 is
`9f0d543184d729768ba0606420ca05d005c6e1bd1961bfeda472600d18985345`. Protected trees remain
unchanged and unstaged. The overall A+ goal remains active for the remaining source owners and final
repository-wide audit.

## Iteration 106

Iteration 106 completes the Core dependency-injection configuration owner. All 66 production files
and comments were read manually. Core configuration has one physical owner under
`Configuration/DependencyInjection`, its advanced facade is under `Advanced/Registration`, and
actual runtime container code remains under `DependencyInjection`. Independent assemblies remain
siblings under `src`; persistence, scheduling, and transport providers remain grouped by adapter
family.

Empty, no-op, duplicated, throwing-placeholder, and unsafe removal contracts were eliminated
without feature loss. Registration identity, validation, lifetime, endpoint planning, rider
completion, filter selection, request defaults, factory results, transport specifications, and
consumer-kind ownership now have explicit behavior. Thirty-three dependency-injection contract
methods provide 34 cases, all 16 handler overloads are guarded, and permanent architecture tests
enforce the physical and public boundaries. Direct assertion and smell audits found no shallow,
assertion-free, skipped, random, sleeping, or swallowed-exception test.

One red-first open-generic case found a production defect. Six controlled counterchanges were killed
and restored byte-for-byte. A full-run-only scheduled-publish test ambiguity was also corrected to
assert delivered messages rather than generic task completion. The final 77-project Release build
has zero warnings and errors, both format gates pass, and all 23 test hosts pass 6,317/6,317 with no
skip. Owner coverage is 82.7847% line and 76.4354% branch over 539 methods, with zero CRAP scores
above 30 and a maximum of 29.0179. The coverage SHA-256 is
`f61d55f3c854c5aca80d392b9db98721d8a54fc746ac3b8d06e797bf8c25053c`.

Package validation passes 18 journeys, 31 packages, three isolated provider consumers, and all 30
runtime APIs. The 18,879-line packed API SHA-256 is
`09218528f7e3f0b9c54ea3142587fa165c28ad017e590a7b042b6efcea9b076e`. Source hygiene and protected
trees pass. The overall A+ goal continues with the remaining source owners and final whole-repository
audit.

## Iteration 107

Iteration 107 completes the manual 80-file Core Advanced owner review. The directory model is now
explicit: `src/ViciOne.ServiceBus` is the Core project, independent assemblies remain sibling
projects, integration adapters remain grouped beneath Persistence, Scheduling, and Transports, and
Advanced's internal directories mirror real namespace and API ownership.

`TransactionContext` is now the convention-correct `ITransactionContext` without a compatibility
alias. Nullable dispatcher flow, cancellation-token identity, supervisor completion, retained log
contexts, abstract JSON mappings, diagnostic Unicode handling, and convention documentation are
corrected and directly tested. All 50 changed or added tests have meaningful assertions and no
identified test smell. Nine of nine controlled counterchanges were killed; one exposed a real
supervisor cancellation race, whose fix passed 20 isolated repetitions.

Fresh Advanced coverage passes 3,382 tests at 98.7% line and 91.1% branch coverage over 373 methods,
with zero CRAP scores above 30. The 77-project Engineering build has zero warnings and errors, both
format gates pass, and all 23 native test hosts pass 6,355/6,355 with no skip. Package/API validation
passes twice with 18 journeys, 31 packages, three provider-testing consumers, and 30 runtime APIs;
the intentional 18,879-line contract SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional async naming, comments, directives, dummy and legacy identities, SDK
pinning, empty directories, formatting, and Git whitespace pass. Protected trees remain unchanged
and unstaged. The overall A+ goal continues with the remaining source owners and final repository
audit.

## Iteration 108

Iteration 108 completes the manual review of all eight Core Batching files and 1,150 final source
lines. The existing `Contexts/` and `Runtime/` folders correctly mirror their namespaces inside the
Core project; Batching is not an external provider project.

Admission now becomes terminal when timer scheduling throws or returns `false`, clears retained
messages, stops registrations, and propagates the same failure to every owned pipeline. Primary and
distinct cleanup failures are preserved. Timer and cancellation callbacks use observed asynchronous
executor operations instead of self-blocking queue admission, and equal ordering keys retain
monotonic admission order. Direct tests also close all timestamp fallback and collector lifetime
contracts.

Nine new requirement-mapped tests pass; all changed tests have causal assertions and no identified
quality smell. Six of six isolated counterchanges were killed and restored. The focused Batching
suite passes 79/79. Final Core coverage passes 3,391/3,391 and records Batching at 96.2% line
(430/447) and 91.6% branch (174/190), across 80 methods with zero CRAP scores above 30. The accepted
Cobertura artifact is `/private/tmp/vsb-iteration108-final.cobertura.xml`, SHA-256
`a83efb60a9d774fa9879689c7fbece53f4eddc011df5bb7606e3ac6b9ee79b4b`.

The bidirectional Async gate exposed and then confirmed the private
`TerminateFailedAdmissionAsync` identity. A full-suite-only scheduling observation race was traced
to mismatched 30-second operation and 1.2-second inactivity policies; aligning the harness policy
made the isolated and final complete runs deterministic without product-code changes.

The final Engineering build passes all 77 projects with zero warnings and errors. Both format gates
pass, and all 23 hermetic Unit/Architecture hosts pass 6,364/6,364 with zero failures and skips.
Package validation passes 18 developer journeys, 31 fresh packages, three isolated provider-testing
consumers, and all 30 runtime API assemblies. The unchanged 18,879-line contract SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements JSON, Git whitespace, async naming, comments, directives, dummy markers, SDK pinning,
and empty-directory checks pass. The protected `review/` and `TestResults/` trees remain unchanged
and unstaged. The overall A+ goal continues with the remaining source owners and final whole-source
completion audit.

## Iteration 109

Iteration 109 completes the manual review of all 17 Core Caching files and the final 1,701 source
lines. `Caching/Implementation` is a coherent non-public namespace inside the Core project;
independent assemblies stay as `src` siblings and provider projects stay grouped under
`Persistence`, `Scheduling`, and `Transports`.

A red-first lifecycle case found and corrected a partially registered usage-event callback leak.
Five new tests and one strengthened assertion cover compensation, direct-add capacity backpressure,
caller cancellation and ownership, synchronous disposal, null keys, empty hit ratio, and canceled
clear invalidation. Three of three controlled counterchanges were killed and fully restored. The
focused suite passes 105/105. Fresh Caching coverage is 94.31% line and 90.23% branch across 97
methods with zero CRAP scores above 30. The artifact SHA-256 is
`16ea8775fb8206dcdeb4895df17568f5324391e8804363fd2c6cd70802741e20`.

The final Engineering build passes 77 projects with zero warnings and errors. Both format gates
pass. All 23 hermetic hosts pass 6,369/6,369, and the final Core host passes 3,396/3,396. Package/API
verification passes 18 journeys, 31 packages, three isolated provider consumers, and all 30
runtime APIs; the 18,879-line contract SHA-256 remains
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Two redundant test-side nullable directives were removed; the only directive-shaped text left is
an intentional Roslyn fixture. Source hygiene, requirements, empty directories, and Git whitespace
pass. Protected trees remain unchanged and unstaged. The overall A+ goal remains active.

## Iteration 110

Iteration 110 completes the manual review of all 17 Core Clients files and 1,805 initial source
lines. `src/ViciOne.ServiceBus` remains the physical Core project rather than a container for sibling
assemblies. Its `Clients/Contexts`, `Clients/Endpoints`, and `Clients/Requests` paths match their
namespace and runtime owners; independent assemblies remain direct `src` children and external
providers remain grouped under `Persistence`, `Scheduling`, and `Transports`.

Request completion now has exactly one synchronized terminal owner. Fault observation is connected
before sending, only the first response branch succeeds, and null connection handles or sent
messages fail at their provider boundary. Complete direct and scoped factory overload matrices,
factory-disposal failure sharing, deadline and timer invariants, and initialized multi-response
forms have exact tests.

Thirteen requirement projections add 30 focused cases; Clients passes 123/123. Seven controlled
counterchanges were killed and restored, and three null-provider cases were red before correction.
Fresh owner coverage is 98.9831% line and 89.6739% branch over 137 methods with zero CRAP scores
above 30. The accepted Core coverage run passes 3,426/3,426, artifact SHA-256
`3fe785da97559080e7eef14bc4dab1f155ae8ce155c15b8423af847655d83ce6`.

The final Engineering build passes 77 projects with zero warnings and errors. Both format gates
pass. All 23 hermetic hosts pass 6,399/6,399 with no failure or skip. Package/API verification passes
18 journeys, 31 packages, three isolated provider consumers, and 30 runtime APIs; the unchanged
18,879-line API SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Async naming, source and project architecture, requirements, directives, dummy and legacy markers,
SDK pinning, empty directories, formatting, and Git whitespace are clean. Protected trees remain
unchanged and unstaged. The overall A+ goal remains active.

## Iteration 111

Iteration 111 completes the manual review of all ten final Core Consumers files and 539 source lines.
`src/ViciOne.ServiceBus/Consumers` remains one internal Core capability; `Contexts/`, `Conventions/`,
and `Metadata/` match their namespaces and runtime ownership.

Owned consumer factories now preserve exact operation and release failures, including ordered dual
failures. Invalid convention-provider results fail at their owning boundary, while descriptor order,
later-convention replacement, no-op version identity, registration exclusion, probe identity, and
context null boundaries have direct tests.

Eight requirement projections add 17 focused cases; Consumers passes 136/136. Seven cases were red
before product correction and three isolated counterchanges were killed and restored. Fresh owner
coverage is 100% line and branch over 34 methods with no CRAP score above 30. The complete Core
coverage run passes 3,443/3,443 and has SHA-256
`91dde737706e9d409aac01328c607314b0b57ee6c0decf31458188be462abeff`.

Both format gates pass. The 77-project Release build has zero warnings and errors. All 23 hermetic
hosts pass 6,416/6,416 with no failure or skip. Package/API verification passes 18 journeys, 31
packages, three isolated provider consumers, and 30 runtime APIs; the unchanged 18,879-line API
SHA-256 is `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, Async and source architecture, directives, dummy and legacy markers, SDK pinning,
empty directories, formatting, and Git whitespace are clean. Protected trees remain unchanged and
unstaged. The overall A+ goal remains active.

## Iteration 112

Iteration 112 completes the manual review of all 15 Core Context files and 2,181 final source lines,
including every source comment. `Activities/` and `Consumption/` correctly group internal Core
responsibilities beneath `src/ViciOne.ServiceBus` while retaining the concise public
`ViciOne.ServiceBus.Context` namespace. Independent assemblies remain sibling projects; provider
integrations remain grouped under `Persistence/`, `Scheduling/`, and `Transports/`.

Response task ownership, synchronous endpoint-resolution behavior, endpoint-provider validity, and
proxy typed-lookup validity are corrected. Complete projection, payload, notification,
deserialization, scope, response-shape, fault, observer, and parameter contracts now have direct
tests. The focused profile grows from 35 to 96 cases; original red evidence confirms all four defect
families and three controlled counterchanges were killed and restored.

Fresh Context coverage is 100% executable lines (527/527) and 93.75% branches (120/128), over 294
methods with maximum CRAP 6. The focused artifact SHA-256 is
`f8d8c82b050dc8003ca7411080c64299a05a991cc8df689189b6a31f04e5cd92`. Complete Core coverage
passes 3,504/3,504 with SHA-256
`51ad6d890e9c31ce7652c931f77fefbae7c0c0aeef58edeef33a44729d247820`.

Both format gates pass. The 77-project Release build has zero warnings and errors. All 23 hermetic
hosts pass 6,477/6,477 with no failure or skip. Package/API verification passes 18 journeys, 31
packages, three isolated provider consumers, and all 30 runtime APIs; the unchanged 18,879-line API
SHA-256 is `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, Async naming, source/comment/directive/file/folder architecture, dummy and legacy
markers, CLI SDK pinning, empty directories, formatting, and Git whitespace are clean. The explicit
`net10.0` product target remains intentional. Protected trees remain unchanged and unstaged. The
overall A+ goal remains active.

## Iteration 113

Iteration 113 completes the manual review of the full 66-file, 3,846-line built-in InMemory owner,
including every source comment. The process-local implementation remains inside the Core project;
its public contracts and selection API use matching Core namespaces. Independent provider
assemblies remain siblings grouped beneath `Persistence`, `Scheduling`, and `Transports`.

Address parsing and reconstruction are canonical and boundary-safe, endpoint addresses follow the
final configured host, exchange types fail at configuration, moved messages retain complete MIME
metadata, and durable dispatch rejects every invalid catalog or endpoint result at its owner. Direct
tests also close custom-address and delay-provider ownership, public binding and callback APIs,
logical-delay boundaries, runtime fabric identity, publish discovery, and all new parameters.

Eighteen requirement projections add 24 focused cases; InMemory passes 104/104. Seven simultaneous
counterchanges caused exactly 15 expected failures with 34 unrelated passes and were fully restored.
Focused owner coverage is 90.4889% line and 76.0101% branch; the complete Core run passes 3,528/3,528
and raises owner coverage to 96.4444% line and 78.7879% branch. Across 272 methods, maximum CRAP is
18 and none exceeds 30. The final artifact SHA-256 values are
`5388c5139c95c229dc00315fa7a8ca902085fdf75bc36537446a3c3409176de8` focused and
`b0f878be0ebb78f4ad4c48126e78fde891ef751fc8996a59b634a8d1302ed7a9` complete Core.

Both format gates pass. The 77-project Release build has zero warnings and errors. All 23 hermetic
hosts pass 6,501/6,501, and the separate architecture host passes 307/307. Package/API validation
passes 18 journeys, 31 packages, three isolated provider consumers, and all 30 runtime APIs; the
18,879-line API SHA-256 remains
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional Async naming, comments, directives, filenames, folders, namespaces,
dummy and legacy markers, SDK pinning, empty directories, formatting, and Git whitespace pass.
`global.json` selects only Microsoft Testing Platform and contains no SDK version. Protected trees
remain unchanged and unstaged. The overall A+ goal remains active.

## Iteration 114

Iteration 114 completes the manual review of all 87 Core Initializers production files and 7,593
final source lines plus all 46 direct test files and 7,452 test lines. Their comments, filenames,
namespaces, folders, and Core assembly ownership are coherent. Independent assemblies remain direct
siblings under `src`; optional integrations remain grouped under `Persistence`, `Scheduling`, and
`Transports`; no product C# file exists directly in the repository `src` root.

Null property and header initializer tasks now fail explicitly at their owning boundary, and header
failure prevents downstream dispatch. All convention metadata entry points have exact contract
coverage. Converter discovery is decomposed without changing enum, nullable, named-value, or
registered-converter behavior. The stale DateTime converter description is corrected. Two red-first
cases and an isolated counterchange prove the null-task behavior; the repository Async gate also
caught and drove correction of the new private helper's name.

Initializers passes 181/181 focused tests with 97.6589% line and 90.3448% branch coverage. Complete
Core coverage passes 3,531/3,531 and reaches 97.7007% owner line and 90.4310% owner branch coverage.
Across 494 owner methods, maximum CRAP is 28 and none exceeds 30. Final coverage artifact SHA-256
values are `621c6186e8f9e4412d4bdfa2a33395a710cf697f5aee62947790454be4233509` focused and
`0402d07f5a2dfe26c6e63875e835575611ea4e7d55d552b13e0089b33e1e4b40` complete Core.

Both format gates pass. The serial Engineering Release build passes all 77 projects with zero
warnings and errors. All 23 hermetic hosts pass 6,504/6,504 with no failure or skip. Package/API
validation passes 18 journeys, 31 packages, three isolated provider-testing consumers, and all 30
runtime APIs; the 18,879-line public API SHA-256 remains
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional Async naming, comments, directives, filenames, folders, namespaces,
dummy and legacy markers, SDK pinning, empty directories, formatting, and Git whitespace pass.
Protected `review/` and `TestResults/` remain unchanged and unstaged. The overall A+ goal remains
active.
