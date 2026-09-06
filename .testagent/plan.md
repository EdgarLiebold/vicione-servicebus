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
