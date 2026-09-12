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

## Iteration 83 outcome

Replace the implementation-defined `MessageBody` capability with one Greenfield contract that is
always materialized, length-known, byte-stable, independently streamable, and explicit about the
text representation required by text-only transports. Consolidate redundant array, byte-array, and
memory implementations without losing segment, empty, UTF-8, Base64, JSON, MessagePack, native
provider, mediator, forwarding, durable, journal, outbox, or scheduling behavior.

## Iteration 83 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-MESSAGE-BODY-CONTRACT` | public shape and concrete owner census | Abstractions, Core, MessagePack, and provider tests | exact length, defensive `byte[]` copies, independent `OpenReadStream`, optional transport text, sealed concrete types |
| `REQ-VSB-BINARY-MESSAGE-BODY` | selected memory, empty content, ownership | Abstractions serialization tests | exact segment, constructor snapshot, source mutation isolation, no implicit binary-to-text conversion |
| `REQ-VSB-TEXT-MESSAGE-BODY` | UTF-8 text and Base64 carrier text | Abstractions serialization tests | exact bytes/text, eager validation, source stability, malformed Base64 boundary |
| `REQ-VSB-MESSAGE-BODY-STREAM` | repeatable independent streams | shared owner assertions | position zero, read-only behavior, independent positions and disposal, unchanged content |
| `REQ-VSB-SERIALIZED-BODY-SNAPSHOT` | JSON and MessagePack materialization | Core and MessagePack tests | source mutation isolation, accessor order, concurrent access, exact bytes, single serialization |
| `REQ-VSB-MEDIATOR-BODY` | bounded canonical JSON materialization before dispatch | Core mediator and architecture tests | readable exact body, byte length, oversize rejection, cancellation, dispatch ordering, bounded growth |
| `REQ-VSB-NATIVE-BODY-SNAPSHOT` | ActiveMQ, SQS, and Azure Service Bus adapters | owning provider tests | constructor-time snapshot, no caller/native mutation leakage, exact text/binary representation |
| `REQ-VSB-BODY-CROSS-OWNER` | forwarding, move, persistence, journal, outbox, scheduling | owning unit/integration tests | byte-for-byte transport paths and exact text-only carrier paths preserve all features |
| `REQ-VSB-SOURCE-NAVIGATION` | body filenames, namespaces, and visibility | architecture tests and manual ledger | one type per file, no redundant binary implementation, provider adapters remain with providers |
| `REQ-VSB-SOURCE-COMMENTS` | every fully read body and caller file | manual review plus hygiene gates | current ownership, encoding, stream, failure, and completion semantics only |

## Iteration 83 mutation obligations

- Return caller/native memory directly, delay a snapshot, or expose a mutable array: mutation after
  construction and returned-view adversarial tests must observe the breach.
- Remove exact length or defensive byte-copy access, or make optional transport-text discovery
  throw for an opaque body: public-shape and mediator contract tests must fail.
- Reuse one stream, expose a writable stream, inherit position, or let disposal affect later reads:
  independent-stream tests must fail.
- Decode opaque binary bytes as text or conflate Base64 carrier text with decoded bytes:
  exact-representation and capability tests must fail.
- Serialize JSON or MessagePack more than once, race first materialization, or observe later source
  mutation: concurrency, invocation-count, and snapshot tests must fail.
- Measure mediator content without retaining it, allocate beyond its declared hard bound, dispatch
  before materialization, or lose cancellation: direct and architecture tests must fail.
- Read SQS, NMS, or BinaryData again after construction, or return native storage: provider mutation
  tests must fail.
- Decode a binary body at a byte-only destination or Base64-encode a textual body at a text-only
  destination: cross-owner transport and persistence assertions must fail.
- Restore `ArrayMessageBody` or `MemoryMessageBody`, keep an unsealed concrete body, or export a
  provider-only implementation: compile-bound census and public API review must fail.
- Retain implementation-defined ownership prose, ambiguous `GetString`, or obsolete measured-only
  mediator comments: manual review and documentation checks must reject the exact source.

## Iteration 83 completion

The complete public body contract, every production implementation, every changed byte/text
consumer, and their directly affected comments were read manually before the final design was
accepted. `MessageBody` is now one immutable, materialized contract with an exact `Length`, a
defensive `ToArray`, an independent read-only `OpenReadStream`, and an optional explicit transport
text capability. The three overlapping array/memory implementations were replaced by one sealed
`BinaryMessageBody`; JSON, MessagePack, Mediator, native-provider, forwarding, journal, durable,
outbox, scheduling, and SQL owners now preserve the same snapshot and representation rules.

The apparently repeated `ViciOne.ServiceBus` path was also adjudicated during this pass. The
directory `src/ViciOne.ServiceBus` is the Core project, while the sibling
`src/ViciOne.ServiceBus.*` directories are separate product assemblies. Persistence, Scheduling,
and Transports remain repository-level provider groups containing separate adapter projects. This
topology makes assembly and dependency ownership visible; nesting sibling projects inside the Core
project would create a false ownership relationship and SDK glob hazards. Genuine type, namespace,
filename, and folder mismatches will still be corrected in each complete owning pass.

Five isolated counterchanges were compiled, executed, killed, and restored: forwarding with a copy
serializer, non-Base64 Quartz persistence, non-Base64 Entity Framework outbox persistence, ActiveMQ
text transport for binary MessagePack, and acceptance of the native SNS wrapper instead of its
payload. The final sequential Release Engineering build and both formatting gates have zero
warnings, errors, or changes. The complete Unit solution passes 5,477 tests with no failures or
skips. Fresh package validation passes 18 developer journeys, 31 packages, three isolated provider
consumers, and all 30 runtime API assemblies. The deliberate 19,674-line public API has SHA-256
`7841eea6a51d14b0dfbe8062838e5d1cacb10248b55da34ad5add0f6f0cc186d`.

Real-provider acceptance passes for ActiveMQ OpenWire and AMQP (2/2), Amazon SQS/SNS through
LocalStack (1/1), PostgreSQL (1/1), and SQL Server (1/1). The new cross-owner tests additionally
prove successful in-memory forwarding, Quartz store/rehydrate/deliver, and classic Entity Framework
outbox store/replay with exact MessagePack binary payloads. An internal read-only Red Team found no
remaining Critical or High implementation defect; its originally missing cross-owner acceptance
cases are now implemented. This internal review is supporting evidence, not an independent external
acceptance.

Core-host instrumentation reports 43,447 of 62,001 lines (70.07%) and 14,967 of 23,883 branches
(62.67%). The separate Quartz host passes 216 tests and instruments the Quartz product project at
98.01% line and 85.36% branch coverage. These figures are reported per instrumented host rather
than merged or extrapolated into a false whole-suite percentage; repository-wide merged coverage
remains a later dedicated owner because most test projects do not yet carry the MTP coverage
provider. Source hygiene finds no C# preprocessor directives, no empty source directories, and no
dummy, stub, TODO, FIXME, or compatibility-shim marker. The remaining generic asynchronous return
comments belong to source owners not yet manually read and remain queued for their mandatory
file-by-file passes; no comment generator or bulk rewrite is used.

## Iteration 84 outcome

Make the complete MessagePack assembly a binary-owned Greenfield serialization boundary. Remove
the inherited inner Base64/object payload compatibility path while retaining the explicit outer
Base64 carrier required by text-only transports. Align serializer-independent metadata conversion,
fail fast at every owned construction and configuration boundary, preserve cancellation, and close
the whole assembly with direct coverage and mutation evidence.

## Iteration 84 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-MESSAGEPACK-ENVELOPE` | binary-only payload and immutable ownership | MessagePack envelope tests | compile-bound `byte[]` property, independent constructor/clone bytes, null metadata projection |
| `REQ-VSB-MESSAGEPACK-DESERIALIZATION` | native and overlay decoding, malformed input, cancellation | MessagePack serializer-context tests | supported/unsupported paths, exact boundaries, false for malformed payload, observable cancellation |
| `REQ-VSB-MESSAGEPACK-OBJECT-SERIALIZATION` | direct, dictionary, scalar, JSON text, binary, blank, and JSON null | MessagePack object tests | exact value/default result for every serializer-independent metadata form |
| `REQ-VSB-MESSAGEPACK-CONFIGURATION` | endpoint/bus, serializer/deserializer, both default states | MessagePack configuration tests | exact call, factory, default flag, ordering, and shared bidirectional factory |
| `REQ-VSB-MESSAGEPACK-FORMATTER-CACHE` | complete type/factory/delegate boundary | MessagePack formatter tests | null, interface, abstract, unrelated type, concurrency, failure caching, and weak-key behavior |
| `REQ-VSB-MESSAGEPACK-MESSAGE-DATA` | inline text/bytes, external reference, empty handle, and wire nil | MessagePack data tests | exact value/address/ownership and canonical empty normalization |
| `REQ-VSB-MESSAGEPACK-FORWARDING` | private snapshot, overlay, admission, and supported types | MessagePack forwarding tests | repeated owned bodies, exact overlay behavior, capacity-path survival, no legacy payload form |
| `REQ-VSB-MESSAGEPACK-SCHEDULING` | text-backed Quartz carrier | Quartz integration tests | exact binary payload, canonical outer Base64, metadata identifiers, and application header |
| `REQ-VSB-MESSAGEPACK-COVERAGE` | complete assembly host | native MTP coverage run | package line/branch rates, exact uncovered classification, no aggregate-host extrapolation |
| `REQ-VSB-SOURCE-NAVIGATION` | all fourteen production files | manual owner ledger | one primary type per file and matching root/Serialization/Formatters ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all fourteen files | manual review plus format/hygiene gates | current behavior and failure semantics only; no history, filler, or generated rewrite |

## Iteration 84 mutation obligations

- Return the caller's serialized byte array instead of a snapshot: the payload-ownership test must
  fail on reference identity before content corruption can be hidden.
- Replace JSON metadata parsing with absence/default behavior: the complete reference-input test
  must fail on the JSON object projection.
- Omit the receive-side registration from the endpoint serializer composition: both default-state
  rows must fail on the exact missing bidirectional operation.
- Accept an unrelated concrete type in the formatter cache: the complete type-boundary test must
  fail before delegate compilation.
- Ignore the retained overlay bytes when payload admission is active: the forwarding test must
  observe the original value instead of the applied replacement.
- Remove cancellation propagation from `TryGetMessage`: the cancellation contract must fail because
  the abort is converted into an ordinary unsupported/invalid result.

## Iteration 84 completion

All fourteen MessagePack production files and 1,345 physical lines were read manually in full,
together with their direct tests and the Quartz scheduling integration. The envelope now owns only
encoded `byte[]` payloads; all byte-bearing constructors and clones take defensive snapshots; and
the old inner object/Base64 compatibility branch is gone. The outer Base64 message body remains as
the explicit lossless carrier for text-only brokers and schedulers, so no transport capability was
lost. Metadata strings now use the shared JSON contract rather than being guessed as Base64.

Every formatter/cache delegate and serializer/configuration parameter has a direct exact-owner
boundary. Cancellation raised by MessagePack callbacks is unwrapped and remains observable rather
than being converted to `false`. Quartz delivery additionally proves that an application header
survives the real stored MessagePack scheduling path. Seven red executions cover the initial
cancellation defect and six isolated counterchanges; every counterchange was restored immediately.

The final MessagePack host passes 113/113 tests with no failures or skips. Its package reports
99.36% line coverage and 97.75% branch coverage. The only uncovered handwritten line sequence
points are the compiler-emitted continuations after two non-returning
`ExceptionDispatchInfo.Throw` calls. The remaining partial branches are defensive invariant guards
for a non-null JSON object/dictionary projection and a statically declared formatter table, plus a
MessagePack source-generator branch; none is represented as executed evidence.

The normal sequential Release Engineering build has zero warnings and errors. The complete
sequential Unit solution passes 5,485 tests with no failures or skips. Both repository format gates
and `git diff --check` pass. Fresh-package verification passes 18 developer journeys, 31 packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies. The packed public API
remains exactly 19,674 lines with SHA-256
`7841eea6a51d14b0dfbe8062838e5d1cacb10248b55da34ad5add0f6f0cc186d`.

The reviewed assembly has no preprocessor directives, dummy/placeholder implementation, optional
Courier/Job Service dependency, file/type/namespace mismatch, or stale construction-history
comment. Repository-wide source hygiene still reports zero C# preprocessor directives and zero
empty source directories. The complete A+ source goal remains active for the next unreviewed owner.

## Iteration 85 outcome

Review the complete initializer feature as one coherent owner: the optional
`ViciOne.ServiceBus.Initializers` API assembly and the Core initializer engine that implements its
conventions, factories, contexts, header/property initialization, property providers, and type
conversion. Preserve the separate package boundary unless the fully read dependency graph proves a
better Greenfield ownership model. Make every public and internal boundary explicit, cancellation-
correct, deterministic, testable, and discoverable; align type, namespace, filename, folder, and
comment ownership without compatibility-only API or feature loss.

## Iteration 85 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-INITIALIZER-API` | typed/runtime send, publish, request, and schedule entry points | Core initializer and contract tests | every overload, exact forwarding, null boundaries, cancellation, and unsupported-capability behavior |
| `REQ-VSB-INITIALIZER-VARIABLES` | identifier and UTC timestamp capture/reuse | variable and integration tests | explicit value, generated value, per-initialization reuse, cross-initialization isolation, and cancellation |
| `REQ-VSB-INITIALIZER-CONTEXTS` | payload, values, headers, and scoped state | context and initializer tests | exact propagation, isolation, missing value behavior, and deterministic ownership |
| `REQ-VSB-INITIALIZER-CONVENTIONS` | object, dictionary, dynamic, and registered convention selection | convention and registry tests | precedence, unsupported input, concurrency, cache behavior, and exact selected factory |
| `REQ-VSB-INITIALIZER-FACTORIES` | message/header/property factory construction | factory and contract tests | complete parameter validation, stable inspection, immutable plans, and failure boundaries |
| `REQ-VSB-INITIALIZER-CONVERTERS` | scalar, nullable, collection, dictionary, object graph, task, variable, and message-data conversion | converter tests | positive, negative, null, boundary, cancellation, and exact-type behavior |
| `REQ-VSB-INITIALIZER-PROVIDERS` | synchronous/asynchronous input and converted property values | provider tests | exact value, task completion/failure/cancellation, invocation count, and context propagation |
| `REQ-VSB-INITIALIZER-HEADERS` | copied, dictionary, provided, string, and fixed headers | header tests | normalization, overwrite semantics, invalid keys/values, exact output, and cancellation |
| `REQ-VSB-INITIALIZER-OWNERSHIP` | optional API package versus Core implementation dependency | architecture tests and manual ledger | no cycle, no hidden facade, no accidental default-glob ownership, and minimal explicit imports |
| `REQ-VSB-SOURCE-NAVIGATION` | all package and Core initializer files | architecture tests and manual ledger | one primary type per file where practical and matching type/namespace/folder ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the complete owner | manual review plus hygiene gates | current code/function semantics only; no history, filler, workaround, or generated prose |

## Iteration 85 mutation obligations

- Remove or misroute any public initializer overload: its exact forwarding, boundary, or
  cancellation test must fail.
- Reuse a variable across initialization scopes or create a new value per property: reuse and
  isolation assertions must fail.
- Change convention precedence, cache the wrong input/message pair, or accept an unsupported shape:
  convention and concurrency tests must fail.
- Skip an owned null/type/key validation or invoke a dependency before validation: exact parameter
  and zero-side-effect assertions must fail.
- Collapse nullable, collection, dictionary, task, variable, or message-data conversion into an
  apparently similar path: positive and negative partition tests must observe the semantic change.
- Invoke an asynchronous provider twice, hide its cancellation/failure, or use the wrong context:
  invocation-count and propagation tests must fail.
- Restore a broad unused global import, relocate a sibling assembly under the Core directory, or
  keep a type/file/namespace mismatch: architecture and manual ownership checks must fail.
- Retain a stale construction-history comment or replace understood semantics with generic filler:
  the manual full-source ledger must reject the exact file.

## Iteration 85 completion

All 91 Core initializer files and all nine files in the optional initializer package were read
manually in full. No generator or scripted comment rewrite was used. Every comment was checked
against the implementation while its owning file was reviewed. Generic filler and construction-
history prose were removed, and the remaining documentation describes current behavior,
boundaries, cancellation, or failure semantics.

`ViciOne.ServiceBus` remains the Core project. Persistence, Scheduling, Transports, Initializers,
and the other capability packages remain sibling projects beneath `src` because they have
independent package and dependency boundaries. Within the initializer owners, folders,
namespaces, filenames, and types now agree: optional variables live in `Variables`, the unused
optional-package global imports are gone, and implementation-only convention, cache, factory,
provider, converter, and initializer types are internal. Compatibility-only public cache
interfaces were removed without removing runtime behavior.

The implementation now preserves cancellation and dependency faults across collection, task,
variable, header, message-data, and provider paths; rejects null-returning asynchronous delegates;
uses input contexts without inventing another message-object depth; initializes concrete
dictionary object graphs; applies invariant and range-safe temporal, numeric, string, and enum
conversion; and keeps cache and provider behavior deterministic. Public extension APIs have direct
typed/runtime forwarding, validation, cancellation, probe, pipe, request, publish, send, and
schedule coverage.

The final Initializers selection passes 175/175 tests. Its unique aggregate coverage across the
Core and optional initializer source is 2,404/2,461 lines (97.68%) and 1,045/1,160 branches
(90.09%). The uncovered code is not represented as executed evidence: it is dominated by 41
defensive false-return paths in the nested property-provider factory and two double-checked cache
race paths, with a small remainder of isolated defensive null and exception branches.

Four isolated manual counterchanges were compiled and executed: increasing input-view depth,
rejecting concrete dictionary object graphs, ignoring a pre-canceled exact-array conversion, and
accepting an out-of-range Unix timestamp. Each intended test failed for the changed semantic. Each
source file was then restored manually and verified byte-for-byte against its pre-mutation SHA-256
before the final build and test runs.

The restored final Core host passes 2,879/2,879 tests, and the complete architecture host passes
292/292 tests, both without failures or skips. The sequential Release Engineering build has zero
warnings and errors. Format verification, `git diff --check`, and the requirement-projection gate
pass. Fresh-package verification passes 18 developer journeys, 31 packages, three isolated
provider-testing consumers, and all 30 runtime API assemblies. The packed public API is 19,265
lines with SHA-256 `09ca6bb86e6d74034de1689eddae0d3e35b914f93b8d6f0a52b60091f22131e4`.

Repository-wide source hygiene reports zero C# preprocessor directives and zero empty source
directories. The only textual placeholder and `NotImplementedException` matches are respectively
a real schedule-declaration placeholder in state-machine semantics and a real technical-failure
classification; neither is dummy implementation. The complete A+ source goal remains active for
the next unreviewed owner.

## Iteration 86 outcome

Review the complete Core batching owner as one coherent runtime capability: batch context and
message projection, collection and release, lifetime ownership, consumer dispatch, factory
construction, and runtime settings. Preserve batching features while making ordering, timing,
capacity, cancellation, fault, disposal, concurrency, and boundary semantics explicit and
deterministic. Align every type, namespace, filename, folder, and manually reviewed comment with
its final responsibility.

## Iteration 86 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-BATCH-CONTEXT` | batch projection over member consume contexts | context tests | identity, ordering, metadata, payload access, cancellation, and invalid boundaries |
| `REQ-VSB-BATCH-COLLECTION` | collect, capacity release, timeout release, and shutdown release | collector tests | exact membership/order, trigger precedence, no loss/duplication, and terminal state |
| `REQ-VSB-BATCH-CONCURRENCY` | simultaneous delivery and release signals | lifecycle and runtime tests | single release, race safety, stable counts, and no post-terminal mutation |
| `REQ-VSB-BATCH-TIME` | time-provider-driven delivery limits | time-provider tests | deterministic boundaries, delayed completion, cancellation, and no wall-clock dependency |
| `REQ-VSB-BATCH-DISPATCH` | consumer invocation and result propagation | integration tests | exact batch, dependency context, success, fault, and cancellation propagation |
| `REQ-VSB-BATCH-FACTORY` | consumer/factory construction and lifetime | factory and lifecycle tests | parameter validation, exact dependencies, cleanup, and repeated creation isolation |
| `REQ-VSB-BATCH-SETTINGS` | normalized runtime capacity, timeout, and concurrency | runtime-state tests | defaults, valid boundaries, invalid values, and immutable runtime snapshot |
| `REQ-VSB-SOURCE-NAVIGATION` | all eight batching files | architecture tests and manual ledger | one primary type per file where practical and matching type/namespace/folder ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the complete owner | manual review plus hygiene gates | current behavior only; no history, filler, workaround, or generated prose |

## Iteration 86 mutation obligations

- Reorder, omit, or duplicate a collected message: exact identity and ordering assertions must
  fail.
- Move a capacity or timeout boundary by one item or one time tick: boundary tests must fail.
- Allow two competing release paths to win: concurrency tests must observe duplicate dispatch or
  an invalid terminal transition.
- Replace the injected time source with wall-clock time: deterministic time-provider tests must
  fail without sleeping.
- Hide a consumer fault or cancellation, or release after shutdown incorrectly: propagation and
  lifecycle assertions must fail.
- Remove owned parameter validation or retain mutable settings: exact boundary and snapshot tests
  must fail.
- Retain a stale construction-history comment or a type/file/namespace mismatch: the manual source
  ledger and architecture checks must reject the exact file.

## Iteration 86 completion

The complete final batching API/runtime inventory and every directly affected production consumer
were read manually in full. No generator or scripted comment rewrite was used. Each comment was
checked while its implementation was understood, including batching contexts and runtime,
configuration and conventions, dependency-injection observers, in-memory outbox integration,
Job Service convention selection, JSON conversion, and the public Abstractions contracts. The
Core assembly remains `src/ViciOne.ServiceBus`; Persistence, Scheduling, Transports, Initializers,
and the other dependency-bearing capabilities remain sibling projects under `src`. Within every
reviewed project, the final namespaces, folders, filenames, and primary types express their actual
ownership.

The public collection contract is now the Greenfield `IMessageBatch<TMessage>` interface under
`Advanced`. It inherits `IReadOnlyList<ConsumeContext<TMessage>>`, exposes the standard `Count`
and indexer shape, and no longer carries the redundant legacy `Length` member. The former public
`Batch<TMessage>` contract is absent from product assemblies and the packed API. Remaining
`Batch<TMessage>(...)` source occurrences are only the intentional receive-endpoint configuration
verb. Completion mode now shares the same public capability folder, while grouping adapters are
internal implementation details under `Configuration/Consumers`.

Collection timing now starts with the first successfully admitted unique message. `FromLast`
restarts only after a later unique admission, while duplicate identifiers cannot replace the
accepted context, timestamps, log context, or admission activity. `MessageBatch<TMessage>` rejects
empty snapshots, null member contexts, undefined completion modes, and reversed timestamps before
capturing an immutable ordered copy. Runtime settings snapshot validated options, default values
are canonical across API and runtime, and the configuration callback accepts an optional endpoint
name without nullable suppression.

Connection teardown has one dedicated `BatchConsumerConnectHandle` owner. Synchronous disconnect,
`Dispose`, and `DisposeAsync` share one idempotent operation; new admissions stop before the batch
lifetime drains; synchronous and asynchronous cleanup failures remain observable; and independent
disconnect and drain failures are aggregated in owner order. The architecture gate was updated to
bind these lifecycle invariants to the extracted owner instead of the connector's former concrete
source layout. In-memory-outbox nested batch mechanics are internal and descriptively named, so no
legacy nested public runtime surface remains.

Four isolated counterchanges were compiled and executed. Moving activity capture before duplicate
admission made the duplicate-activity test fail; returning zero from `Count` made the exact batch
projection test fail; disabling `FromLast` restart made the deterministic timer test fail; and
accepting a null reference grouping key made the grouping-boundary test fail. An initial activity
counterchange survived because it changed which successfully admitted unique message owns the
delivery activity rather than allowing a duplicate to mutate state; that ambiguity was reviewed,
the intended latest-unique-admission behavior was retained, and the precise duplicate mutation was
then killed. Every mutation was restored manually. Final SHA-256 values are
`9a46e174c4a7bd38c2294b31e615fd82d829fc10a60a0fe579e96e186a032650` for `MessageBatch.cs`,
`27cfbf7b5eb3c489d9d2c66ef7fb4cd0ba96f997128bb1e105ae409bb9892777` for `BatchConsumer.cs`,
and `5bcd12bcc51dd798dddcc5e7909a7e8d35cac73bfcf57708e9a0d029463722ee` for
`GroupKeyProvider.cs`.

The restored final Batching selection passes 70/70 tests, the direct Abstractions selection passes
64/64, the direct `BatchOptions` selection passes 38/38, and the strengthened API architecture
test passes. Unique executable coverage across the Core and Abstractions batching owner is
757/772 lines (98.06%) and 239/266 branches (89.85%). `BatchOptions`, both grouping adapters,
`BatchEndpointExtensions`, `MessageBatch`, runtime settings, the consumer factory, configurator,
connect-handle primary logic, and the connector factory each have complete line coverage; the
public option and grouping contracts also have complete branch coverage. Remaining misses are
defensive activation, executor-race, and terminal cleanup paths and are not represented as
executed evidence.

The final Core host passes 2,903/2,903 tests and the complete architecture host passes 292/292,
both without failures or skips. The official serial Unit solution passes 5,653/5,653 tests across
22 hosts with no failures or skips. The Release Engineering solution builds with zero warnings and
errors. Both repository format gates pass; the Engineering gate inspected 5,733 files and changed
none. `git diff --check` passes, as do requirement projection, bidirectional async naming, source
layout, source-comment, and public-API gates through the complete architecture host.

Fresh-package verification covers 18 developer journeys, 31 packages, three isolated provider-
testing consumers, and all 30 runtime API assemblies. The packed public API contains 19,222 lines
with SHA-256 `05a971207d33b8476a17cf9217376969c9cccc37aa0e43449f5607ee36f7c615`.
Repository-wide source hygiene reports zero C# preprocessor directives and zero empty source
directories. The broad marker scan contains only real temporary-endpoint semantics, the real
state-machine schedule declaration placeholder, and `NotImplementedException` as a technical
failure classification; the architecture hygiene gates find no dummy or placeholder
implementation. Protected `review/` and `TestResults/` contents were neither changed nor staged.
The complete A+ source goal remains active for the next unreviewed owner.

## Iteration 87 outcome

Review the complete `ViciOne.ServiceBus.Courier` capability as one coherent owner: public routing-
slip contracts and builder APIs, activity execution and compensation, lifecycle-event routing,
request/response proxies, dependency-injection registration, middleware, serialization boundaries,
and transport dispatch. Preserve every routing-slip feature while making the public model immutable,
the received wire model validated, cancellation and disposal observable, configuration deterministic,
and every runtime boundary explicit. Retain `src/ViciOne.ServiceBus.Courier` as a sibling project of
the Core assembly; within it, align every type, namespace, filename, folder, and manually reviewed
comment with its final responsibility.

## Iteration 87 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-COURIER-CONTRACTS` | itinerary, logs, failures, variables, subscriptions, and lifecycle events | contract and serialization tests | immutable collection surface, exact values, transport round trip, malformed input rejection, and caller-mutation isolation |
| `REQ-VSB-COURIER-BUILDER` | activities, variables, subscriptions, source itinerary, and snapshots | builder tests | every overload, null/empty/flag boundaries, atomic updates, injected clock, repeatable builds, and detached state |
| `REQ-VSB-COURIER-EXECUTION` | empty, active, completed, revised, terminated, faulted, and compensated routing slips | executor and lifecycle tests | exact destination, event sequence, state evolution, cancellation, failure propagation, and no duplicate terminal event |
| `REQ-VSB-COURIER-EVENTS` | topology publication, explicit subscriptions, supplemental delivery, activity filters, and selected contents | event publisher tests | every event/contents flag, ordering, custom envelope, invalid boundary, cancellation, and isolated message state |
| `REQ-VSB-COURIER-ACTIVITIES` | constructor, delegate, and dependency-injection factories | factory and middleware tests | exact instance/context, null return, sync/async disposal, observer sequence, fault, cancellation, and continuation behavior |
| `REQ-VSB-COURIER-REGISTRATION` | typed/runtime/scanned registration and endpoint ownership | registration and host tests | full signature matching, namespace filters, definitions, companion endpoints, duplicate registration, and invalid types |
| `REQ-VSB-COURIER-REQUESTS` | request metadata, completion response, declared fault, standard fault, and retry | request tests | exact metadata, injected time, missing-state diagnostics, retry count/delay, cancellation, and null-response rejection |
| `REQ-VSB-COURIER-ACCESSORS` | typed arguments, data, and variables across all lifecycle contexts | accessor tests | reference/value types, defaults, precedence, null context, invalid key, missing dictionaries, and conversion failures |
| `REQ-VSB-COURIER-OBSERVABILITY` | tracing, metrics, probe, consumed/faulted notification | host and observability tests | exact tags, duration source, observer order, owned cancellation classification, and probe shape |
| `REQ-VSB-SOURCE-NAVIGATION` | all 135 Courier production files | architecture tests and manual ledger | project boundary retained; one primary type per file where practical; exact type/namespace/folder ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the complete Courier owner | manual review plus hygiene gates | current code and behavior only; no history, filler, workaround, or generated prose |

## Iteration 87 mutation obligations

- Make a routing-slip or lifecycle-event collection mutable, retain a caller-owned collection, or
  skip received-state validation: immutability, mutation-isolation, and malformed-wire tests must
  fail.
- Change activity/variable precedence, remove a selected lifecycle-event payload, or publish when a
  non-supplemental subscription owns delivery: exact contract assertions must fail.
- Route execution or compensation to the wrong endpoint, skip a state transition, or emit a terminal
  event twice: lifecycle ordering and count assertions must fail.
- Drop cancellation, replace the injected clock with wall time, hide an activity/disposal failure,
  or continue the pipeline after a failed boundary: propagation and zero-side-effect assertions must
  fail.
- Accept a mismatched runtime activity/definition type, lose a companion endpoint, or scan outside
  the requested namespace: registration-matrix assertions must fail.
- Remove any typed event accessor's context/key validation or change variable-over-argument
  precedence: complete accessor boundary tests must fail.
- Retain a stale construction-history comment or a type/file/namespace mismatch: the manual source
  ledger and architecture checks must reject the exact file.

## Iteration 87 completion

All 135 Courier production files and their 9,374 lines were read manually in full together with
the existing direct tests. No generator or scripted comment rewrite was used. Every comment was
reviewed while its implementation was understood and was retained or rewritten only when it
described current code or behavior. The assembly boundary is the primary source-layout rule:
`src/ViciOne.ServiceBus.Courier` remains an independent sibling of the Core project
`src/ViciOne.ServiceBus`, while provider integrations remain grouped by capability under
`Persistence`, `Scheduling`, and `Transports`. Within Courier, context types, advanced API types,
root-namespace types, filenames, and folders now express their actual ownership. There are no C#
files directly under the `src` root and no empty source directories.

Routing-slip and lifecycle-event contracts now expose read-only collections backed by detached
snapshots. Received routing-slip state is validated and isolated before execution. Typed revised-
event variable access is complete for reference and value types. Event publication preserves
subscription ownership and supplemental-delivery semantics without exposing mutable state.
Builder, host, request-proxy, and executor boundaries reject invalid values before side effects.
Request responses and routing-slip subscriptions retain their distinct endpoint addresses, and
executor timestamps use the injected `TimeProvider`.

Activity factories, scope providers, middleware, and dependency-injection registration now have
explicit lifecycle ownership. Scope cleanup attempts both ambient-context restoration and scope
disposal, keeps the original failure ordering, and aggregates independent failures instead of
leaking a scope. Registration validates runtime activity and definition compatibility and retains
companion endpoint ownership. The Core consume-scope provider no longer leaves an ambient context
behind when scope creation or cleanup fails. Futures consumes the same read-only Courier variable
contract without restoring a mutable compatibility surface.

Four isolated counterchanges were compiled and executed. Returning a caller-owned routing-slip
collection made the mutation-isolation test fail. Sending a subscription to the request-response
address made the two-address request-proxy test fail. Restoring ambient context before protected
scope disposal made the dual-failure cleanup test observe an undisposed scope. Replacing the
injected clock with `TimeProvider.System` made the deterministic executor timestamp test fail.
Every mutation was restored manually and byte-for-byte verification produced final SHA-256 values
`994189b32c22cdd03732c4adbf8c07e28219c08e0470ccc1f0fc231c145434db` for
`RoutingSlipMessageState.cs`, `09093dd430022a00e47721a885eb85b64d00b6d6b3cf149bd83ff6d9bddc683b`
for `RoutingSlipRequestProxy.cs`, `661ecc35d7448e2eadda8173ee7cbbf79df9a466521467934bb9523d8088c1eb`
for `ActivityScopeDisposal.cs`, and
`aaeabbb04356e39f3b18cf648ec1adb7e4ee792f1aedf23ca3b7db8da894bd8d` for
`RoutingSlipExecutor.cs`.

The final direct Courier selection passes 150/150 tests. Courier coverage increased from 69.02%
lines and 48.62% branches to 84.33% lines and 70.64% branches. These are measured values rather
than a claim of complete execution; unexecuted paths remain visible for later risk-directed work.
The final Core host passes 2,968/2,968 tests and the complete architecture host passes 292/292,
both without failures or skips. The official serial Unit solution passes 5,718/5,718 tests across
22 hosts with no failures or skips. The final Release Engineering solution builds with zero
warnings and errors. Both repository format gates pass without changes, and requirement
projection plus the complete bidirectional async naming gate pass on the final names and manifest.

Fresh-package verification passes 18 developer journeys using 31 freshly packed ViciOne packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies. The intentional
Greenfield API changes were reviewed before updating the contract: Courier collection contracts
are read-only, revised-event typed accessors are complete, and the corresponding Futures helpers
accept read-only variables. The packed public API contains 19,224 lines with SHA-256
`47b29f2bdc942446f27c7f5e9597e062d6add4ad16d5af941273047e65b5343c`.

`git diff --check`, JSON validation, both format gates, source-layout checks, comment hygiene, and
the complete architecture suite pass. Repository-wide product source contains zero C#
preprocessor directives. The broad marker scan contains only the real state-machine schedule
declaration placeholder semantics and `NotImplementedException` as a retry failure
classification; neither is a dummy implementation. Protected `review/` and `TestResults/`
contents were neither changed nor staged. The complete A+ source goal remains active for the next
unreviewed owner.

## Iteration 88 outcome

Review the complete `ViciOne.ServiceBus.Futures` capability as one coherent owner: durable state
and stored messages, command correlation, pending request and routing-slip execution, terminal
result and fault publication, subscriber replay, definitions, discovery, registration, and
persistence-facing contracts. Preserve every future feature while removing dispatch/tracking race
windows, configuration-order dependence, mutable stored-message aliases, incomplete cancellation,
and delayed-fault state inconsistencies. Retain Futures as an independent feature assembly beneath
`src`; align every type, filename, namespace, folder, and manually reviewed comment with its final
responsibility.

## Iteration 88 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-FUTURE-STATE-PERSISTENCE` | command, pending identifiers, subscriptions, variables, results, faults, and concurrency metadata | state and message contract tests plus provider integration tests | non-null rehydration, required values, comparer semantics, detached inputs, exact serialization, and provider round trip |
| `REQ-VSB-FUTURE-REQUEST-DISPATCH` | publish, fixed send, context-selected send, one request, and request ranges | request contract and integration tests | exact destination, pending-before-dispatch, duplicate rejection, rollback on failed dispatch, deterministic range ordering, cancellation, and null boundaries |
| `REQ-VSB-FUTURE-ROUTING-SLIP` | callback-built and container-planned itineraries | routing-slip contract and persistence integration tests | FutureId, subscription, tracking-before-dispatch, failed-dispatch rollback, callback cancellation, terminal result, terminal fault, and configuration-order independence |
| `REQ-VSB-FUTURE-TERMINATION` | immediate fault, deferred fault, all-completed result, and durable replay | state-machine and batch integration tests | exact transition timing, no premature terminal state, late subscriber acceptance, one terminal outcome, and no repeated child work |
| `REQ-VSB-FUTURE-SUBSCRIPTIONS` | response addresses with and without request identifiers | subscription contract tests | value equality, duplicate suppression, detached enumeration, request-id propagation, fan-out, cancellation, and send failure |
| `REQ-VSB-FUTURE-VARIABLES` | synchronous and asynchronous values from typed events and state | variable extension tests | all overloads, case-insensitive lookup, null/empty keys, null factories/results, cancellation, replacement, and conversion failure |
| `REQ-VSB-FUTURE-CONFIGURATION` | request, response, result, fault, routing-slip, and definition configuration | configuration contract tests | every overload, invalid callbacks, mutually required choices, repeated calls, endpoint settings, and exact validation diagnostics |
| `REQ-VSB-FUTURE-REGISTRATION` | typed, runtime, assembly, explicit-type, namespace, and request-consumer registration | registration boundary and host tests | exact definition association, filter scope, invalid/abstract/open types, null elements, duplicate registration, endpoint ownership, and repository requirement |
| `REQ-VSB-FUTURE-API` | complete public Futures surface | architecture and reflection contract tests | Greenfield names, cancellation placement, read-only stored-message exposure, no legacy aliases, and intentional package boundary |
| `REQ-VSB-SOURCE-NAVIGATION` | all 55 Futures production files | architecture tests and manual ledger | independent project retained; one primary type per file where practical; exact type/namespace/folder ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 4,430 Futures source lines | manual review plus hygiene gates | current code and behavior only; no history, filler, workaround, or generated prose |

## Iteration 88 mutation obligations

- Move pending registration after request or routing-slip dispatch, retain a failed-dispatch
  identifier, or allow duplicate pending identifiers: exact observation and rollback assertions
  must fail.
- Replace sequential range state mutation with unsynchronized concurrent mutation, change input
  order, or drop cancellation: deterministic dispatch assertions must fail.
- Replace callback cancellation with a default token or make routing-slip tracking depend on
  configuration order: callback and state assertions must fail.
- Transition a deferred-fault future before all tracked work terminates, accept a duplicate command
  as a terminal replay too early, or publish more than one terminal message: lifecycle integration
  assertions must fail.
- Retain caller-owned stored-message or persisted collection instances, accept invalid contract
  names, or lose case-insensitive durable variables: snapshot and rehydration assertions must fail.
- Accept an invalid runtime future/definition type or scan beyond the requested namespace: complete
  registration-matrix assertions must fail.
- Retain a stale construction-history comment or a type/file/namespace mismatch: the manual source
  ledger and architecture checks must reject the exact file.

## Iteration 88 completion

All 55 Futures production files and their original 4,430 lines were read manually in full together
with the directly affected tests. No generator or scripted comment rewrite was used. Every comment
was checked against the implementation while that file was understood, and comments were retained
or corrected only when they described current code or behavior. Futures remains an independent
feature assembly at `src/ViciOne.ServiceBus.Futures`; `src` is the project/assembly list, while
`src/ViciOne.ServiceBus` is the Core project. Persistence, Scheduling, and Transports continue to
group provider assemblies by capability. Within Futures, all primary types, namespaces, filenames,
and folders match their final ownership, and no additional visual wrapper directory is warranted.

Stored future messages and persisted collection inputs now use detached, read-only snapshots.
Message URNs, future locations, subscriptions, request identifiers, runtime registration types, and
assembly/type scans reject malformed or ambiguous inputs before side effects. The registration API
now distinguishes explicit types (`AddFutures`), explicit assemblies (`AddFuturesFromAssemblies`),
and the loaded-assembly convenience (`AddFuturesFromLoadedAssemblies`) without overload ambiguity.
The unused internal runtime `RegisterFuture` path was removed after static reference inspection
confirmed that no product or test caller existed; no behavior or public API was removed.

Pending request and routing-slip identifiers are now registered before transport dispatch and are
rolled back when dispatch fails. Duplicate and empty identifiers are rejected deterministically,
request ranges preserve input order without unsynchronized state mutation, and routing-slip
tracking is independent of configuration order. Callback, dispatch, terminal publication, and
pending-completion paths forward their caller cancellation tokens. Result/fault factories finish
successfully before terminal state mutation, terminal publication failure rolls state back, and a
deferred fault accepts further subscribers until every tracked operation has terminated. Variable,
result, fault, and routing-slip binder names now satisfy the complete bidirectional async contract.

The new direct contract matrix covers stored-state isolation, message URNs and round trips,
locations and subscriptions, request dispatch and rollback, routing-slip configuration and
tracking, all terminal configurator shapes, result/fault lifecycle behavior, variable conversion,
consumer kinds, registration boundaries, and the default routing-slip fault mapping. Four shallow
legacy registration cases and their three requirement projections were deleted only after the new
matrix fully superseded them. The final Futures selection passes 106/106 tests. Futures coverage
increased from 58.86% lines and 49.77% branches to 90.62% lines and 85.48% branches. The final
method-level risk calculation covers 376 methods and reports zero CRAP scores above 30; the former
unexecuted default routing-slip fault mapper now has 100% line coverage.

Seven isolated counterchanges were compiled and executed. Moving pending registration after
dispatch, dropping routing-slip tracking propagation, making deferred fault transition
unconditional, returning a caller-owned stored-message dictionary, removing terminal-result
rollback, mutating state before invoking a result factory, and dropping consume cancellation from
pending completion each made its precise regression test fail. Every mutation was restored
manually. Byte-for-byte verification produced final SHA-256 values
`cbc23f9914735fc7d83c6a331409a8304ac57801baca6bfebc51f807d7d2d27e` for `FutureRequest.cs`,
`8350e06e5d5c0acd416dcaab09701062bf4205bae6244b81d66b577067eade99` for
`FutureRoutingSlipConfigurator.cs`, `f8c5ac8a2861cabd95a3cb4a5ee88e3bcc44bd3dd240ffb430a9af3a2dc511d5`
for `FutureFault.cs`, `1e0e8387c67fe60acbe41dfff7df6d45f20119698dd4685fc9e880329401a6fd`
for `FutureMessage.cs`, `3d7d77dfd6a2ce95748cbae5b078977f5aee9692a80d9fee42a6b0b58ff99a4a`
for `FutureResult.cs`, and `bf616c5391d5221b0ce076f37e0fc4cca8cfcead5305e97a19145f1d0e32140e`
for `FutureStateExtensions.cs`.

The final Unit and Engineering solutions build in Release with zero warnings and errors. The
official serial Unit solution passes 5,790/5,790 tests across 22 hosts with zero failures and zero
skips; this includes the complete architecture, source-layout, comment-hygiene, requirement-
projection, and bidirectional async gates. Both repository format gates pass without changes.
Azure Table and Entity Framework Core local-integration projects compile against the changed
Futures callback contract; real Azure Table cloud acceptance was already established in iteration
49 and is not falsely represented as a new cloud run in this iteration.

Fresh-package verification passes 18 developer journeys using 31 freshly packed ViciOne packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies. The intentional
Greenfield Futures API changes were reviewed before updating the contract. The packed public API
contains 19,225 lines with SHA-256
`74bc8ccbe3fd160506783a365a85ba9ebf13f5657482edacaf75d54167cbf441`.
`git diff --check` and CoreRequirements JSON validation pass. Repository-wide product source has
zero C# preprocessor directives and zero empty source directories. The broad marker scan contains
only explicit unsupported-operation contracts, real state-machine schedule-declaration placeholder
semantics, and `NotImplementedException` as a retry classification; none is a dummy implementation.
Protected `review/` and `TestResults/` contents were neither changed nor staged. The complete A+
source goal remains active for the next unreviewed owner.

## Iteration 89 outcome

Review the complete `ViciOne.ServiceBus.Mediator` capability as one coherent owner: direct and
container construction, send and publish dispatch, request/response routing, observer ownership,
message-body materialization, dependency-injection scope preservation, request-handler adapters,
and the public test harness. Preserve all mediator behavior while removing inherited factory and
host-builder compatibility entry points, closing observer and resource-lifetime defects, enforcing
configuration and address boundaries before side effects, and making every API shape directly
testable. Retain Mediator as an independent feature assembly directly beneath `src`; do not nest
it inside the Core project merely for visual uniformity.

## Iteration 89 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-MEDIATOR-FACTORY` | default/custom address, clock, limits, and construction boundaries | factory contract tests | exact endpoint addresses, pre-callback validation, required limits, and one explicit factory entry point |
| `REQ-VSB-MEDIATOR-CONFIGURATION` | limits and materialization callback ownership | factory and dispatch tests | single declaration, deterministic diagnostics, one callback invocation, and no partial service registration |
| `REQ-VSB-MEDIATOR-OBSERVERS` | configuration-time and runtime consume/send/publish observers | observer contract tests | primary/response consumption, strict send/publish isolation, disconnect, exact fault, and observer-fault containment |
| `REQ-VSB-MEDIATOR-ADVANCED-SEND` | typed, runtime, initializer, and pipe send forms | advanced API tests | every overload, exact contract, exact pipe invocation, null boundaries, cancellation, and endpoint resolution |
| `REQ-VSB-MEDIATOR-ADVANCED-PUBLISH` | typed, runtime, initializer, and pipe publish forms | advanced API tests | every overload, publication semantics, exact pipe invocation, null boundaries, and observer isolation |
| `REQ-VSB-MEDIATOR-REQUEST-API` | direct and consume-context request handles and clients | request API tests | message/initializer/address/client matrix, correlation metadata, scope propagation, null boundaries, and cancellation |
| `REQ-VSB-SCOPED-MEDIATOR` | scoped publish, request, client factory, and connectors | scoped mediator tests | all public forms execute inside one DI scope, thread-safe lazy context, exact routes, and connector stability |
| `REQ-VSB-MEDIATOR-BODY` | canonical bounded JSON materialization | body serializer, receive-context, and dispatch tests | exact bytes, owned buffer, exact limit, overflow, unsupported stream operations, cancellation, and logical diagnostic address |
| `REQ-VSB-MEDIATOR-SERIALIZATION-CONTEXT` | typed/runtime projection and unsupported transport serialization | serialization-context tests | identity-preserving projections, required types, dictionary conversion, and consistent unsupported-operation failures |
| `REQ-VSB-MEDIATOR-RECEIVE-CONTEXT` | metadata, attached work, delivery/fault state, and notifications | receive-context tests | exact body/address/providers, dispatch waits for attached work, required arguments, cancellation-before-state, and terminal flags |
| `REQ-VSB-MEDIATOR-REQUEST-HANDLER` | one-way and request/response handler adapters | request-handler tests | exact message, cancellation, typed response, null context, and explicit null-response failure |
| `REQ-VSB-TEST-HARNESS-MEDIATOR` | observation and owned asynchronous lifetime | harness behavior tests | request/response evidence, exact failure, observer cleanup, mediator disposal, base cleanup, and idempotence |
| `REQ-VSB-SOURCE-NAVIGATION` | all 29 original Mediator source files | architecture tests and manual ledger | independent project boundary, final 28-file owner, matching type/namespace/file/folder responsibility, and no empty directory |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 3,082 original Mediator source lines | manual review plus documentation gates | current code and behavior only; no history, filler, generic placeholder text, or generated rewrite |

## Iteration 89 mutation obligations

- Disconnect configuration-time consume observers from the materialized runtime: the exact
  configuration-observer test must fail with no recorded consume stages.
- Route publications through send observers: the strict observer-isolation test must fail by
  recording the publication as a send.
- Report the physical mediator endpoint instead of an addressed request's logical destination on
  body admission failure: the logical-address regression test must fail on the exact URI.
- Allow a request-response handler to return `null`: the handler contract must fail because the
  explicit handler diagnostic was replaced by a downstream argument failure.
- Skip asynchronous mediator disposal in the test harness: the lifetime test must fail because a
  request client can still be created through the leaked mediator.

## Iteration 89 completion

All 29 original Mediator production files and their 3,082 lines were read manually in full together
with every direct test and the affected test-harness implementation. No generator or scripted
comment rewrite was used. Every comment was checked against the implementation while the file was
understood. Two generic return descriptions found by the repository documentation gate were then
rewritten to state the exact one-way handler task contract. The final Mediator owner contains 28
C# files and 2,730 lines because two compatibility adapter files were removed and one explicit
factory file was added.

The physical layout follows assembly ownership rather than visual nesting. `src` is the list of
independent projects; `src/ViciOne.ServiceBus` is specifically the Core project and is not a wrapper
for the other assemblies. Mediator therefore remains at `src/ViciOne.ServiceBus.Mediator`.
Persistence, Scheduling, and Transports remain family folders for interchangeable integration
projects. Nesting Mediator beneath the Core project would misstate ownership and expose it to SDK
default compile globs. Every remaining Mediator filename, namespace, type, and folder matches its
responsibility, and no empty directory remains.

`MediatorFactory.Create` is now the single direct construction entry point. The inherited
`Bus.Factory.CreateMediator` adapter and host-builder `UseMediator` compatibility surface were
removed; dependency-injection construction remains the conventional `AddMediator` entry point.
Base addresses are validated as absolute loopback addresses before callbacks or service-collection
mutation. Direct and container configuration reject duplicate limits and duplicate materialization
callbacks. The advanced send and publish overload-hiding relationship is owned by the shared
Abstractions contracts and remains explicitly recorded for that owner rather than being hidden by
a Mediator-only compatibility layer.

Configuration-time observers now participate in the materialized runtime. Publish observers are
strictly isolated from send observers, receive observers cover both primary and response
dispatchers, and observer cleanup is owned by the mediator. Addressed endpoints no longer create an
unbounded URI cache, admission failures retain the logical destination, and the bounded body stream
rejects invalid ranges, repeated completion, cancellation, and post-completion writes. Receive and
serialization contexts validate every required runtime boundary and expose canonical owned JSON.

Scoped mediator client-factory creation is lazy and thread-safe. All contextual and non-contextual
request handle/client forms and every scoped publish form preserve the calling dependency-injection
scope and exact route. Request-response handlers reject a `null` response with a handler-specific
diagnostic. `MediatorTestHarness` now owns observer handles and the mediator's asynchronous lifetime,
attempts every cleanup path, aggregates independent failures, and is idempotent.

The direct Mediator selection passes 86/86 tests. Package coverage increased from 59.90% lines and
46.97% branches to 88.46% lines and 75.48% branches. The method-level calculation covers 267
methods and reports zero CRAP scores above 10. Remaining unexecuted lines are visible defensive
registration delegation, rare concurrent cleanup, and compiler-generated state-machine paths and
are not represented as executed evidence.

Five isolated counterchanges were compiled and executed. Losing configured consume observers,
mixing publish dispatch into send observers, using the physical endpoint for addressed admission
failure, permitting a null handler response, and leaking the harness-owned mediator each made its
precise regression test fail. Every mutation was restored manually. Final SHA-256 values are
`b2b00e6339be67ae86e1f435f030d15183b3e6954cb50071ff38f365e6690d76` for
`MediatorFactory.cs`, `022f62c0fa0990d68f0b7499e6b715c812ef2fa3c8a3f98952d3bfb2bcb0354e`
for `MediatorSendEndpoint.cs`, `e5ca5266da8197d43fc1f0fc011d3afae8851fa4aa9d3e15d4329c15a87a54cb`
for `MediatorRequestHandler.cs`, and
`5363b3f2430dcf1473984b598b25f70c116556dc84a469c73cdcae3ec8c35ad9` for
`MediatorTestHarness.cs`.

The final Release Engineering solution builds with zero warnings and errors. Both repository
format gates pass without changes. The official serial Unit solution passes 5,840/5,840 tests
across 22 hosts with zero failures and zero skips; this includes all 292 architecture tests, the
complete bidirectional async naming gate, source layout, comment hygiene, public documentation, and
the 42 new requirement projections.

Fresh-package verification passes 18 developer journeys using 31 freshly packed ViciOne packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies. The intentional API
diff contains only the single direct factory, removal of the two compatibility adapter types and
their four methods, and the harness's truthful `IAsyncDisposable` contract. The packed public API
contains 19,222 lines with SHA-256
`92338a749f24cbd843a1bb74359acd423948970efff88ab7e727e9317a2a3103`.
`git diff --check` and CoreRequirements JSON validation pass. Repository-wide product source has
zero C# preprocessor directives and zero empty source directories. The protected `review/` and
`TestResults/` trees were neither changed nor staged. The complete A+ source goal remains active
for the next unreviewed owner.

## Iteration 90 outcome

Review the complete `ViciOne.ServiceBus.Quartz` capability as one coherent scheduling-adapter
owner: direct and dependency-injection composition, scheduler identity and lifecycle, one-time and
recurring commands, persisted trigger data, message reconstruction, delivery, cancellation,
pausing, resuming, retry classification, and the durable Quartz acceptance profile. Preserve all
scheduling features while rejecting malformed commands before scheduler access, making persisted
data unambiguous, and closing every bus-owned resource lifetime.

Keep the project at `src/Scheduling/ViciOne.ServiceBus.Quartz`. The `Scheduling` directory is the
family boundary for interchangeable scheduling integrations, whereas `src/ViciOne.ServiceBus` is
the Core project itself and is not a container for other assemblies. This physical layout therefore
matches assembly ownership, dependency direction, namespace responsibility, and the corresponding
test location.

## Iteration 90 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-QUARTZ-PUBLIC-API` | greenfield scheduling composition and lease surface | configuration contract tests | exact four-type public API, seven composition methods, null boundaries, and no compatibility facade |
| `REQ-VSB-QUARTZ-ONE-TIME` / `REQ-VSB-QUARTZ-RECURRING-CONTROL` | one-time, recurring, cancel, pause, and resume inputs | command validation tests | every invalid member fails with its exact category before scheduler-factory access |
| `REQ-VSB-QUARTZ-JOB-DATA` | content type, body, message types, metadata, and transport properties | serialization, codec, context, and job tests | serializer-owned content type, lossless JSON types, fail-closed decoding, and immutable reconstruction snapshot |
| `REQ-VSB-QUARTZ-TRIGGER-KEY` | one-time and recurring Quartz identities | trigger-key tests | non-empty token, identifiers, groups, namespaces, stable escaping, and collision resistance |
| `REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP` | direct observer, partitioner, scheduler, and factory lifetime | lease, configuration, and lifecycle tests | complete idempotent cleanup, caller ownership, exact single failure, and aggregate independent failures |
| `REQ-VSB-QUARTZ-MULTIBUS` | bus and scheduler namespace isolation | configuration and lifecycle tests | assembly-stable bus identity, same-FQN distinction, unique claims, and typed registration |
| `REQ-VSB-QUARTZ-DELIVERY` | scheduled send reconstruction and terminal invalid data | job and send-pipe tests | required absolute address and type list, exact body/content type, cancellation, retry, and unscheduling |
| `REQ-VSB-SOURCE-NAVIGATION` | all 29 original Quartz production files | architecture tests and manual ledger | final 30-file owner, matching type/namespace/file/folder responsibility, and no empty directory |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 2,654 original Quartz source lines | manual review plus documentation gates | current code and behavior only; exact completion and ownership semantics; no history or filler |

## Iteration 90 mutation obligations

- Persist the inbound receive content type instead of the selected serializer's content type: the
  serialization-boundary test must fail with both exact media types.
- Restore the inherited semicolon-delimited message-type representation: the scheduling-boundary
  test must fail when a valid identifier itself contains a semicolon.
- Resolve the scheduler before validating a one-time command: all seven invalid variants must fail
  because the forbidden scheduler access replaces the intended diagnostic.
- Omit disposal of the lifecycle observer from the direct lease: the lease ownership test must
  observe zero disconnections instead of exactly one.
- Reconstruct headers lazily from mutable Quartz job data: the complete metadata snapshot test must
  observe the post-construction mutation.
- Use only the bus type's full name as scheduler owner identity: the typed-registration test must
  reject the assembly-ambiguous key.
- Accept an empty one-time token when forming a trigger key: the empty-token contract test must fail.

## Iteration 90 completion

All 29 original Quartz production files and their 2,654 lines were read manually in full together
with the project file, all comments, the complete public surface, the owning tests, and the
requirements manifest. No generator or scripted comment rewrite was used. Each comment was checked
while its implementation was understood. The final project contains 30 C# files and 2,786 lines;
the added internal `QuartzMessageTypeList` owns the single persisted JSON representation. Every
remaining filename, namespace, type, and directory matches its responsibility, and no empty source
directory remains.

Scheduling commands now validate tokens, absolute destinations, payloads, message-type identifiers,
schedule identifiers, cron data, time zones, time ranges, and misfire policies before serialization
or scheduler access. Persisted content type comes from the serializer that produced the body.
Message-type identifiers are stored as JSON rather than an ambiguous delimiter string, so valid
identifiers containing semicolons or quotes round-trip exactly and malformed stored data is terminal.
Cancellation, pause, and resume likewise validate trigger identities before resolving a scheduler.

Scheduled-message contexts snapshot standard metadata, user headers, Quartz fire metadata, and
transport properties at construction. Destination and supported message types are revalidated at
the delivery boundary. Direct leases now own the bus lifecycle-observer handle as well as the
partitioner and any adapter-owned scheduler factory; cleanup attempts every resource, preserves one
failure, aggregates independent failures, and remains idempotent. Configuration failure releases
already-created resources through small dedicated helpers. Scheduler ownership uses an
assembly-stable bus identity, preventing equal fully qualified type names from different assemblies
from colliding.

The focused profile grew from 216 to 267 tests and passes 267/267 with zero failures and zero skips.
Fresh package coverage is 97.75% lines and 86.12% branches across 202 instrumented methods, with no
CRAP score above 30. Extracting direct-configuration cleanup reduced that method's complexity from
26 to 18 and its CRAP score from 34.67 to 18.01. Unexecuted lines remain defensive cleanup-failure
paths and are not reported as executed evidence.

Seven isolated counterchanges were compiled and executed. Wrong content-type ownership, delimiter
storage, premature scheduler access, observer leakage, lazy header reconstruction, assembly-
ambiguous bus identity, and an empty scheduling token each made its precise regression test fail.
Every counterchange was restored manually before the final build. Final SHA-256 values are
`788fd94a01b204b3ae86ee243c3b95c604b5e2d8a9be2c09636f3acd4c77232a` for
`ScheduleMessageConsumer.cs`, `0f32a6ca76cfca1a29c6be961337db7840839554a32dadb71dd8aed4e291f237`
for `QuartzSchedulerLease.cs`, `2172882d92f9980414ad432fe8ef7751f686b690e69f24bad17acd67a401a107`
for `QuartzSchedulingExtensions.cs`, `8ce1d85a9ed88963b87667967ef4a5d49bb688bb9215b5bc2b0385deea76b655`
for `QuartzMessageTypeList.cs`, `840ff29f081c6d581637b99a62dc52aa006d0276b445fad366e5f6248e61cef9`
for `QuartzScheduledMessageContext.cs`, `104afeeeeda54e52c7478ebc8c43085799a80ab27f1031a876e3e7b44e052a9c`
for `QuartzSchedulerBinding.cs`, and
`0407a5a570ba43ad8a5471c71165be924300dd9753d58e4f2d03d6c8542e80c6`
for `QuartzTriggerKey.cs`.

The final serial Release Unit/Architecture solution builds with zero warnings and errors and passes
5,891/5,891 tests across 22 hosts with zero failures and zero skips. This includes the complete
architecture, bidirectional async naming, source layout, comment hygiene, documentation, and
requirement-projection gates. Whitespace formatting, warning-level style, requirements JSON, Git
whitespace, C# preprocessor, and empty-directory checks pass.

Fresh-package verification passes 18 developer journeys using 31 freshly packed ViciOne packages,
three executed isolated provider-testing consumers, and all 30 runtime API assemblies. Quartz adds
only an internal codec, so the packed public API remains unchanged at 19,222 lines with SHA-256
`92338a749f24cbd843a1bb74359acd423948970efff88ab7e727e9317a2a3103`. The protected `review/` and
`TestResults/` trees were neither changed nor staged. The complete A+ source goal remains active for
the next unreviewed owner.

## Iteration 91 outcome

Review the complete `ViciOne.ServiceBus.SignalR` integration as one coherent transport-adjacent
backplane owner: public composition, endpoint identity, local connection and subscription state,
cross-node broadcast/connection/group/user delivery, acknowledged group membership, protocol
serialization, client invocation results and cancellation, dependency-injection scope ownership,
logging, and malformed-contract rejection. Preserve SignalR scale-out behavior while replacing the
legacy public implementation surface with one minimal Greenfield composition API and completing the
modern .NET SignalR lifetime-manager contract.

Move the independent project to `src/Transports/ViciOne.ServiceBus.SignalR` and its tests to the
matching `tests/Transports` family because the package adapts ViciOne.ServiceBus as a SignalR
backplane. `src/ViciOne.ServiceBus` remains the Core project itself, not a container for sibling
assemblies. Other independent Core, feature, and tooling packages remain direct children of `src`;
family folders are used only where several interchangeable providers or integrations share one
architectural axis. This avoids false ownership and the SDK's recursive default compile globs.

## Iteration 91 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SIGNALR-PUBLIC-API` | composition and timeout configuration | configuration tests | exact two-type public API, fluent return, custom/default timeout, duplicate-hub rejection, null boundary, and no partial registration |
| `REQ-VSB-SIGNALR-LOCAL` | connection, group, user, broadcast, disconnect, and failure logging | local lifetime-manager tests | exact recipients, exclusions, ordinal identity, membership cleanup, cancellation, and partial-write diagnostics |
| `REQ-VSB-SIGNALR-SCALEOUT` | cross-node fanout and acknowledged group commands | scale-out tests | exact recipients, no origin echo, remote add/remove acknowledgement, unknown connection, timeout, cancellation, and invalid command rejection |
| `REQ-VSB-SIGNALR-FANOUT` | multi-connection/group/user selection | fanout tests | stable ordinal de-duplication across repeated identifiers and overlapping memberships |
| `REQ-VSB-SIGNALR-CLIENT-RESULTS` | typed client invocations, completion, error, cancellation, disconnect, and stale delivery | client-result tests | local and remote success/null/error/cancel, owning-connection enforcement, protocol/invocation identity, timeout, disconnect cleanup, and duplicate suppression |
| `REQ-VSB-SIGNALR-SERIALIZATION` | protocol fanout and stored frames | serializer and serialization tests | every registered protocol, exact owned payloads, empty/malformed input rejection, and single-completion enforcement |
| `REQ-VSB-SIGNALR-CONTRACTS` | bus-delivered internal command boundaries | contract-validation and boundary tests | every required string, payload collection, exclusion, action, connection, group, user, and invocation member rejected before effects |
| `REQ-VSB-SIGNALR-SCOPES` | per-operation DI scope ownership | configuration and client-result tests | asynchronous scope creation, async-only disposal after success and failure, and required scoped services |
| `REQ-VSB-SIGNALR-RUNTIME-STATE` | subscription and pending-invocation indexes | runtime-state and client-result tests | ordinal keys, idempotent removal, ownership, duplicate invocation rejection, and terminal removal semantics |
| `REQ-VSB-SOURCE-NAVIGATION` | all 32 original SignalR source files | architecture tests and manual ledger | one transport-family project, final 31-file owner, matching type/namespace/file/folder responsibility, and no empty directory |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 1,368 original source lines and all replacement code | manual review plus documentation gates | current code and behavior only; exact ownership and concurrency semantics; no history, filler, generated rewrite, or stale names |

## Iteration 91 mutation obligations

- Remove identifier de-duplication from connection, group, and user fanout: each of the three exact
  fanout tests must fail by observing duplicate delivery.
- Change group identity from ordinal to case-insensitive comparison: the group case-sensitivity test
  must fail because two distinct SignalR groups collapse into one.
- Remove owning-connection validation from pending client invocations: the wrong-connection result
  test must fail because a different connection can complete the invocation.
- Omit asynchronous disposal of an operation scope: the DI lifetime test must fail because its
  async-only scoped resource is not disposed.

## Iteration 91 completion

All 32 original SignalR production files and their 1,368 lines were read manually in full together
with the complete replacement implementation, every comment, every direct test, the project file,
the requirements manifest, and the packed public surface. No generator or scripted comment rewrite
was used. Every comment was checked while its implementation was understood. The final owner has 31
C# files and 1,840 lines. Multi-type files were split so each independently meaningful runtime type
has a matching filename. The project and tests now occupy the matching `Transports` family, the old
paths and three empty move-remnant directories are absent, and all current solution, capability,
validation, and repository-graph paths agree.

The public package now exposes exactly `SignalRBackplaneOptions` and
`SignalRBackplaneExtensions.AddSignalRBackplane<THub>`. Registration is fluent, rejects missing or
duplicate composition before partial mutation, and validates the acknowledged remote-group timeout
with the repository's actionable feature/bus/problem/fix diagnostic shape. All consumer definitions,
wire contracts, runtime indexes, serializers, scopes, and lifetime-manager implementation details
are internal.

The lifetime manager now implements modern typed SignalR client results across nodes, including null
results, remote errors and cancellation, caller cancellation, timeouts, disconnect cleanup, stale or
duplicate delivery, and protocol/invocation identity validation. Connection, group, and user
identifiers use ordinal semantics. Multi-target fanout removes repeated identifiers and overlapping
members before delivery. Group commands are acknowledged by the owning node, endpoint identifiers
are deterministic and bounded, request handles are disposed, and bus operation scopes are created
and disposed asynchronously. Operational failures use the injected typed logger rather than ambient
context.

The focused profile grows from 41 to 96 tests and passes 96/96. Fresh focused coverage is 99.5585%
lines and 91.7910% branches across 110 instrumented methods, with no CRAP score above 30. The highest
risk method is `ConnectionConsumer.DeliverAsync` at CRAP 18.0069 with 97.22% line and 83.33% branch
coverage. Remaining formal branch gaps are compiler-generated asynchronous paths, simple consumer
constructors, and defensive binder/delegation branches; they are not represented as executed
evidence.

Four isolated counterchanges were compiled and executed. Removing fanout de-duplication made all
three targeted fanout regressions fail; case-insensitive group identity, missing pending-invocation
ownership, and leaked async scopes each made their precise regression test fail. Every counterchange
was restored manually. Final SHA-256 values are
`d2ae3366c1fa906cdd1cc6978c037c0da3e49ad5b5485fde3c7585178276b625` for
`ServiceBusHubLifetimeManager.cs`,
`6ad37586732aa02c35f044dcf3c7e51bee7c2c81057bb29feb1379650e8cfa85` for
`PendingClientInvocationTracker.cs`,
`618a42ea5f8fd6b86ead8cb2f26f17d2b7571eb2a8deeb38171cb2048513ceec` for
`ConnectionSubscriptionIndex.cs`,
`55ee9e13f63b6092956668f470a274543692dcec02e196101fb9a58748cf7a43` for
`DependencyInjectionBackplaneScopeProvider.cs`, and
`e794618f8ddef6f1d45628e7ecfccca1c99c47935221eaf074825768c8906cce` for
`HubMessageSerializer.cs`.

The final serial Release Unit solution passes 5,951/5,951 tests with zero failures and zero skips;
the complete Architecture project separately passes 292/292. These include the
repository-wide bidirectional async-name/implementation gate, source navigation, comments,
configuration diagnostics, requirement projections, and public documentation. The focused and
Architecture builds finish with zero warnings and errors. Both repository format gates pass with
zero changes across 5,778 Engineering and 5,356 Unit files. Requirements and metadata JSON,
`git diff --check`, repository-wide C# preprocessor, and empty-source-directory checks pass.

Fresh-package verification passes twice: 18 developer journeys using 31 freshly packed ViciOne
packages, three executed isolated provider-testing consumers, and all 30 runtime API assemblies.
The manually reviewed SignalR API diff removes 118 inherited implementation-surface lines and adds
only the two intended Greenfield types. The packed public API contains 19,104 lines with SHA-256
`34c7a90ef04451531e03134e0891e752a410996742627d4648941427f04aee27`.
Protected `review/` and `TestResults/` contents were neither changed nor staged. The complete A+
source goal remains active for the next unreviewed owner.
