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
