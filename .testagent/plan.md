# A+ remediation test plan

## Iteration 1 outcome

Eliminate confirmed persistence defects, silent configuration contracts, discarded cancellation tokens, mutable process-global identifier configuration, and the unimplemented Azure message-session query path without losing supported behavior.

## Requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-APlus-RUNTIME-001` | EF abandoned inbox entry stores the exact supplied UTC instant and remains terminal after a fresh context/store | EF reliable-store tests | SQLite provider behavior plus restart query |
| `REQ-APlus-CONFIG-001` | Receiver wrapper forwards topology flags, dependencies, dependents, and generic/runtime message topology changes | core configuration tests | Direct state and readiness/completion observations |
| `REQ-APlus-CONFIG-002` | In-memory `AutoStart` changes the bus endpoint configuration | in-memory configuration tests | Both boolean values observed through the built configuration |
| `REQ-APlus-CONFIG-003` | Endpoint registration inclusion is mutable only through the authoritative registration owner | dependency-injection tests | True/false round trip and downstream selection behavior |
| `REQ-APlus-API-001` | Composite filter collections are get-only and matching remains behaviorally correct | abstraction/core tests plus architecture test | Compile-time/reflection shape and include/exclude truth table |
| `REQ-APlus-ASB-001` | Message-session saga query correlation evaluates the current persisted session state and forwards matching queries | Azure Service Bus tests | Matching, non-matching, identity, count, and cancellation behavior |
| `REQ-APlus-ASB-002` | Message-session saga writes propagate the caller cancellation token | Azure Service Bus tests | Exact token identity for save and update operations |
| `REQ-APlus-JOB-001` | Job lifecycle notifications and progress propagation preserve caller cancellation | job-service tests | Exact token identity at provider, send, and progress-buffer boundaries |
| `REQ-APlus-NEWID-001` | The static `NewId` façade is immutable after startup and remains unique under parallel generation | abstractions tests | Public API-shape audit and 100,000-value parallel uniqueness run |

## Test partitions

- Positive: each supported configuration affects its runtime owner.
- Negative: null arguments, pre-cancelled tasks, and unsupported capabilities fail with the narrowest stable exception.
- Boundary: true/false flags, empty/non-empty filter predicates, exact timestamps including non-UTC offsets.
- Persistence: reload with a new EF context and a newly created reliable store.
- Composition: validation happens before a receive pipeline handles a message.
- Compatibility: no alias or obsolete member is added; supported feature behavior remains available.

## Mutation obligations

- Remove the EF `CompletedAt` assignment: the exact timestamp/restart assertion must fail.
- Replace each forwarding setter/method with a no-op: its direct behavior test must fail.
- Reintroduce a composite-filter setter or invert include/exclude semantics: API-shape or truth-table tests must fail.
- Replace Azure query predicate evaluation with an unconditional match: the non-matching and count assertions must fail.
- Drop the Azure state-write or job notification cancellation token: the exact token-identity tests must fail.
- Reintroduce a public process-global `NewId` mutator: the façade API-shape test must fail.

## Execution order

1. Add failing tests against the baseline for each confirmed defect.
2. Implement the smallest coherent behavior correction.
3. Run focused owner projects.
4. Run test-gap, assertion-quality, and anti-pattern checks for changed tests.
5. Execute one-cause mutations and restore byte-for-byte.
6. Run clean Release builds, the complete Unit/Architecture profile, relevant provider profiles, format/static gates, and zero-test/skip guards.
7. Record evidence, commit, tag, push, and verify remote hashes.

## Later iterations

Subsequent plans will cover application options and parameter completeness; member-level public API baselines and layering; source navigation and package ownership; XML documentation/comment/directive cleanup; then full coverage and final multidimensional re-review.

## Iteration 2 outcome

Make SQL URI materialization fail explicitly at invalid database boundaries and replace duplicated 30-bit topology-name suffixes with one deterministic 65-bit shortening policy that preserves provider length budgets.

## Iteration 2 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SQL-URI-MATERIALIZATION` | Both SQL providers preserve valid absolute/relative values and reject null, database null, blank, malformed, and non-string values | SQL transport tests | Identical result/exception contracts for both handlers |
| `REQ-VSB-TOPOLOGY-NAME` | Bounded temporary names use the full budget, retain a readable prefix, and carry a 13-character hash | core topology tests | Exact length, separator position, alphabet, and invalid-budget guard |
| `REQ-VSB-ASB-SUBSCRIPTION-NAME` | Long names are deterministic, provider-bounded, and collision-resistant; missing names are rejected | Azure Service Bus tests | Exact shape, repeated-input stability, different-input separation, and 10,000-name collision set |

Iteration 2 mutation obligations reject relative SQL values independently in each provider, reduce the shared suffix to six characters, bypass missing-name validation, and remove the minimum-budget guard. Each owning test must fail before the original corrected source is restored byte-for-byte.

## Iteration 3 outcome

Prove every application-level outgoing-options field at the shared context boundary, fail explicitly when a requested partition key cannot be represented by the selected transport, and make both reliable-messaging query surfaces validate and forward their complete contracts consistently.

## Iteration 3 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-APPLICATION-SEND-OPTIONS` | Headers, lifetime, all four identifiers, and partition key reach a send context exactly | abstractions tests | Direct public entry call plus independent field assertions at the context boundary |
| `REQ-VSB-APPLICATION-PUBLISH-OPTIONS` | Headers, lifetime, all four identifiers, and partition key reach a publish context exactly | abstractions tests | Direct public entry call plus independent field assertions at the context boundary |
| `REQ-VSB-APPLICATION-SCHEDULE-OPTIONS` | Destination, due time, message, options, and cancellation reach the advanced scheduler exactly | core scheduling tests | Public application API with a recording schedule provider |
| `REQ-VSB-APPLICATION-REQUEST-OPTIONS` | Metadata reaches the request envelope and an absolute deadline is derived from the injected clock | core client tests | In-memory request boundary with exact metadata and deadline assertions |
| `REQ-VSB-APPLICATION-OPTIONS-CAPABILITY` | An explicitly requested partition key is never silently discarded | abstractions tests | Unsupported context fails with a stable `NotSupportedException` |
| `REQ-VSB-RELIABLE-OPERATIONS-QUERY` | Snapshot, outbox page, and inbox page validate inputs and preserve exact cancellation | core reliable-messaging tests | Recording stores plus behavior against the in-memory store |

## Iteration 3 test partitions

- Positive: every non-null option value changes the corresponding outgoing context property exactly once.
- Default: omitted nullable values preserve existing context metadata and an empty header set adds nothing.
- Negative: null option collections, unsupported partition capability, invalid page sizes, incomplete inbox cursors, null queries, and expired request deadlines fail before provider I/O.
- Boundary: page sizes 1 and 1,000, deadline at and immediately beyond the injected current time, and null-valued headers.
- Propagation: cancellation-token identity is asserted at endpoint, scheduler, request, outbox-store, and inbox-store boundaries.

## Iteration 3 mutation obligations

- Remove each outgoing metadata assignment or header loop: its independent assertion must fail.
- Reintroduce silent partition-key discard: the unsupported-capability test must fail.
- Remove public API pipe forwarding or replace its cancellation token: the recording boundary test must fail.
- Remove either reliable query validation call or replace either forwarded query/token: the store-boundary test must fail.
- Ignore `RequestOptions.Deadline` or use the process clock: the injected-clock boundary test must fail.

## Iteration 4 outcome

Make physical source navigation deterministic without changing runtime behavior: every ordinary C# file must be named for a top-level type it owns, multi-type files without a primary type must be explicitly classified as cohesive declarations, and the convention must cover every evaluated product and native-test compile item.

## Iteration 4 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SOURCE-NAVIGATION` | Single-type files match their declared type; multi-type files identify a primary type or a reviewed cohesive grouping | architecture tests | Roslyn inventory over all evaluated `src/**` and `tests/**` compile items |
| `REQ-VSB-SOURCE-NAVIGATION` | Partial fragments and generated/global files remain valid without weakening ordinary source rules | architecture tests | Explicit syntactic classification and stale-exception rejection |

## Iteration 4 mutation obligations

- Rename a representative single-type file away from its declared type: the source-navigation guard must fail with the exact repository-relative path.
- Change a reviewed cohesive group without updating its exact type manifest: the source-navigation guard must fail with the exact path and declarations.
- Change an approved partial fragment so that its exact owner identity no longer matches: the source-navigation guard must fail.
- Add a declaration to a `GlobalUsings` file or a secondary test class using a qualified xUnit attribute: infrastructure and test naming checks must fail.
- Add an unnecessary exception for an already conforming file: stale-exception validation must fail.

## Iteration 5 outcome

Remove compatibility-era public metadata and identifier contracts that a .NET 10 greenfield API would not introduce: span-based `NewId` formatting and parsing with exact boundary validation, standard unsupported-capability exceptions, no legacy binary-serialization markers, and no former state-machine product identity in source or package metadata.

## Iteration 5 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-NEWID-SPAN-API` | identifier bytes and text use read-only spans without by-reference managed objects | abstractions tests | exact reflection shape plus unchanged reference corpus |
| `REQ-VSB-NEWID-SPAN-API` | every formatter accepts exactly 16 bytes and every parser accepts exactly 26 characters | abstractions tests | both adjacent invalid lengths, null constructor input, and invalid alphabet/input cases |
| `REQ-VSB-GREENFIELD-CAPABILITIES` | unsupported behavior uses the BCL capability exception rather than an unfinished-code identity | architecture and behavior tests | no custom type or throw site plus stable failure classification |
| `REQ-VSB-GREENFIELD-METADATA` | product declarations carry no legacy binary-serialization opt-in | architecture tests | complete evaluated product compile-item scan |
| `REQ-VSB-GREENFIELD-IDENTITY` | source, documentation, and package metadata contain no former state-machine brand | architecture tests | complete product project and compile-item scan |

## Iteration 5 mutation obligations

- Reintroduce a by-reference `string` or `byte[]` parameter into the public `NewId` surface: the API-shape test must fail.
- Bypass the exact 16-byte formatter boundary independently for short and long input: boundary tests must fail.
- Reintroduce the custom unsupported-capability exception, a `[Serializable]` product declaration, or the former state-machine brand: the corresponding whole-product architecture test must fail.

## Iteration 6 outcome

Eliminate externally mutable process-global metadata and shared routing-slip state, and replace the RabbitMQ cluster-node parser with a canonical .NET parsing contract that round-trips DNS, IPv4, and IPv6 nodes while rejecting invalid ports before connection work.

## Iteration 6 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-METADATA-IMMUTABILITY` | message types, contract names, and reflected properties cannot mutate cached process state | abstractions tests | read-only public shape, mutation rejection, and stable repeated/parallel observations |
| `REQ-VSB-METADATA-IMMUTABILITY` | the core metadata facade cannot weaken the abstraction-level immutability contract | core metadata tests | exact public return types, stable collection identity, and mutation rejection |
| `REQ-VSB-CONSUMER-METADATA-IMMUTABILITY` | consumer convention metadata cannot be externally mutated | core consumer tests | read-only public shape and stable repeated observation |
| `REQ-VSB-COURIER-BUILDER-ISOLATION` | no-argument activities cannot share a publicly mutable dictionary | core courier tests | no public mutable sentinel, immutable activity arguments, and independent builders |
| `REQ-VSB-RABBITMQ-CLUSTER-NODE` | node text follows standard string/span parsing and canonical formatting | RabbitMQ tests | DNS, IPv4, bracketed/raw IPv6, absent/edge ports, invalid input, and culture independence |

## Iteration 6 mutation obligations

- Return a cached array directly from one metadata surface: the immutability shape or mutation test must fail.
- Restore the public mutable `NoArguments` dictionary: the API-shape test must fail; make the private empty dictionary mutable: the behavior test must fail.
- Restore the nullable-port formatting defect or accept a port outside `1..65535`: the cluster-node round-trip or boundary test must fail.

## Iteration 7 outcome

Make every application-interface capability a compile-time contract. Root interfaces must not use default implementations that probe for an Advanced interface at runtime and fail only after deployment; Advanced adapters may implement the required application member only when they provide the complete underlying capability.

## Iteration 7 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-GREENFIELD-APPLICATION-CONTRACTS` | application interfaces have no runtime-probing default members | architecture tests | reflection over every member declared by send, publish, scheduler, and typed consume contracts |
| `REQ-VSB-GREENFIELD-APPLICATION-CONTRACTS` | advanced implementations bridge options and scheduled cancellation without capability casts | abstraction/core behavior tests | exact option/cancellation propagation plus compile-time implementation closure |

## Iteration 7 mutation obligations

- Restore a runtime-probing default body on any application interface: the reflection guard must fail.
- Drop an options pipe or cancellation token from an Advanced adapter: the existing independent metadata and token assertions must fail.

## Iteration 8 outcome

Make application-level outgoing options stable at the asynchronous boundary, reject invalid lifetimes before provider work, and ensure every physical product source file belongs to exactly one evaluated product compilation.

## Iteration 8 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-APPLICATION-OPTIONS-IMMUTABILITY` | Default header collections cannot be mutated and caller-owned headers are snapshotted before asynchronous work | abstractions and core client tests | mutation rejection plus send, publish, schedule, and request boundary observations |
| `REQ-VSB-APPLICATION-OPTIONS-VALIDATION` | Zero and negative message lifetimes fail synchronously before endpoint or provider use | abstractions and core client tests | both adjacent invalid partitions across option pipes and request entry point |
| `REQ-VSB-SOURCE-OWNERSHIP` | Every physical `src/**/*.cs` file has exactly one owner in the evaluated product graph | architecture tests | complete file-to-`@(Compile)` ownership comparison across all product projects |

## Iteration 8 mutation obligations

- Retain a caller-owned header dictionary instead of copying it: all three outgoing pipe snapshot tests must fail.
- Permit a zero lifetime by changing the boundary from `<=` to `<`: the zero-boundary test must fail while the negative partition remains valid.
- Replace one immutable default header collection with a mutable dictionary: the default-shape test must fail.
- Add a product source path excluded from every project: the compile-ownership test must fail with the exact path and zero owners.

## Iteration 9 outcome

Make dependency resolution centrally owned and exact, remove accidental layer edges, and prove the
provider-testing delivery packages through isolated package-only consumers. Generate the packed
public API inventory deterministically across independent package runs.

## Iteration 9 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-DEPENDENCY-PINNING` | Every centrally managed project enables central transitive pinning | architecture tests | evaluated property across the complete governed project graph |
| `REQ-VSB-DEPENDENCY-VERSION-OWNERSHIP` | Every central-transitive lock entry equals its central minimum | architecture tests | all frameworks and governed lock files |
| `REQ-VSB-DEPENDENCY-CATALOG` | The central catalog contains no unused identity | architecture tests | direct and resolved package closure plus file-based tool locks |
| `REQ-VSB-TESTING-DI-BOUNDARY` | Testing packages depend on DI abstractions without the full container | architecture tests | exact direct package references for all testing packages |
| `REQ-VSB-CAPABILITY-DEPENDENCY-BOUNDARIES` | Capability packages contain only their intentional direct edges | architecture tests | exact visualizer and EF Core dependency sets |
| `REQ-VSB-TESTING-PACKAGE-CONSUMERS` | Each provider-testing package restores, builds, and executes without another direct ViciOne package | package consumer and architecture gates | three isolated consumers against a fresh package feed |
| `REQ-VSB-PACKED-PUBLIC-API` | Repeated package-only API inventories are byte-identical | package gate and architecture tests | stable structural inventory and identical SHA-256 values |

## Iteration 9 mutation obligations

- Disable central transitive pinning: the evaluated-property guard must fail for every centrally managed project.
- Change one central-transitive resolved version: the lock-version guard must report the exact project and package.
- Restore a full DI-container dependency in a testing package: the exact dependency-boundary guard must fail.
- Add a second direct ViciOne package to an isolated consumer: the package-isolation guard must fail.
- Include NuGet archive hashes in the public API inventory: independent identical package runs must produce different output and expose the nondeterminism.

## Iteration 10 outcome

Turn the deterministic package API inventory into an enforced, versioned public contract for every
delivered package. Pack the complete thirty-package catalog, restore all twenty-nine runtime
packages through a dedicated package-only consumer, and reject any unreviewed difference from the
committed packed API baseline.

## Iteration 10 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-PACKED-PUBLIC-API` | The gate compares a freshly generated inventory with a committed baseline and has an explicit update operation | architecture and package gates | exact script contract, tracked non-empty baseline, and default mismatch failure |
| `REQ-VSB-PACKAGE-CATALOG` | Every packable product project produces its exact documented package | architecture and package gates | evaluated project/package catalog equals all thirty expected package files |
| `REQ-VSB-PACKED-PUBLIC-API` | Every runtime package participates in a package-only restore before reflection | architecture and package gates | dedicated consumer with twenty-nine direct locked package references and no source references |

## Iteration 10 mutation obligations

- Replace the baseline comparison with unconditional success: the architecture contract must fail.
- Change one committed API line: the default fresh-package gate must fail and report the diff.
- Remove one runtime package reference from the complete API consumer: the exact catalog and lock
  assertions must fail.
- Remove one expected package artifact from the gate catalog: the exact packable-project/package
  comparison must fail.

## Iteration 11 outcome

Preserve caller cancellation as cancellation across timeout wrappers, job shutdown, provider
cleanup admission, and test-harness polling. Genuine elapsed deadlines must remain timeouts, while
job-owned cancellation remains a successful shutdown condition.

## Iteration 11 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-TASK-TIMEOUT-CANCELLATION` | generic and non-generic, pre-canceled and canceled while waiting | abstractions tests | exact caller token in `OperationCanceledException` for all four partitions |
| `REQ-VSB-JOB-CANCELLATION` | caller cancellation during the completion wait | core job tests | exact caller token escapes while the job-owned token is canceled |
| `REQ-VSB-ACTIVEMQ-CANCELLATION` | queue and topic deletion canceled during bounded admission | ActiveMQ tests | queued broker operation is not invoked and exact token escapes |
| `REQ-VSB-TEST-HARNESS-CANCELLATION` | state observation canceled during polling | testing tests | polling returns caller cancellation without waiting for another interval |

## Iteration 11 mutation obligations

- Restore either timeout-wrapper cancellation branch to `TimeoutException`: its exact generic or
  non-generic cancellation test must fail.
- Omit the caller token from the job-completion wait or catch caller cancellation: the job-handle
  test must fail.
- Restore `CancellationToken.None` for either ActiveMQ deletion: the corresponding saturated-queue
  test must fail.
- Restore the non-cancelable state-machine polling delay: its in-flight cancellation test must fail.

## Iteration 12 outcome

Make asynchronous intent mechanically unambiguous in both directions and normalize every public
product cancellation signature to the standard final-parameter shape. Update all implementations,
forwarders, call sites, documentation, and the intentionally versioned package API contract as one
atomic change.

## Iteration 12 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-API-ASYNC-NAMING` | every evaluated product and test method, including local functions | architecture tests | no async contract without `Async`; no `Async` marker without async contract |
| `REQ-VSB-API-CANCELLATION-SHAPE` | every externally visible product method with a token | architecture tests | token is the final declared parameter |
| existing request behavior requirements | reordered request factory signatures | core and abstraction tests | unchanged exact timeout, token, destination, context, and message forwarding |

## Iteration 12 mutation obligations

- Add `Async` to a synchronous helper or remove it from an asynchronous local function: the exact
  bidirectional architecture branch must fail.
- Move one public request-factory token before its timeout again: the cancellation-shape gate must
  report the exact member and following parameter.
- Swap or omit timeout/token forwarding at an implementation boundary: the existing request
  metadata and dependency-injection forwarding tests must fail.

## Iteration 13 outcome

Replace embedded compiler maintenance with an explicitly owned current package; remove redundant or
convenience-only compiler directives and IDE/maintenance markers; and correct confirmed API comments
whose stated timing, acknowledgement, provider, or return-value semantics disagree with the code.

## Iteration 13 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SOURCE-DIRECTIVES` | every product directive is an exact reviewed feature-preserving exception | architecture tests | syntax-aware full product inventory equals the two-line Amazon S3 allowlist |
| `REQ-VSB-SOURCE-COMMENTS` | maintenance markers and historical/speculative construction narrative | architecture tests | Roslyn comment-trivia scan including trailing comments |
| `REQ-VSB-SOURCE-COMMENTS` | known false timing, acknowledgement, provider, and lifecycle contracts | architecture tests | zero complete-comment matches across all product sources |
| `REQ-VSB-DEPENDENCY-OWNERSHIP` | expression compilation is centrally versioned, package-owned, explicitly imported, and never embedded | architecture and product builds | exact version, owner set, source absence, and import closure |

## Iteration 13 mutation obligations

- Insert a redundant nullable directive: the exact directive inventory must fail.
- Add a trailing maintenance marker: the Roslyn comment scan must fail.
- Restore a false broker-acknowledgement contract: the semantic documentation guard must fail.
- Remove one expression-compiler import: the owning product build must fail at the call site.

## Iteration 49 outcome

Make Azure Table one coherent greenfield capability: align public namespaces and physical ownership
with the package, hide provider implementation types, normalize all composition verbs, preserve the
complete saga and bounded-journal feature set, and prove behavior against the real Azurite API.

## Iteration 49 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-AZURE-TABLE-PUBLIC-API` | exact intended exported types and greenfield names | Azure Table unit tests | reflection over the complete exported type/member surface |
| `REQ-VSB-AZURE-TABLE-CONFIGURATION` | direct saga, registered saga, runtime-type, Job Service, and message-journal composition | Azure Table unit and local-integration tests | fail-fast ownership plus real persisted behavior |
| `REQ-VSB-AZURE-TABLE-SAGA-BOUNDARY` | nulls, cancellation, insert conflict, ETag update/delete, unsupported query | Azure Table unit tests | exact exceptions, token identity, provider-call counts, and error identity |
| `REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION` | native and serialized property conversion | Azure Table unit and real-provider tests | exact round trip plus malformed-value failure |
| `REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL` | finite capacity, retention, property limits, lease concurrency, and foreign-row ownership | Azure Table unit and local-integration tests | ordered transaction assertions and real concurrent Azurite writes |
| requirement projection completeness | unit and local manifests include their projection gates | both Azure Table test projects | projection tests pass against exact embedded manifests |

## Iteration 49 mutation obligations

- Remove custom-formatter key validation: the unsafe-key boundary test must fail before provider I/O.
- Stop mapping an Azure 412 write response to typed concurrency: the exact exception test must fail.
- Stop mapping an Azure 409 duplicate save to typed concurrency: the exact save contract must fail.
- Broaden the journal row-key range to include foreign rows: the real ownership test must fail.
- Permit a null repository dependency: the exact fail-fast boundary test must fail.
- Re-expose one provider implementation type: the exact exported-surface test must fail.
- Discard a saga-specific formatter during registration: the two-saga DI contract must fail on the
  exact formatter instance without conflating registrations across saga types.

## Iteration 50 outcome

Make MessagePack a self-contained greenfield serialization capability: expose only composition and
the advanced serializer factory, align namespaces and folders with the package, remove optional
Courier and Job Service product dependencies without losing their contract behavior, and make
mutable descriptors and concurrent resolver creation safe.

## Iteration 50 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-MESSAGEPACK-PUBLIC-API` | exact two-type exported surface | MessagePack tests | complete reflection inventory of exported types |
| `REQ-VSB-MESSAGEPACK-CONFIGURATION` | extension null ownership, shared concurrent serializer, independent media-type descriptors | MessagePack configuration tests | exact exceptions, reference identity, and mutation isolation |
| `REQ-VSB-MESSAGEPACK-DEPENDENCIES` | no Courier or Job Service assembly dependency | MessagePack configuration and domain tests | assembly-reference exclusion plus retained nested Job and Courier round trips |
| `REQ-VSB-MESSAGEPACK-RESOLVER` | production resolver chain selects ServiceBus mappings and generic interface formatters | MessagePack resolver tests | exact formatter types returned by the composed production options |
| `REQ-VSB-MESSAGEPACK-FORMATTER-CACHE` | concurrent cold access returns one formatter instance | MessagePack formatter tests | sixteen synchronized callers observe one reference |
| requirement projection completeness | projection gate is represented in the MessagePack manifest | MessagePack requirement tests | compiled metadata and embedded manifest are identical |

## Iteration 50 mutation obligations

- Re-expose an implementation envelope: the exact public-surface test must fail.
- Remove a composition null guard: the extension-boundary test must report the wrong exception.
- Return one mutable media-type descriptor: the isolation test and transport consumers must fail.
- Bypass the formatter cache: the synchronized resolver identity test must fail.
- Replace untrusted-data security with trusted-data mode: the security contract must fail.
- Restore a Courier product mapping and dependency: the assembly-reference contract must fail.
- Remove the ServiceBus resolver from the composed options: the production-chain selection test must fail.

## Iteration 77 outcome

Make cache and request-client lifetimes deterministic at concurrency boundaries, make absolute
deadlines truly absolute, expose factory ownership through the public contract, fail at each owning
API boundary before dependency work, and give client internals explicit physical and namespace
owners without changing request, publish, send, scoped, mediator, or cache features.

## Iteration 77 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-CACHE-MULTI-INDEX` | synchronous index projection concurrent with disposal | cache generation/locking tests | disposal cannot reach a resource while its selector is active |
| `REQ-VSB-CACHE-EXPIRATION` | absolute versus sliding usage observation | cache lifecycle tests | absolute mode never subscribes; sliding mode retains touch behavior |
| `REQ-VSB-REQUEST-CLIENT-BOUNDARY` | context, address, message, initializer, and wrapper boundaries | client boundary tests | exact argument exception occurs before endpoint or wrapped-factory work |
| `REQ-VSB-REQUEST-LIFECYCLE` | repeated/concurrent disposal and hostile synchronization context | client lifecycle tests | one owned disposal, no post-disposal creation, cancellation completes without ambient-context pumping |
| `REQ-VSB-APPLICATION-REQUEST-OPTIONS` | endpoint acquisition spans an absolute deadline | request metadata tests | send aborts after the exact deadline and implicit TTL cannot extend it |
| `REQ-VSB-SOURCE-NAVIGATION` | client internal responsibility layout | architecture tests | exact files and namespaces in `Contexts`, `Endpoints`, and `Requests`; no stale flat internals |

## Iteration 77 mutation obligations

- Remove the active-operation guard from synchronous index projection: the disposal race test must
  observe resource disposal while the selector is blocked.
- Re-enable usage subscription for absolute expiration: the exact subscriber-count test must fail.
- Move one argument guard after endpoint or wrapped-factory lookup: the boundary spy must record an
  unintended dependency call.
- Invoke the owned factory context twice or permit creation after disposal begins: the lifetime
  tests must fail on exact count or exception ownership.
- Start the deadline timer before delayed endpoint acquisition: the deadline test must observe a
  completed send after the absolute deadline.
- Restore ambient scheduler capture: the hostile-context test must observe a queued callback instead
  of prompt cancellation completion.
- Return one client internal to the former flat namespace or folder: the exact layout inventory and
  internal implementation manifest must fail.
- Remove addressed response-endpoint readiness gating: the receive-endpoint context test must fail.
- Make host response-endpoint disposal a no-op: exact owned-disposal evidence must fail.
- Let stale pending cache creation survive invalidation: creation ownership evidence must fail.
- Make response-handler disposal skip disconnection: exact handle forwarding must fail.
- Make keyed-cache removal or clearing a no-op: their independent release assertions must fail.
- Drop configured consume-pipe options: exact bus-context forwarding must fail.
- Remove response host metadata: exact response projection must fail.
- Disable direct-interface discovery in the public API extractor: the exact API architecture guard
  must fail before a changed interface contract can be accepted.

## Iteration 77 completion

All planned boundaries were implemented and all seventeen isolated mutations were killed, restored,
and followed by fresh validation. The bounded profile passes 190 tests, the complete core profile
passes 2,721 tests, and the architecture profile passes 260 tests. Bounded coverage is 92.46% line
and 86.92% branch with no method above CRAP 30. The fresh-package/API gate passes all 18 journeys,
31 packages, three isolated provider consumers, and 30 runtime API contracts with direct-interface
metadata now enforced.

## Iteration 78 outcome

Turn consumer creation, infrastructure events, metadata, and message-data storage into coherent
greenfield responsibilities. Preserve every supported feature while making lifetime ownership,
event snapshots, storage retention, cancellation, path safety, stream ownership, and timing
semantics explicit; expose only intentional application contracts; and align all source comments,
types, namespaces, filenames, and directories with their final owners.

## Iteration 78 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-CONSUMER-FACTORY-LIFETIME` | default, delegate, object, and external-instance factories on success/failure | core consumer tests | exact sync/async disposal counts, async precedence, exception propagation, and zero disposal for external instances |
| `REQ-VSB-CONSUMER-CONVENTION-STATE` | stable cache snapshot while process-global conventions are mutated | core consumer tests | serialized mutation collection plus repeated and concurrent same-version identity |
| `REQ-VSB-EVENT-SNAPSHOTS` | readiness, endpoint/transport lifecycle, and fault projections | core event tests | exact host/input identifiers, addresses, tags, metrics, time-provider values, immutable copies, and null-element rejection |
| `REQ-VSB-METADATA-OWNERSHIP` | public message metadata versus internal implementation-type creation | core and architecture tests | no redundant public facade; exact dynamic implementation caching retained through all serializers/initializers |
| `REQ-VSB-MESSAGEDATA-PUBLIC-API` | intentional application surface only | architecture and package API tests | exact exported type/member allowlist and updated deterministic packed baseline |
| `REQ-VSB-MESSAGEDATA-REPOSITORIES` | in-memory and file-system null, cancellation, retention, missing data, and path containment | core MessageData tests | fail-before-I/O guards, exact token identity, injected-time expiry boundaries, round trips, and traversal rejection |
| `REQ-VSB-MESSAGEDATA-CONVERSION` | byte/text/object/stream conversion and lazy resolution | core MessageData tests | input snapshots, null/type failures, single fetch, explicit stream-retention capability, and exact disposal ownership |
| `REQ-VSB-MESSAGEDATA-PROPERTY-PROVIDERS` | synchronously/asynchronously completed, faulted, canceled, empty, and populated inputs | core MessageData tests | identical value and exception behavior independent of task completion timing |
| `REQ-VSB-MESSAGEDATA-COMPOSITION` | selector, repository, path, policy, and convention boundaries | core MessageData tests | exact caller-parameter failures before registration, repository, or transport work |
| `REQ-VSB-SOURCE-NAVIGATION` | consumer/event/metadata/message-data owners | architecture tests | exact matching namespaces, filenames, folders, visibility, and no stale former owner |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the bounded production scope | manual review plus architecture hygiene tests | comments state only current code/function behavior; no historical, procedural, stale, filler, or generated narrative |

## Iteration 78 mutation obligations

- Skip owned-consumer disposal on success or failure, prefer synchronous disposal over
  `IAsyncDisposable`, or dispose an external instance: the corresponding exact lifetime assertion
  must fail.
- Remove convention-test serialization or return a new same-version cache snapshot: the stability
  test must fail without relying on arbitrary delays.
- Drop one event field, reuse a caller-owned mutable collection, or admit a null message type: the
  direct snapshot assertion must fail.
- Re-expose one implementation cache, converter, value, property provider, or configuration
  specification: the exact public-surface inventory must fail.
- Ignore pre-cancellation, bypass in-memory expiry, permit either adjacent expiry boundary, or allow
  a file path outside the configured root: the repository contract test must fail before unrelated
  I/O.
- Infer source-stream retention from a concrete converter type or omit a required disposal: the
  custom-converter ownership test must fail.
- Restore `.Result`, branch on task completion timing, or treat `HasValue == false` differently in
  one timing partition: paired property-provider tests must observe the mismatch.
- Let a null selector result, path, repository, converter, address, or payload reach a dependency:
  the owning boundary spy must record the unintended call or the wrong parameter name.
- Return one type to the former flat/singular namespace or top-level `Metadata` owner: the exact
  source-navigation inventory must fail.
- Restore a stale or procedural comment from the bounded inventory: the syntax-aware hygiene gate
  and the manual file ledger must reject the exact source location.

## Iteration 78 completion

All planned consumer, event, metadata, and message-data boundaries were implemented and the nine
isolated product mutations were killed and restored. The reviewed capability has 88.62% line and
80.59% branch coverage across 246 instrumented methods with no CRAP score above 30. The internal
Async Red Team converted every surviving suffix, alias, Release, same-FQN, and Quartz-interface
attack into semantic protection; its final focused profile passes 30 tests on an unchanged hash.
The final architecture assembly passes 289 tests and the complete sequential Unit solution passes
5,323 tests with no failures or skips. The protected `review/` and untracked `TestResults/` trees
remain outside the iteration commit.

## Iteration 79 outcome

Make the abstractions project root a strict application-contract boundary. Separate the
consume-scoped outgoing implementation into its owning context capability, prove all five outgoing
operations and their argument/dependency boundaries directly, pin the named conservative message
policy, and retain every public feature while making file, type, namespace, and comment ownership
deterministic.

## Iteration 79 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SOURCE-NAVIGATION` | exact abstractions project-root files and namespaces | architecture tests | root contains only reviewed application contracts and project infrastructure |
| `REQ-VSB-SOURCE-NAVIGATION` | public outgoing contract versus internal context implementation | architecture tests | exact declared type identities in `IOutgoingMessages.cs` and `Context/ConsumeContextOutgoingMessages.cs` |
| `REQ-VSB-APPLICATION-CONSUME-OUTGOING` | routed and explicit sends | core context tests | exact route, endpoint resolution, message/options/token forwarding, and dependency-call counts |
| `REQ-VSB-APPLICATION-CONSUME-OUTGOING` | default and configured publish | core context tests | exact publish endpoint resolution, message/options/token forwarding, and dependency-call counts |
| `REQ-VSB-APPLICATION-CONSUME-OUTGOING` | scheduled send and unavailable scheduler | core context tests | exact destination/due time/message/token/result identity and fail-before-scheduler behavior |
| `REQ-VSB-APPLICATION-CONSUME-OUTGOING` | constructor and every public null boundary | core context tests | exact parameter name and zero downstream work |
| `REQ-VSB-MESSAGE-LIMITS` | named conservative policy | configuration tests | exact five values and stable singleton identity |
| `REQ-VSB-SOURCE-COMMENTS` | all comments in the bounded root scope and moved implementation | manual review plus hygiene tests | comments describe only current behavior, completion, ownership, and failure semantics |

## Iteration 79 mutation obligations

- Reinsert an implementation into an application contract file or add an unreviewed root file: the
  exact source-navigation inventory must fail on the changed path or declared type set.
- Ignore the configured route, alter the routed destination, or resolve/send more than once: the
  routed-send test must fail on exact route, address, message, token, or call count.
- Drop explicit options or replace either caller token: the direct send/publish tests must fail on
  reference identity or token equality.
- Bypass default publication or invoke a downstream provider for a null message: the publish
  partition must fail on exact call count or boundary exception.
- Ignore the consume-context scheduler, alter due time/destination/message/token, or manufacture a
  different result: the scheduled-send test must fail on exact forwarding and result identity.
- Remove one null guard or move it after dependency work: the boundary matrix must fail on exact
  parameter name or nonzero dependency calls.
- Change one value of `MessageLimits.Conservative` or allocate a replacement per access: the named
  policy contract must fail.
- Restore generic task-filler wording or stale ownership language in the bounded comments: manual
  review and the repository comment-hygiene gate must reject the changed source.

## Iteration 79 completion

The fifteen original Abstractions application-root files and the project definition were read
manually in full. The root is now an exact application-contract boundary: the internal consume
outgoing implementation moved to `Context`, all five operations and their argument/dependency
boundaries have direct tests, the real InMemory journey covers routed and default operations, and
the conservative message policy is pinned exactly. Seven isolated product/structure mutations were
killed and restored. The final Release build has no warnings or errors; the complete sequential
Unit solution passes 5,332 tests, the Async guard passes 30 tests, and fresh package/API validation
preserves the 19,773-line public contract. The overall goal remains open; iteration 80 continues
with the Abstractions `Context` owner.

## Iteration 80 outcome

Replace the mixed Abstractions `Context` bucket with explicit API-layer and runtime owners. Preserve
public send/publish proxy and scope functionality as Advanced SPI, internalize and functionally
name runtime dispatch, split each options implementation into its matching source file, relocate
Core-only consume/retry/sentinel implementations, and correct typed-proxy scope preservation. Prove
every moved behavior and boundary directly before deleting the old catch-all tests and directory.

## Iteration 80 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SOURCE-NAVIGATION` | exact Advanced context SPI, internal dispatch/options, and Core owner layouts | architecture tests | exact paths, namespaces, type identities, visibility, and absent former `Context` directory |
| `REQ-VSB-API-LAYERING` | public proxies/scopes versus internal runtime mechanics | architecture tests | Advanced SPI remains exported; dispatchers, sentinel, outgoing facade, and pending faults are not exported |
| `REQ-VSB-RUNTIME-DISPATCH` | send, publish, response, pipe, initializer, and task forwarding | Abstractions dispatcher tests | exact generic contract, object, pipe, token, result task, and one downstream invocation |
| `REQ-VSB-RUNTIME-DISPATCH` | null, incompatible, open, value, by-ref, and pointer contracts | Abstractions dispatcher tests | exact parameter and zero downstream invocations |
| `REQ-VSB-CONTEXT-PROXY` | all send/publish getters, setters, typed messages, payload behavior, and replacement views | Abstractions proxy tests | exact state forwarding and replacement view retains the current proxy |
| `REQ-VSB-CONTEXT-SCOPE` | self/local/parent precedence, isolated add/update, cancellation, typed message, and replacement view | Abstractions scope tests | exact factory counts, identity, owner mutation, and retained local payload |
| `REQ-VSB-MISSING-CONSUME-CONTEXT` | every property and operation family | Core context tests | every member fails with the domain exception; token-bearing operations preserve pre-cancellation |
| `REQ-VSB-PENDING-FAULTS` | validation, sealing, concurrency, cancellation, complete notification, and sync/async failures | Core retry tests | exact data/token forwarding and all collected notifications are attempted |
| `REQ-VSB-APPLICATION-OPTIONS-*` | every options type, snapshot, apply, partition capability, and probe | Abstractions options tests | exact immutable snapshot, fields, failure order, and scope name |
| `REQ-VSB-SOURCE-COMMENTS` | all comments in the original and final bounded scope | manual review plus hygiene gates | only current functional, state, ownership, and failure semantics remain |

## Iteration 80 mutation obligations

- Return a replacement send view over the wrapped parent instead of the current proxy: the local
  payload-retention assertion must fail.
- Omit or redirect any send/publish proxy getter or setter: the complete property matrix must fail
  on exact value or recorded mutation.
- Drop a pipe, token, explicit runtime type, values object, or returned task from any dispatcher
  overload: the exact invocation and task-identity tests must fail.
- Re-export one dispatcher, sentinel, outgoing implementation, or pending-fault collection, or
  restore the former mixed directory: the API-layer/source-owner architecture test must fail.
- Stop notification enumeration after one synchronous observer failure: the every-fault attempt
  test must observe the missing later call.
- Let a late fault enter after sealing, seal on pre-cancellation, or permit a second notification:
  the collection state tests must fail.
- Change payload lookup precedence, invoke an unnecessary factory, update a parent payload in
  place, or lose the typed message: the scope identity and factory-count tests must fail.
- Apply an unset option, share caller-owned headers, mutate context before rejecting an unsupported
  partition key, or use the wrong probe scope: the options tests must fail.
- Restore a stale path, cache-centric public description, or operation-forwarding claim that the
  proxy does not implement: the exact source inventory and manual comment ledger must reject it.

## Iteration 80 completion

All original `Abstractions/Context` files, their contracts, callers, final owners, and comments were
read and adjudicated manually. The public proxy/scope SPI moved to `Advanced/Contexts`; runtime
dispatch and option snapshots became internal implementation owners; Core-only consume and retry
types moved into Core; and the former mixed directory was removed. The send proxy now retains its
current payload scope across typed replacement, and pending fault notification attempts every
collected fault even when an observer throws synchronously or returns no task.

Eight isolated mutations were killed and restored. The final Release Unit-solution build has zero
warnings and errors, the complete Unit solution passes 5,356 tests, and the fresh package/API gate
passes all 18 journeys, 31 packages, three provider consumers, and 30 API assemblies. The three
executable Core files reach 136/136 covered lines and 20/20 covered branches. The old directory,
empty source directories, preprocessor directives, stale public cache/sentinel exports, and bounded
dummy/legacy markers are absent. The repository-wide A+ source goal remains active.

## Iteration 81 outcome

Make Advanced context metadata and consume-scoped send operations precise, idiomatic, directly
tested, and navigable while preserving the coherent project boundary. Correct inconsistent message
body boundaries discovered while validating the context contract, and explicitly retain the
cross-project mediator-body and context-interface naming decisions for their complete owning passes.

## Iteration 81 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-ADVANCED-CONTEXT-KEYS` | consume/send partition and routing lookup | Abstractions Advanced-context tests | exact payload value, absence result, and null receiver |
| `REQ-VSB-ADVANCED-CONTEXT-KEYS` | set, clear, try-set, unsupported capability | Abstractions Advanced-context tests | exact payload mutation, boolean result, exception, and no unrelated mutation |
| `REQ-VSB-CONSUME-SEND` | all ten typed/runtime/initialized overloads | Abstractions Advanced-context tests | exact endpoint, message/type/values/pipe/token forwarding and one dependency call |
| `REQ-VSB-CONSUME-SEND` | receiver, destination, value/type/pipe boundaries | Abstractions Advanced-context tests | declaration-order parameter ownership and zero endpoint resolution/send calls |
| `REQ-VSB-CONSUME-SEND` | context token, caller token, shared token, and linked tokens | Abstractions Advanced-context tests | exact token identity where possible and cancellation from either linked source |
| `REQ-VSB-CONTEXT-TRANSFER` | reused send context receives a new consume scope | Abstractions Advanced-context tests | current metadata and payload identify the same consume context |
| `REQ-VSB-CONTEXT-TRANSFER` | host, fault, causality, identifier, and redelivery metadata | Abstractions Advanced-context tests | exact copied headers, lineage, source, and payload behavior |
| `REQ-VSB-RECEIVE-METADATA` | direct, textual, and `DateTime` timestamp representations | Abstractions context tests | equal UTC result for every accepted representation |
| `REQ-VSB-RETRY-METADATA` | attempt/count/redelivery reads and null receivers | Abstractions Advanced-context tests | exact one-based semantics, zero pre-retry state, and parameter ownership |
| `REQ-VSB-EMPTY-HEADERS` | singleton shape, empty reads, enumeration, and invalid keys | Abstractions serialization tests | sealed type, stable property identity, empty values, and exact key failures |
| `REQ-VSB-SERIALIZER-CONTEXT` | header-provider parameter identity | Core serialization boundary tests | both generic overloads reject a null `headers` parameter by its public name |
| `REQ-VSB-MESSAGE-BODY-CONTRACT` | default segment and required constructor inputs | owning body tests | consistent empty views and fail-fast exact parameter names |
| `REQ-VSB-SOURCE-NAVIGATION` | project roots and Advanced context types | architecture tests and manual ledger | one assembly per project root, provider grouping retained, exact file/type/namespace owner |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the bounded files | manual review plus hygiene gates | current behavior only; no history, procedural narrative, filler, or unsupported promise |

## Iteration 81 mutation obligations

- Restore noun-shaped `PartitionKey()` or `RoutingKey()`, use the wrong capability payload, or
  report success without assignment: the exact API and behavior tests must fail.
- Validate a later consume-send parameter before the receiver/destination, resolve an endpoint for
  rejected input, or route through the wrong overload: the parameter-order and recording-endpoint
  assertions must fail.
- Drop either cancellation source, create an unnecessary linked token for one source, or use a
  different token for resolution and send: the four token partitions must fail.
- Reintroduce divergent default-array body views or defer a required native/text constructor
  failure: the direct body-boundary tests must fail on value, accessor, timing, or parameter name.
- Retain an earlier consume-context payload while copying later metadata, or skip UTC normalization
  for a direct `DateTimeOffset`: the exact identity/offset assertions must fail.
- Remove retry receiver validation, permit an invalid empty-header key, or restore the misleading
  `dictionary` name for a header provider: direct boundary checks or the XML documentation contract
  must fail on the exact public parameter.
- Split the public Advanced namespace cosmetically, move a provider project into an unrelated root,
  or add a second type to a context file: the source-navigation inventory must fail.
- Restore historical repair prose, generic "member" wording, an inaccurate copy/ownership claim,
  or an incomplete linked-cancellation description: manual review and comment hygiene must reject
  the exact location.

## Iteration 81 completion

The 31 Advanced context files and every followed source dependency changed by this iteration were
read manually in full, including their comments, types, namespaces, filenames, and physical owners.
The repository retains Core and sibling feature packages as independent project roots, while
Persistence, Scheduling, and Transports remain category roots for provider projects. This is an
ownership distinction, not an accidental duplicate source tree, and no cosmetic relocation is
introduced.

All planned context-key, consume-send, cancellation, context-transfer, receive-metadata, retry,
empty-header, serializer-parameter, and message-body boundaries are implemented and directly
protected. Ten isolated counterchanges were killed and restored. The final Release build has zero
warnings and errors; the complete sequential Unit solution passes 5,392 tests, including 292
architecture tests; and whitespace plus warn-level style gates pass. Fresh-package validation
passes 18 journeys, 31 packages, three isolated provider consumers, and all 30 runtime API
assemblies. The deliberate 19,701-line package API has SHA-256
`982dc572231657c53b09f70a396f7cdec26ac93fe07401f06eb681da1931a6a1` and reproduces on a second
unchanged run.

Core-module coverage is 70.09% line (43,449/61,986) and 62.70% branch
(14,990/23,907). It is explicitly module instrumentation rather than a fabricated whole-suite
percentage because the direct Abstractions test project has no MTP coverage provider. Global source
hygiene finds no C# preprocessor directives, dummy/stub/TODO/FIXME markers, or empty directories.
The non-readable mediator body, mutable body-array ownership, and repository-wide context-interface
naming remain queued for complete owning passes so no cross-cutting API decision is made from a
partial inventory.

The separate internal Async Red Team passes the bidirectional semantic naming audit over all 4,112
physical production C# files: the 30-test guard and two independent compilation-based scanners find
no mismatch, no `async void`, and no hidden conditional-compilation case. Its documentation axis
identified eight generic Task return comments in this iteration's fully read files; all eight are
now operation-specific. Another 698 remain in source owners not yet manually read. They form
an explicit cross-iteration worklist and will be rewritten only after each owning method has been
read and understood; no comment generator or bulk substitution may be used.

## Iteration 82 outcome

Make the Abstractions serialization owner internally minimal, culture-invariant, symmetric, and
directly tested. Remove the internal camel-case lookup mechanic from the public API, align metadata
key validation and duplicate handling in both dictionary directions, relocate the complete
extension boundary contract to its actual test owner, and manually correct every stale comment in
the owner and any fully read dependency. Resolve the cross-project message-body capability only
after independent Red-Team analysis of every implementation and caller.

## Iteration 82 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-CAMEL-CASE-METADATA` | exact JSON camel-case normalization | Abstractions serialization tests | Turkish culture, ordinary PascalCase, acronym prefix, mutable and read-only dictionaries |
| `REQ-VSB-CAMEL-CASE-METADATA` | required key and API visibility | Abstractions serialization tests | null/empty/blank rejection and non-public compile-bound type |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | dictionary/header-provider reads | Abstractions serialization tests | exact and camel-case lookup, reference/value conversion, fallback, and zero conversion on absence |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | try-get semantics | Abstractions serialization tests | present, absent, and rejected conversion for reference and value types |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | dictionary serialization/deserialization | Abstractions serialization tests | null filtering, valid keys, case-insensitive last value, empty normalization, and exact delegation |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | consume/send header reads | Abstractions serialization tests | consume conversion, direct text, defaults, send runtime-type requirement, and missing values |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | consume object projection | Abstractions serialization tests | exact source identity and returned dictionary identity |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | every required public boundary | Abstractions serialization boundary tests | every reference/value overload, exact parameter, and zero dependency calls |
| `REQ-VSB-SOURCE-NAVIGATION` | serialization test ownership | requirement gates and manual ledger | boundary test and immutable requirement entry belong to Abstractions, not Core |
| `REQ-VSB-SOURCE-COMMENTS` | all owner comments and followed header contract | manual review plus hygiene gates | only current function, representation, ownership, and failure semantics |

## Iteration 82 mutation obligations

- Restore current-culture or one-character lowercasing: the Turkish or acronym contract must fail.
- Export the camel-case helper again: the visibility contract must fail.
- Remove key validation or perform dictionary/header work first: boundary tests must fail on exact
  parameter identity or unexpected dependency invocation.
- Use `Dictionary.Add` during deserialization or preserve the first case-insensitive duplicate: the
  symmetric last-value assertion must fail.
- Deserialize an absent key, serialize an all-null dictionary, convert direct send headers, or
  deserialize an already textual consume header: call counts and exact values must fail.
- Return a different object projection or pass a replacement source: identity assertions must fail.
- Move the boundary test back to Core or restore its Core requirement entry: owner and requirement
  completeness gates must reject the mismatch.
- Restore generic header-provider or serializer filler documentation: the manual comment ledger must
  reject the fully read source.

## Iteration 82 completion

All 22 Abstractions Serialization files were read manually in full and adjudicated together with
their tests, public callers, and concrete body dependencies. The internal metadata lookup is no
longer exported; ordinary and acronym-prefixed keys use the same invariant JSON camel-case rule;
invalid retained metadata keys fail at their owning boundary; both dictionary directions use
case-insensitive last-value semantics; and every one of the sixteen public extension overloads now
has direct reference/value behavior and boundary evidence. The former Core boundary test and its
requirement entry moved to the owning Abstractions test project. `EmptyHeaders` remains under
Serialization because its namespace and test owner already match that path.

Seven valid test-first failures and four isolated counterchanges prove the corrected semantics and
API visibility. The final Release Unit-solution build has zero warnings and errors. The complete
sequential Unit solution passes 5,405 tests with no failures or skips, including all 292 architecture
tests; the Abstractions assembly contributes 588 direct tests. Repository-wide whitespace and
warn-level style verification pass. Fresh-package validation passes 18 developer journeys, 31
packages, three isolated provider-testing consumers, and 30 runtime API assemblies. The reviewed
19,698-line packed API contract has SHA-256
`29152df6b6532748a802e66de982ab65db03479397c235cb855c4764e8665ece` and matches the independently
regenerated artifact byte-for-byte.

Global source hygiene remains at zero C# preprocessor directives, zero empty source directories,
and zero dummy/stub/TODO/FIXME/compatibility-shim markers. The fully read Serialization owner and
followed `IHeaderProvider` contain none of the known generic task/header/operation filler. The
independent body Red Team reports FAIL for the existing cross-project `MessageBody` contract and
defines the next atomic owner: immutable byte ownership, explicit text encoding, readable Mediator
content, all implementations/callers, adversarial mutation tests, and an Embedded-target allocation
gate. This is retained as an explicit next iteration rather than hidden by a partial local fix.
