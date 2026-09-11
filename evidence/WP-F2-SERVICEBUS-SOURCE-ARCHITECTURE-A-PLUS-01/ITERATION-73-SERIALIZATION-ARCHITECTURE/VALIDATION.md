# Iteration 73 — Serialization Architecture

## Decision

**PASS for the bounded iteration.** The complete core serialization implementation was read manually, reorganized by cohesion, hardened, and validated without a known regression. The repository-wide A+ source goal remains open because product areas outside this completed scope still require the same file-by-file manual review.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `5185a6a22ac3509d14c51268ed5b9724bb1e6337` |
| Starting tag | `servicebus-a-plus-remediation-iteration-72-2026-09-11` |
| Candidate product C# aggregate | `a2472e94ea1da447af4d9a2513a8d4b7222e56cd028f1c9a1385dca3b4c5b37b` |
| Candidate serialization-scope aggregate | `3d48544599885a0bcca17acdeafdab86c71270303b909f6da3c59e37b59e33f9` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-73-2026-09-11` |

The candidate aggregates were calculated after the final source and test changes and before the completion commit. Generated files below `bin`, `obj`, `artifacts`, and `TestResults` were excluded.

## Manual review scope and method

Every one of the 49 pre-existing C# files and 4,190 physical lines below `src/ViciOne.ServiceBus/Serialization` was read in full and interpreted in its caller, wire-format, persistence, forwarding, and transport context. Splitting multi-type files and removing dead types produced a final bounded scope of 54 C# files and 4,303 physical lines. Directly coupled envelope abstractions, serializer configuration, persistence consumers, MessagePack consumers, forwarding consumers, and affected tests were reviewed where the serialization changes crossed their boundaries.

No generator or scripted rewrite authored production code, comments, names, tests, or architectural dispositions. Comments were evaluated only after the corresponding implementation had been understood. Every changed source comment is current English that describes code behavior or contract semantics; construction history and temporary process commentary were not retained. Mechanical tools were used only for file enumeration, hashes, compilation, tests, API extraction, formatting verification, coverage collection, and CRAP calculation.

## Physical organization

The former flat serialization directory and separate `JsonConverters` directory were replaced with one cohesive tree. The public `ViciOne.ServiceBus.Serialization` namespace remains stable where it is the correct discoverability boundary; physical implementation cohesion does not force artificial public namespace fragmentation.

| Directory | Files | Responsibility |
|---|---:|---|
| `Admission` | 10 | Bounded payload serialization and admission runtime |
| `Bodies` | 4 | Encoded message-body implementations |
| `Contexts` | 6 | Envelope, raw, and serializer context projections |
| `Encryption` | 3 | AES-GCM message-data encryption and key contracts |
| `Forwarding` | 3 | Forwarding pipes, expiration, and expired state |
| `Headers` | 4 | Header storage, message identifiers, and variable conversion |
| `Json` | 4 | JSON envelope, options, context, and conversion extensions |
| `Json/Converters` | 10 | Internal JSON converter implementations |
| `Metadata` | 2 | Stable infrastructure metadata codec |
| `ReliableMessaging` | 2 | Durable envelope metadata and the serialized-message marker |
| `Serializers` | 6 | Serializer selection, raw, envelope, copy, and forwarding serializers |

Each final file has one filename-matching top-level type. The only additional types are private nested implementation details inside their owning codec or context. `PayloadAdmissionRuntime.cs` was split from seven top-level types into seven matching files, and `ExpiredForwarding` was separated from `ForwardingExpiration`. No empty directory remains.

## Architectural and semantic remediation

### Envelope and public contracts

- Renamed the singular `MessageEnvelope.MessageType` member to `MessageTypes` and changed it to `IReadOnlyList<string>`.
- Changed envelope headers from mutable `Dictionary<string, object?>` to `IReadOnlyDictionary<string, object?>`, while wire DTOs retain serializer-compatible mutable storage behind read-only interface projections.
- Added defensive collection snapshots so source arrays and dictionaries cannot mutate copied envelopes.
- Changed the JSON wire property from `messageType` to `messageTypes`. This is an intentional greenfield contract correction; no legacy alias was retained.
- Split high-complexity envelope overlay logic into named identifier, address, expiration, and sent-time decisions. The former fully covered CRAP-50 method no longer exists.
- Preserved diagnostic propagation headers in raw forwarding while continuing to hide transport-owned `VSB-` metadata. This fixed chained Quartz trace propagation without exposing service-bus internals as application headers.

### JSON conversion and forwarding

- Moved JSON converters into `Serialization/Json/Converters`, changed their namespace accordingly, and made implementation-only converters internal and sealed.
- Restricted the converter factory to dictionary shapes that can actually be materialized, while consistently supporting string- and URI-keyed dictionary interfaces and compatible key/value enumerables.
- Made duplicate string and URI JSON properties consistently use the last value, matching normal JSON object materialization.
- Rejected invalid URI property names and invalid or truncated dictionary JSON with `JsonException` instead of leaking incidental exceptions or returning partial data.
- Centralized null/default suppression before scalar writing, retained the stable decimal-as-invariant-string representation, and separated scalar serialization from dictionary-entry policy.
- Corrected forwarding overlays so matching objects merge recursively, matching arrays append in order, null object properties preserve existing values, and incompatible JSON shapes replace the original value instead of silently retaining stale content.
- Renamed `SystemTextJsonBodyMessageSerializer` to the internal behavior-oriented `SystemTextJsonForwardingSerializer` and `SystemTextMessageDataReference` to `JsonMessageDataReference`.

### Metadata, bodies, headers, encryption, and reliable messaging

- Replaced the ambiguous public `ObjectDeserializer` helper with `ServiceBusMetadataSerializer`. Reference conversion is exposed as `DeserializeReference`; value conversion has an optional one-argument form and a type-inference-friendly explicit-fallback form named `DeserializeValue`.
- Replaced `MessageIdMessageHeader` with sealed `MessageIdHeaders` and made every access path case-insensitive and key-validating.
- Hardened mutable and read-only header adapters so invalid externally injected entries cannot escape and arbitrary dictionary implementations remain supported.
- Removed the dead `SerializedMessage` contract and the `NotSupportedMessageBody` type whose only behavior was to throw.
- Replaced the empty `SerializedMessageBody` marker with the explicit singleton `SerializedTransportMessage.Instance` and propagated the marker through Quartz, EF Core, in-memory, RabbitMQ, and tests. It is a real pipeline identity token, not a dummy payload.
- Made encryption keys validate envelope bounds, retain owned key material, and expose key bytes only to the encryption implementation. AES-GCM boundary and tamper behavior remains covered.
- Removed the unusable reliable-metadata sent-time field and retained only metadata that durable persistence can reconstruct correctly.
- Renamed the mutable-looking `Serialization` implementation to internal `SerializerCollection`, made public content-type accessors defensive values, and sealed concrete types that are not extension points.

### Cross-cutting failure found by the complete tests

The generic JSON dictionary codec correctly preserves explicit default scalar values when default suppression is disabled. The first complete core run showed that Courier had previously relied on the serializer to discard default-valued object properties. That concern now belongs to the Courier object-to-activity-argument projection: anonymous-object defaults are filtered before routing-slip construction, while explicitly supplied dictionary defaults remain intentional values. Guid, integer, Boolean, and DateTime fallback and override cases prove the distinction.

## Test additions and strengthened proof

The core test count increased from 2,587 to 2,612, and the complete unit-solution count increased from 5,120 to 5,145. New or materially strengthened tests cover:

- read-only envelope collections, copy isolation, plural message-type wire names, and all metadata overlay fields;
- raw-header filtering through every access path plus case-insensitive diagnostic propagation;
- native serializer-context messages, runtime contracts, defensive message-type snapshots, and metadata-codec delegation;
- all supported scalar dictionary representations, null/default policies, nested objects and arrays, key/value arrays, duplicate keys, URI keys, unsupported materialization shapes, and malformed input;
- runtime and generic JSON transforms, public interface materialization, and every required argument;
- recursive forwarding merges, top-level array appends, and incompatible-shape replacement;
- reference/value metadata conversion, nullable variable lookup, absence behavior, and key validation;
- raw text bodies, probe metadata, raw option validation, and `BodyConsumeContext` runtime admission;
- Courier object defaults versus explicit dictionary defaults;
- encryption key envelope bounds and serializer-option isolation.

Every added fact or theory method has a matching requirement projection entry. The exact projection gate passes.

## Validation

| Gate | Result |
|---|---|
| Core test project build | PASS — 0 warnings, 0 errors |
| Core test project | PASS — 2,612 passed, 0 failed, 0 skipped |
| Engineering solution build | PASS — 0 warnings, 0 errors in 2m 44s |
| Complete unit solution | PASS — 5,145 passed, 0 failed, 0 skipped |
| Bidirectional async and cancellation architecture gates | PASS as part of the complete architecture test assembly |
| Developer journeys | PASS — 18 scenarios, 31 freshly packed packages, 3 isolated provider testing consumers |
| Packed public API | PASS — all 30 runtime package APIs match the committed baseline |
| Packed public API hash | `25970fe31eb05b2cbf8fac61a8e57397e1b69407733e09930678747b00db3d5c` |
| `dotnet format --verify-no-changes` | PASS; one known non-fatal workspace-load warning, no format changes |
| `git diff --check` | PASS |
| Preprocessor directives in bounded scope | PASS — none |
| Dummy, stub, placeholder, TODO, FIXME, legacy, MassTransit, or `NotImplementedException` markers in bounded scope | PASS — none |
| Filename/top-level-type mismatches in bounded scope | PASS — none |
| Empty directories in bounded scope | PASS — none |

The complete test run initially exposed two real behavioral couplings: Courier default filtering and Quartz diagnostic propagation. Both were diagnosed at their owning boundary and corrected with regression tests. A later isolated `BodyConsumeContext` test failure was a test-fixture error: the strict proxy omitted the publish provider legitimately read by the base constructor. The fixture was corrected without changing product behavior, and the complete core and unit runs then passed.

## Coverage and risk result

Coverage was collected by the complete 2,612-test core module. Lines shared by compiler-generated state-machine classes were merged by source file and line before calculating the bounded aggregates. Generated sources and test infrastructure were not counted as product coverage.

| Scope | Line coverage | Branch coverage | Instrumented methods | Methods with CRAP > 30 |
|---|---:|---:|---:|---:|
| Core `src/ViciOne.ServiceBus/Serialization` implementation | 1,384 / 1,494 = **92.64%** | 701 / 882 = **79.48%** | 301 | **0** |
| Serialization implementation plus `src/ViciOne.ServiceBus.Abstractions/Serialization` contracts | 1,639 / 1,805 = **90.80%** | 818 / 1,040 = **78.65%** | 372 | **0** |
| All product code instrumented by the core module | 42,605 / 61,626 = **69.13%** | 14,729 / 23,904 = **61.62%** | Not used as the bounded decision | Not used as the bounded decision |

The repository-wide aggregate is not represented as complete repository coverage: it is the product code loaded by the core test module. Provider local-integration suites and later full-goal aggregation remain separate evidence. The bounded decision is based on the complete serialization scope and its directly coupled abstraction contracts.

This iteration did not run a mutation campaign and does not claim mutation closure. Repository-wide mutation testing remains a final goal-level validation after all manual source iterations are stable, so subsequent architectural changes do not invalidate an earlier campaign.

## Repository hygiene and limits

- `review/**` was neither edited nor staged.
- Generated `TestResults/**` coverage data remains untracked and is excluded from the completion commit.
- The iteration is an implementation-team review and does not claim independent external or Red Team acceptance.
- Real-provider protocol acceptance was not repeated because the provider changes are compile-time consumer updates for serialization contracts, not broker protocol changes. Full provider acceptance remains part of final goal validation.
