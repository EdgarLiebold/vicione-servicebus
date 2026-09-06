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
