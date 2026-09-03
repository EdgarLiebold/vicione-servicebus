# V5 payload, diagnostics, and analyzers validation

Date: 2026-09-03

## Bound inputs and scope

- Package: V5 integration 3/4, including V5.1 RT-001, RT-005, and no-throw rejection observation.
- Architecture assignment: `PO-2026-09-03-SERVICEBUS-REVIEW-INTEGRATION-14`.
- Product baseline: `3744d8b31b08b5b79321b6fb243a263c2c9e050c`, tree
  `38e5d2d43040472c0a9cede18ea2646b885a3a2d`.
- Protected review aggregate: `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`.
- V5 cumulative patch: `88f2c61a3b2fc470f525ce68a3972463e731bea418cfa9de0d58f4e745a1a344`.
- V5.1 delta: `fac6328e141fd71daf01db0efe9987a79a5d561e8929ab209c4e70b9dbe7562c`.
- Frozen semantic sources: payload `55d64655`, analyzers `43678bdd`, redaction `00b28038`, observability
  `17e10d83`, and final hardening `6ab00c32`.
- `review/**` remained read-only, unmodified, untracked, and unstaged throughout.

The donor is semantic evidence rather than an executable verdict path. Its mechanisms were adapted to the current
typed-bus DI boundary, existing V4 MessageData owner, serializer implementations, diagnostics infrastructure, native
xUnit 4/MTP v2 tests, requirements projection, and source-order architecture guards.

## Payload admission and serialization ownership

Abstractions now own an immutable validated admission policy, exact decision result, stage-specific exception, evaluator
SPI, and bounded serialization-buffer SPI. Registration is bus-generic and resolves exactly one matching runtime owner;
unconfigured buses remain unaffected and typed buses cannot consume another bus's policy.

The common physical send boundary performs admission before user send observers and provider I/O. Event Hub's special
single and batch producers invoke the same boundary before their distinct observation and SDK-send paths. System.Text.Json,
raw JSON, and MessagePack serialize the application body once, evaluate those bytes, reuse the admitted representation,
and independently bound the final envelope. Repeated body access reads the cached result rather than serializing again.

`BoundedPayloadSerializationBuffer` allocates exactly the configured owner capacity and can neither resize nor return a
segment outside that capacity. A hostile size hint is rejected before bytes are committed. The public evaluator proves
inclusive actual-byte decisions at maximum minus one, exact maximum, and maximum plus one. Real serializer owners prove
their same boundary matrix where the serializer's `IBufferWriter` reservations permit it.

There is an unavoidable distinction between final byte length and serializer reservation capacity: `Utf8JsonWriter` and
MessagePack may legally request a conservative contiguous segment larger than the bytes they ultimately commit. An
`IBufferWriter` must either return at least that segment or reject it; satisfying the request by allocating beyond the
hard limit would violate the security invariant. The real-owner tests therefore prove the minimum bounded owner capacity
with deterministic metadata as well as the independent exact-byte evaluator semantics. They do not mislabel an
unbounded staging allocation or a second serialization as exact-limit evidence.

## Existing MessageData owner

Offload evidence is emitted only by the existing bus-owned MessageData transform and only after a value has an actual
stored address. Merely registering a repository or carrying an inline value does not satisfy the threshold. The evidence
contains no payload, repository, policy, or serialized control data, and reference identity prevents combining owners.

Full regression exposed an initially unsafe empty-value path: reading `EmptyMessageData.Address` while constructing a
fault replaced the intended request fault with a timeout. The final implementation first checks `HasValue`, preserving
the established empty-value/fault contract. Both interface and concrete request contracts now return their exact original
`MessageDataException`, and the removal of that guard is causally killed.

## Redaction and diagnostics

Public sensitivity metadata supports payload and member classification. The runtime inspector is conservative across
base classes, interfaces, and overrides, while its weak cache does not root collectible application types. The redactor
never invokes arbitrary application `ToString`; it emits bounded invariant primitive forms, redacts only classified
scope, supports relative URIs, sanitizes control characters and malformed surrogate input, and never splits a Unicode
scalar at its bound.

Registration is idempotent and preserves an application override. Admission metrics carry only bounded stage and reason
dimensions and no payload, identifiers, addresses, exception text, or message type. Rejection is observed once per
buffer. A throwing metric listener/exporter cannot change either accepted results or the original admission exception.
The existing hostile logger, Activity listener, Meter factory/listener, and message-observer regression owners also pass
in the complete run; no parallel admission-specific logger, Activity, health, or observer owner was introduced.

## Semantic analyzers

VOSB5001 through VOSB5005 use canonical Roslyn symbols and containing-assembly identity rather than names or namespace
text. They cover blocking consumer calls, inherited `ConsumerDefinition` concurrency/QoS declarations, canonical
topology exclusion, and large inline consumed or outbound contracts. Lookalikes, shadowed members, MessageData fields,
unrelated methods, and generated code remain clean. Compilation-end VOSB5005 output is deduplicated and deterministic
when one analyzer instance processes compilations in parallel.

## Native execution evidence

| Scope | Result |
|---|---:|
| New Core payload/redaction/telemetry cases | 46/46 passed, 0 skipped |
| Existing MessageData fault regression | 2/2 passed, 0 skipped |
| New MessagePack cases | 2/2 passed, 0 skipped |
| New analyzer cases | 6/6 passed, 0 skipped |
| New architecture cases | 4/4 passed, 0 skipped |
| Requirements projections | 4/4 passed, 0 skipped |
| Complete Unit/Architecture aggregate | 3,460/3,460 passed, 0 skipped |
| Shipping Release build | 0 warnings, 0 errors |
| Engineering Release build | 0 warnings, 0 errors |

The JSON envelope owner was additionally run three times in independent processes after deterministic metadata replaced
a timestamp-length-dependent calibration; all three runs passed 16/16.

## Quality and hygiene gates

- 42 package test methods expand to 58 native cases and contain 228 direct assertion calls; no package test is
  assertion-free or trivial-only.
- 35 independent buildable one-cause production mutations are causally rejected; every mutant was restored before
  positive execution.
- Scoped `dotnet format --verify-no-changes` passes for all 51 changed/new C# paths.
- All four changed requirements JSON files parse, and their projection owners pass in the complete native run.
- Both Release solutions build with analyzer execution enabled and zero warnings or errors.
- The protected review manifest passes from its correct `review/` working directory.
- `git diff --check` passes; no protected review file is staged or modified.

V5 package 3/4 is locally validation-complete and ready for its stationary product/architecture checkpoint. No remote
publication is authorized or implied.
