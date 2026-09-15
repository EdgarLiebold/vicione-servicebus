# Core Owner Reading Completion and Source Contract Review

## Disposition

The original autonomous whole-product greenfield A+ goal remains active. The main
has now personally read all 557 tracked files in the Core test-project directory,
including every code file, helper, fixture, project input and data record. This
iteration completes the remaining connected 150-file packet. It is a reading and
existing-test review milestone, not whole-project test acceptance or A+ completion.

The main also completely reads nine connected productive source files and checks
every comment against their actual implementation. One misleading constructor
comment is corrected manually. No executable code, test, public signature,
dependency, directive or feature is changed. All review observations, comments and
this report are individually authored; no source, test, comment or report generator
is used. The requirements data view described below is read-only, lossless input
presentation, not generated source, assertions, comments or review conclusions.

Three source-contract items and nine grouped existing-test quality items are kept
explicit below. A weak assertion is not labeled a confirmed runtime defect; no
runtime counterexample, surviving mutant or mutation kill is invented. Earlier
findings retain their actual dispositions.

## Bound input and authority

Repository: repositories/vicione-servicebus.
Branch: feature/servicebus-a-plus-api.
Remote: origin, git@github.com:EdgarLiebold/vicione-servicebus.git.
Secured input commit: 4ac87c03b95c07bf0414434c031a45fb57d904b6.
Input productive source tree: 400a506421aa680730469d7bfbb4b78e364164b7.
Unchanged Core owning tree: e3be6b831183b3b65636c3b5e167c165696037d2.
Prior annotated tag:
servicebus-a-plus-iteration-130-connected-execution-harness-review-checkpoint-2026-09-15.
Its independently verified object is 902a658609ffed61c18a4e27809aaf082e32122f,
peeled to the input commit. That security covers all earlier committed work; the
new comment derivative requires this iteration's own checkpoint.

The fully read entry instructions and seven unchanged authority hashes recorded
in Iteration 130 remain bound here. AI_WORKING_AGREEMENT.md SHA-256 is
e6d5f60db535ad6228fca5445b68abaa7a29cd6e24b5d2f876352bc7de875d2e;
the selected WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01 Development Slice is
5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199.
The Licensing-only decision grants no ServiceBus exception or product-role authority.

Protected review/, TestResults/ and workspace vicione-legacy/ are not inspected,
enumerated or changed. Unrelated user work and existing index contents are preserved.
The actual Core owner is tests/ViciOne.ServiceBus.Tests, not tests/Unit/Core.
Its configured artifact role ViciOne.ServiceBus.Tests.Unit does not identify a
second copied project.

## Completed personal reading

Every complete code read includes the full implementation, all secondary types,
callbacks, guards, assertions, lifetimes and comments through EOF. Invisible or
truncated output is not credited. Deferred observations arise from these reads,
not merely filenames, requirement titles or an automated scanner.

| New packet | Full files | Physical input lines |
| --- | ---: | ---: |
| Initializers | 46 | 7,452 |
| Mediator | 14 | 3,312 |
| MessageData | 13 | 2,620 |
| Requirements catalogue | 1 | 5,768 |
| Serialization | 48 | 7,243 |
| Transformation | 1 | 343 |
| Transports | 27 | 5,866 |
| Complete new Core packet | 150 | 32,604 |
| Previously bound Core reads | 407 | 110,523 |
| Cumulative Core directory reads | 557 / 557 | 143,127 |
| Additional productive source reads | 9 | 1,510 |
| Additional shared build/project reads | 5 | 443 |

Physical counts describe the original tracked bytes. The JSON catalogue receives
full semantic-record reading credit, not a claim that every original physical
character was displayed. Additional project-file rereading is not double-counted
as another owning file. This does not imply all productive src or all repository
test-project owners have been completely read or accepted.

The strict read-only reconciliation succeeds: the sorted union of previously bound
and new personally read paths is exactly the 557-path scoped Git manifest; every
current Core file equals its input Git blob byte-for-byte; new packet hashes and
line counts match the prior pinned metadata; the Core tree is unchanged. The nine
productive inputs and five graph inputs also reconcile, allowing only the exact
manual two-line comment substitution documented below. No parser or scanner alone
substitutes for personal reading.

Agreement section 4.3 still requires the complete effective shared build/package/
fixtures/data/execution closure, appropriate full language parsers and final
Git-Dateimenge = gelesene Dateimenge admission before new Core test design/editing
or whole-owner acceptance. The 557-file directory reading is complete; full effective
project admission is not yet asserted. Five extra graph reads are not called the
complete import/dependency/CI closure.

## Complete requirements data reading

CoreRequirements.json input SHA-256 is
b492c5e6d50ed977503d744b5174c199eeb379e097e54fb3804e50a0e436ba21.
The entire JSON is parsed with duplicate-field rejection. A deliberately duplicate
field input is rejected before the actual catalogue is admitted. Every array record
has exactly the five required nonempty string fields: requirementId, variantKey,
testAssembly, testType and testMethod. All 2,922 records use the actual single
ViciOne.ServiceBus.Tests assembly and contain 527 distinct type identities.

The read-only presentation explicitly emits changing type and requirement values,
retains every original array index and exact variant/method string, and inherits
only explicitly displayed repeated context. Reconstructing all five-field objects
from the emitted ordered view equals the original parsed array exactly. Whitespace
and JSON object-field order are not semantic inputs; no value, entry or array order
is removed. The main personally reads view lines 1–5,143, including records 0–2,921
and explicit EOF. The unobserved truncated attempt at 4,561–4,800 receives no
credit; the same bounded range is subsequently fully visible before completion.

Catalogue labels are associations, not proof verdicts. In particular:

- Old cache scheduler-rejection/invoke-then-throw vocabulary maps to direct cleanup
  availability/idempotence facts; it needs source/spec reconciliation against the
  native cache architecture, not automatic acceptance or a fabricated missing method.
- Receive proxy exact-arguments record 2,001 maps to a fixture that records operation
  names but does not independently record its notification argument identities.
- EveryAccessorOrder covers four first-accessor choices, not all 24 permutations.
- The nonce requirement uses random vocabulary; the associated duplicate check proves
  uniqueness, not an independent randomness property.
- Json serializer ContentType isolation record 1,981 correctly concerns those
  serializers, not the separate Mediator global mutable field.
- Durable InMemory admission/replay/completion associations do not prove actual
  cloud-provider acceptance or crash/restart durability.
- LegacySagaIntegrationTests names retained classic saga functionality. A title alone
  cannot justify deleting that capability as obsolete MassTransit compatibility.

## Strong controls retained

### Initializer conversion, ownership and independent values

AdvancedMessageInitializerExtensionsTests compares selected typed/runtime contract
type, payload, pipe and cancellation arguments. MessageInitializerContractTests
distinguishes property caller cancellation from header send cancellation, rejects
null collaborators and invalid context outputs, retains original provider faults
and checks no downstream send after a null header task. Property/header tests use
genuinely incomplete sources and independently populated before/after values.
MessageDataPropertyConverterContractTests mutates caller bytes after wrapping and
checks retained 1/2/3, distinguishes borrowed streams and no-value poison access.

Object graph tests preserve explicit nested fields, different host scalars,
most-derived duplicate property selection and known fault projections. Selected
TypeConverterContractTests uses a hostile custom negative sign under changed culture
with finally restoration, literal invariant output, UTC-kind and adjacent temporal
overflow controls. It would be incorrect to report culture testing as wholly absent.

### Real Mediator execution and immutable transport bodies

MediatorDispatchTests executes actual dispatch, configured body/depth limits,
short-circuit filters, request responses and mandatory versus optional publication.
It checks a detached eagerly serialized snapshot, separate nonwritable stream
positions and unchanged bytes after payload mutation. A genuinely held handler
provides a cancellation control. Receive context tests hold attached work and assert
dispatch remains incomplete before release. Send observer tests preserve original
pipeline/serialization faults and prevent application dispatch on rejection.

ScopedMediatorContractTests executes eleven distinct publication forms, known
calling scope identities, exact response values and addressed destinations. Its
noncontextual request scope-label gap is separately retained below, not generalized
to those actual contextual scope checks.

### MessageData and application serialization

AES-GCM tests exercise literal versioned envelope layout, ordered boundary-sized
bytes, tampered regions, independent wrong key material with an authentication
cause, key rotation and defensive key copies. Repository tests provide independent
stored-content and expiry/stream lifetime controls; they qualify narrower encryption
round-trip gaps. Nested MessageData tests use independent body/name/address values
in their stronger cases and inspect terminal counts after stopping real transport.
MessageData remains an application-data feature, not an obsolete compatibility shim.

JSON tests inspect literal date/time/decimal/null/default wire values, duplicate
last-value-wins semantics, explicit raw advertised-contract admission and constructor
binding. Protobuf/XML cases preserve actual generated application payload fields and
non-ASCII opaque XML text/bytes. Json isolation mutates real ContentType exports and
configured inputs, verifying fresh independent serializer values. Eager body tests
count one converter write before reads, mutate inputs and inspect immutable content
through 128 concurrent readers. Runtime bus policy isolation checks exact ordered
values/naming after bounded shutdown.

### Lifecycle, fabric and original failure identities

KillSwitch tests include literal activation/ratio/window behavior, exact one-tick
recovery boundaries, one owner and original/restored log-context behavior. Receive
lifecycle tests actually hold reset or connection work, assert pending stop and
retain exact failures, cancellation identities and post-stop attempt/fault sequences.
ReceivePipeDispatcherTests retains exact primary/settlement failures and properly
restores ambient LogContext in finally; this is a positive cleanup counterexample.

Fabric tests distinguish case comparers, repeated bindings, cycles/diamonds, once-only
dispatch and literal FIFO content. EndpointProviderTests genuinely holds disposal,
retains ordered original disconnect/dispose failures and retries after malformed
collaborator results. SendEndpointCacheTests distinguishes cache-owned creator
cancellation from caller cancellation. A synchronous HostHandle Start initiation
returning an asynchronous readiness handle is legitimate and is not blindly renamed
Async. Real empty JSON application bodies and narrow failing test collaborators are
intentional capabilities/negative controls, not productive dummy implementations.

## Source contract findings

### MD01 — Global mutable Mediator MIME metadata — OPEN

MediatorReceiveContext.cs defines a static readonly ContentType instance and its
public ContentType getter returns that same object. readonly protects the reference,
not ContentType.MediaType or its parameters. Mutation through one exported instance
can therefore change later contexts globally. This is a concrete source-derived
unsafe shared reference; no new runtime poisoning regression has been executed.
The writable MediaType contract is documented in the official [.NET 10 ContentType
reference](https://learn.microsoft.com/en-us/dotnet/api/system.net.mime.contenttype.mediatype?view=net-10.0).
Fresh immutable-value presentation should retain canonical application/json
and all existing Mediator functionality. The already effective defensive Json
serializer tests do not discharge this separate field's obligation.

### MD02 — Notification boundary ordering and invalid observer task — OPEN

MediatorReceiveContext notify methods pre-cancel before checking their required
context/consumer inputs; BaseReceiveContext validates required inputs first. The
Mediator path also lacks the base path's explicit null observer Task diagnostic.
These are inconsistent/proof-unsettled boundaries, not accepted runtime defects
without the whole API policy and causal evidence. Cancellation precedence may be
intentional; the review must decide it consistently rather than mechanically copy
the base implementation and accidentally alter a feature contract.

### PA01 — Encoded size versus writer-owned reservation — OPEN

BoundedPayloadSerializationBuffer enforces written bytes plus requested sizeHint
within one hard owned allocation. A serializer's worst-case reservation is not the
exact eventual encoded body/envelope length. The JSON envelope test derives its
minimum admitted capacity by executing the same producer; that proves producer
capacity transitions, not an independent exact encoded-size guarantee. Public policy
and measured ownership semantics require an explicit greenfield contract decision
and independent causal evidence. Retaining bounded memory matters on the constrained
platform; simply removing reservation limits is not an accepted fix.

The constructor's former comment incorrectly promised that an exact-size payload
would remain admissible. After reading the full writer and body construction paths,
the main manually replaces it with the accurate ownership statement:

```csharp
// Every returned memory region stays within the hard ownership limit, including
// reservations for worst-case serializer expansion.
```

Input buffer SHA-256:
ddf1dc29e2c0ae789938f57f5aaa4182900a9bd2ded7740d84568e39e32a5427.
Final buffer SHA-256:
590d53ee14a5dea3926d946a6ff5c3696f765982fcd70f11fd81aca8af888152.
The strict exact-string derivative comparison permits only those two comment lines.
PA01 runtime semantics remain open; correcting documentation does not close them.

## Existing-test quality findings

Grouped scope: 0 Critical / 5 High / 2 Medium / 2 Low, all nine groups OPEN. These
overlap prior groups; counts are not added as independent production defect totals.
Potential counterchanges below are evidence criteria deferred until owning-project
admission, not prematurely designed or generated new tests.

### H01 — Exact forwarding, identity and noncommuting order gaps

Receiving/ReceiveContextProxyTests.Operations_DelegateOnceWithTheirExactArgumentsAsync
checks ordered method names and task identity but does not capture/compare consumed
context, duration, consumer type, exception or notification cancellation arguments.
Wrong-argument forwarding can preserve that asserted trace. Its payload factories
are not independently captured/exercised. TransportValueFormattingTests key adapter
delegates return constants without recording the supplied context/message.

MessageFabricTopologyBuilderTests uses the same exchange and queue name, so a swap
is invisible; its configured routing key is never exercised by an actual send.
Get operations can create missing nodes and mask omitted declarations; a two-sink
count is not the exact sink pair. MessageInitializerBuilderContractTests uses
independent property assignments, so reordered initializers can produce the same
final state despite its order title. These are concrete proof gaps, not evidence
that the current implementations actually swap or omit arguments.

### H02 — Failure-safe cleanup, held work and ambient state

Several KillSwitch tests call CancelRecovery only after assertions and assign
ambient LogContext without finally restoration. A failed assertion can strand fake
timers or contaminate the next test's context. ConsumerAgentTests, controlled fabric
queue cases, HeaderInitializerContractTests, PropertyInitializerContractTests,
ReceiveLifecycleTerminalityTests, ReceiveLockContextTests and two SendEndpointCache
held-work cases omit unconditional release/drain on early failure or leave waits
unbounded. Some fixture cancellation registrations/sources are not owned/disposed.

The first two MediatorSendObserver facts do not own/dispose the created Mediator,
unlike the later four. Multiple InMemory/integration starts occur before try; some
cleanup passes a canceled runner token, and sequential secondary-first stop can
prevent launching cleanup for the primary owner. ReceivePipeDispatcher and stronger
prior retry/telemetry finally controls demonstrate the required property already
exists elsewhere; do not weaken those controls into the poorer pattern.

### H03 — Terminal multiplicity and drained outcomes

First completion, Take or predispose count checks in selected Mediator/dispatcher/
serialization-fault/admission/send transport facts do not exclude late or duplicate
deliveries/faults after shutdown. Queue stop/drain tests do not assert which buffered
messages were delivered or discarded. A post-stop state label is not by itself a
content/attempt ledger. Strong MessageData, serialization redelivery, runtime policy
and receive lifecycle cases already use exact post-stop counts and retain stronger
status; the gap is not reported as universal.

### H04 — Controlled overlap and independent timing bounds

Task.Yield in recovery is not a queued callback completion barrier; a 250ms wall
observation is not a causal queue milestone. Two distinct cache keys do not prove
same-key cold contention. Task.Run batches without a common admission gate may
execute serially. Both-token Host retry cancellation sequentially changes two tokens
without controlling the classification interleaving. Multi-bus health does not hold
one readiness owner pending despite its only-after-both-ready title.

Independently bounded entered/release/terminal observations must distinguish the
actual property after admission. Preserve genuinely held pending disposal/reset/
handler and one-tick FakeTimeProvider tests rather than imposing a superficial
uniform delay or relaxing their assertions.

### H05 — Security rejection mechanism is not independently isolated

SystemTextJsonTypeSafetyTests has a real hostile $type literal, but names a foreign
assembly/type that is unavailable. Rejection may merely reflect failed resolution,
not independently demonstrated exclusion of a resolvable forbidden activation.
AES-GCM nonce uniqueness permits a deterministic unique sequence. A tampered key ID
can fail unknown-key lookup instead of isolating authenticated associated-data
behavior with a resolvable alternative ID. These gaps do not imply an actual security
exploit or that tested authentication/wrong-key/rotation behavior is incorrect.

### M01 — Retained state, independent contents and parameter/boundary matrices

Some failed initializer/factory paths check failure or descriptor count without
retaining independently prepopulated state and exact prior descriptor identity.
MessageInitializerContractTests does not release canceled dependencies and then
inspect absence of late writes. Several all-state titles cover completed/null
dependencies but no actual fault or second-slot behavior in their local facts.
Utf8 threshold cases with only ASCII cannot distinguish byte from character limits;
always-write settings qualify whether a threshold transition is genuinely exercised.

ReceiveMessageLimitsTests uses an all-zero buffer, masking an equal-size content
substitution. MessageSendContextPropertyTests compares nullable MessageId to Guid.Empty
without independently requiring nonnull, so null can pass that assertion. Selected
job projection IDs, raw fault metadata and host scalars are not all independently
rechecked. Native pointer-width bounds and temporal extreme precision need deliberate
platform/value evidence, not an unconditional cross-platform claim.

### M02 — Producer-derived expectations and selected-surface completeness

PA01's minimum writer capacity is a producer-derived expectation. Same-serializer
round trips and shared default-policy helpers do not independently establish wire
schema or all configured providers. BodyReadingObserver checks lengths/text presence,
not every exact byte/cache relationship. Exact literal JSON converters, eager body
mutations and known metadata values elsewhere remain real counterexamples to any
blanket tautology claim. Four first-accessor choices, selected dictionary families,
configuration mutation paths, generic slots and input states are not automatically
the entire runtime surface merely because requirements say Every or All.

### L01 — Naming and catalogue semantics

EndpointProviderTests.SendEndpoint_NormalizesOnceAndForwardsTheCreationTokenAsync
accompanies an asserted normalization count of two and distinct cache-owned creation token. Other
EveryState/EveryAccessorOrder/scope/exact-argument labels exceed their assertions.
Repeated Configuration.Configuration and JobService.JobService namespace segments
and LegacySagaIntegrationTests require greenfield naming disposition without feature
deletion. Sync host initiation and synchronous helper round trips are not Async-name
defects. No fresh repository-wide bidirectional Async acceptance is claimed here.

### L02 — File/type organization and local formatting

SerializationFaultTests and SystemTextJsonCollectionTests contain several secondary
top-level fixture contracts; SystemTextJsonRoundTrip contains two secondary result
records. Their file/type ownership deserves a deliberate consistent .NET disposition.
Nested narrowly owned fixtures need not be split merely for a cosmetic one-type rule.
Packed guard statements/inline records and implicit private members appear in selected
lifecycle/proxy/helper tests. No repository-wide formatter result or claim that every
src/test filename/comment/directive is already A+ follows from this packet.

## Corrected review attribution and failed diagnostic handling

EV01 is a resolved main reading error, not a stale-build defect: a neighboring raw
body +1/rejected:true theory row was initially attributed to raw envelope. Exact
current/input bytes and structured historic CTRF rows show only -1/false and 0/false
for the raw-envelope theory. Its rejected:true branch is unreachable under its
current data; a separate below-body configuration rejection fact exists.

EV02 is another resolved exact-name reading ambiguity. Both the current KillSwitch
method and catalogue use
SuccessfulDeliveriesDuringRecoveryVerification_ReturnToRunningAtExactActivationBoundaryAsync.
A structured exact-field historic CTRF query finds exactly one passed case. No
missing Async rename/catalogue method/discovery defect is established.

CoreMessageBodyContractTests is personally reread: the separate Mediator body-family
fact actually asserts an empty owned type set, in addition to the explicit three
Core body families. Empty expected type set does not mean an absent test.

A first read validator compared binary data to UTF-8 tagged strings incorrectly
when the body fixture contained non-ASCII text. Exact SHA and binary equality identify
the diagnostic encoding problem; the corrected byte comparison passes. A shell
quoting failure in the first final-validator invocation produces no successful
receipt; the correctly quoted invocation is independently successful. A whole-line
rg on minified historical CTRF is truncated and unsuitable; it is replaced by the
bounded structured field query. Failed/truncated probes never receive reading,
acceptance or fresh native execution credit. The installed Ruby lacks filter_map;
the final manifest validator uses compatible map/compact and must succeed separately.

## Evidence and actual validation limits

Own raw directory:
/private/tmp/vsb-iteration131-owner-reading.2KOzZU.

| Raw read-only evidence | SHA-256 |
| --- | --- |
| personal-read-bindings-first149-v2.log | a10d0281dd6623f8fbfc806bf60c76fb87466c95da06da7db3df6d4909a93c9a |
| core-requirements-lossless-reading-view.log | f946b20d9557db043192e0fca636ef0e2da72ac634938748f4833906282077a1 |
| full-core-main-read-bindings-v3.log | 809425bf96e1d2a287e5e4d6268ae6c11defc06fa8d930ba9c4a855e6011abe3 |

First149 byte reconciliation exits 0; the whole JSON parse/schema/duplicate control/
ordered reconstruction exits 0; final 557-path plus extra source/build reconciliation
exits 0. Final evidence validation also checks the handwritten manifest against the
exact selected paths/counts, unchanged history tails and the sole comment derivative.
These are bounded read/byte evidence, not new runtime/mutation or full C# parsing.

Historical Iteration 128 strict build and native 4,007/4,007 passes remain historical.
No duplicate build/native run is necessary to validate this exact comment-only
derivative. No fresh coverage/CRAP, runtime mutant, Architecture/global Async,
real durable-provider or independent external Red Team acceptance is claimed.
Historical Core loaded-assembly coverage is not entire-product coverage. Cache CS01
implementation cleanup is repaired, but its constructor fault regression, effective
cleanup counterchange and portable interval policy remain OPEN, as do all earlier
unclosed broader gates.

## Complete new owning-file manifest

Paths below are relative to tests/ViciOne.ServiceBus.Tests/. Every row is personally
FULL, including all code/helpers/comments. JSON is FULL semantic lossless data reading
as distinguished above. Counts are original physical input lines.

| Path | Lines |
| --- | ---: |
| Initializers/AdvancedMessageInitializerExtensionsTests.cs | 193 |
| Initializers/AdvancedRequestInitializerExtensionsTests.cs | 161 |
| Initializers/AdvancedScheduleInitializerExtensionsTests.cs | 181 |
| Initializers/Contexts/InitializerContextContractTests.cs | 94 |
| Initializers/Conventions/DefaultInitializerConventionTests.cs | 213 |
| Initializers/Conventions/DictionaryInitializerConventionTests.cs | 243 |
| Initializers/DynamicContractIntegrationTests.cs | 174 |
| Initializers/Factories/MessageInitializerBuilderContractTests.cs | 198 |
| Initializers/HeaderInitializers/HeaderInitializerContractTests.cs | 228 |
| Initializers/InitializerConventionRegistryTests.cs | 22 |
| Initializers/MessageInitializerCacheContractTests.cs | 96 |
| Initializers/MessageInitializerContractTests.cs | 663 |
| Initializers/MessageInitializerFactoryContractTests.cs | 30 |
| Initializers/MessageInitializerObjectGraphTests.cs | 361 |
| Initializers/MessageInitializerRequestResponseTests.cs | 117 |
| Initializers/MessageInitializerScalarConversionTests.cs | 403 |
| Initializers/PropertyConverters/ArrayPropertyConverterTests.cs | 44 |
| Initializers/PropertyConverters/CollectionPropertyConverterContractTests.cs | 353 |
| Initializers/PropertyConverters/DictionaryPropertyConverterTests.cs | 60 |
| Initializers/PropertyConverters/ListPropertyConverterTests.cs | 33 |
| Initializers/PropertyConverters/MessageDataPropertyConverterContractTests.cs | 213 |
| Initializers/PropertyConverters/PropertyConverterContractTests.cs | 136 |
| Initializers/PropertyConverters/ScalarPropertyConverterContractTests.cs | 220 |
| Initializers/PropertyConverters/StatePropertyConverterTests.cs | 122 |
| Initializers/PropertyConverters/TaskPropertyConverterTests.cs | 26 |
| Initializers/PropertyConverters/VariablePropertyConverterTests.cs | 197 |
| Initializers/PropertyInitializers/PropertyInitializerContractTests.cs | 298 |
| Initializers/PropertyProviders/AsyncPropertyProviderTests.cs | 58 |
| Initializers/PropertyProviders/PropertyProviderContractTests.cs | 142 |
| Initializers/PropertyProviders/PropertyProviderFactoryArrayTests.cs | 50 |
| Initializers/PropertyProviders/PropertyProviderFactoryContractMatrixTests.cs | 223 |
| Initializers/PropertyProviders/PropertyProviderFactoryDictionaryTests.cs | 77 |
| Initializers/PropertyProviders/PropertyProviderFactoryObjectGraphTests.cs | 81 |
| Initializers/PropertyProviders/PropertyProviderFactoryScalarTests.cs | 81 |
| Initializers/PropertyProviders/PropertyProviderFactoryTests.cs | 42 |
| Initializers/PropertyProviders/PropertyProviderFactoryVariableTests.cs | 24 |
| Initializers/PropertyProviders/PropertyProviderStateMatrixTests.cs | 358 |
| Initializers/PropertyProviders/PropertyProviderTestContext.cs | 48 |
| Initializers/PropertyProviders/TaskPropertyProviderTests.cs | 27 |
| Initializers/TaskInitializerExtensionsTests.cs | 386 |
| Initializers/TypeConverters/DateTimeTypeConverterTests.cs | 50 |
| Initializers/TypeConverters/ExceptionTypeConverterTests.cs | 29 |
| Initializers/TypeConverters/NumericTypeConverterTests.cs | 200 |
| Initializers/TypeConverters/SpecialTypeConverterTests.cs | 203 |
| Initializers/TypeConverters/TemporalTypeConverterTests.cs | 179 |
| Initializers/TypeConverters/TypeConverterContractTests.cs | 115 |
| Mediator/ContainerMediatorIntegrationTests.cs | 301 |
| Mediator/Contexts/MediatorMessageBodySerializerContractTests.cs | 141 |
| Mediator/Contexts/MediatorReceiveContextContractTests.cs | 115 |
| Mediator/Contexts/MediatorSendObserverTests.cs | 289 |
| Mediator/Contexts/MediatorSerializationContextContractTests.cs | 122 |
| Mediator/ExpiredForwardingMediatorTests.cs | 76 |
| Mediator/MediatorAdvancedApiTests.cs | 285 |
| Mediator/MediatorDispatchTests.cs | 545 |
| Mediator/MediatorFactoryContractTests.cs | 288 |
| Mediator/MediatorObserverContractTests.cs | 245 |
| Mediator/MediatorRequestApiContractTests.cs | 204 |
| Mediator/MediatorRequestHandlerContractTests.cs | 136 |
| Mediator/MediatorRequestTests.cs | 227 |
| Mediator/ScopedMediatorContractTests.cs | 338 |
| MessageData/AesGcmMessageDataEncryptionTests.cs | 308 |
| MessageData/MessageDataConfigurationApiTests.cs | 91 |
| MessageData/MessageDataConverterTests.cs | 111 |
| MessageData/MessageDataEndpointIntegrationTests.cs | 238 |
| MessageData/MessageDataInitializerIntegrationTests.cs | 360 |
| MessageData/MessageDataPublicApiTests.cs | 69 |
| MessageData/MessageDataRepositoryTests.cs | 497 |
| MessageData/MessageDataTestSupport.cs | 14 |
| MessageData/MessageDataTransportIntegrationTests.cs | 418 |
| MessageData/MessageDataValueTests.cs | 100 |
| MessageData/PropertyProviders/GetMessageDataPropertyProviderTests.cs | 181 |
| MessageData/PropertyProviders/PutMessageDataPropertyProviderTests.cs | 200 |
| MessageData/TestEncryptionKeyProvider.cs | 33 |
| Requirements/CoreRequirements.json | 5768 |
| Serialization/ArrayMessageTypeTests.cs | 63 |
| Serialization/CoreMessageBodyContractTests.cs | 53 |
| Serialization/DeserializeVariableExtensionsTests.cs | 45 |
| Serialization/DictionarySendHeadersTests.cs | 149 |
| Serialization/EnvelopeMetadataProjectionTests.cs | 317 |
| Serialization/ForwardExtensionsBoundaryTests.cs | 65 |
| Serialization/ForwardMessageTests.cs | 657 |
| Serialization/HeaderRoundTripTests.cs | 70 |
| Serialization/HostMetadataRoundTripTests.cs | 64 |
| Serialization/JsonMessageTypeMappingRegistryTests.cs | 209 |
| Serialization/JsonObjectConsumptionTests.cs | 56 |
| Serialization/MessageBodyContractAssertions.cs | 82 |
| Serialization/MessageIdHeadersTests.cs | 38 |
| Serialization/MinimalEnvelopeRedeliveryTests.cs | 163 |
| Serialization/PayloadAdmissionEvaluatorTests.cs | 304 |
| Serialization/PayloadAdmissionTransportIntegrationTests.cs | 801 |
| Serialization/Protobuf/ProtobufCompatibilityPayload.Populate.cs | 6 |
| Serialization/RawMessageContextTests.cs | 113 |
| Serialization/RejectingMessageSerializer.cs | 15 |
| Serialization/SerializationConfigurationTests.cs | 89 |
| Serialization/SerializationContractIntegrationTests.cs | 527 |
| Serialization/SerializationFaultTests.cs | 273 |
| Serialization/SerializerContextContractTests.cs | 82 |
| Serialization/ServiceBusMetadataSerializerTests.cs | 52 |
| Serialization/SystemTextJsonApplicationFormatCompatibilityTests.cs | 143 |
| Serialization/SystemTextJsonCollectionTests.cs | 313 |
| Serialization/SystemTextJsonConstructorBindingTests.cs | 78 |
| Serialization/SystemTextJsonDateOnlyTimeOnlyTests.cs | 205 |
| Serialization/SystemTextJsonDateTimeTests.cs | 47 |
| Serialization/SystemTextJsonDecimalTests.cs | 39 |
| Serialization/SystemTextJsonDictionaryConverterTests.cs | 290 |
| Serialization/SystemTextJsonExtensionDataTests.cs | 124 |
| Serialization/SystemTextJsonExtensionsTests.cs | 109 |
| Serialization/SystemTextJsonForwardingSerializerTests.cs | 72 |
| Serialization/SystemTextJsonInterfaceMetadataTests.cs | 63 |
| Serialization/SystemTextJsonIsolationTests.cs | 252 |
| Serialization/SystemTextJsonMessageBodyTests.cs | 140 |
| Serialization/SystemTextJsonMessageSerializerOptionsTests.cs | 36 |
| Serialization/SystemTextJsonMessageTypeAdmissionTests.cs | 118 |
| Serialization/SystemTextJsonObjectMessageBodyTests.cs | 87 |
| Serialization/SystemTextJsonPerMessageConfigurationTests.cs | 64 |
| Serialization/SystemTextJsonPolymorphismTests.cs | 146 |
| Serialization/SystemTextJsonRawMessageBodyTests.cs | 90 |
| Serialization/SystemTextJsonRoundTrip.cs | 91 |
| Serialization/SystemTextJsonRuntimeIsolationTests.cs | 199 |
| Serialization/SystemTextJsonScalarTests.cs | 54 |
| Serialization/SystemTextJsonTypeSafetyTests.cs | 41 |
| Serialization/TransportTextMessageBodyNormalizerTests.cs | 149 |
| Transformation/TransformPipelineTests.cs | 343 |
| Transports/BusDepotTests.cs | 58 |
| Transports/BusHealthLifecycleTests.cs | 169 |
| Transports/Components/KillSwitch/KillSwitchIntegrationTests.cs | 199 |
| Transports/Components/KillSwitch/KillSwitchSettingsTests.cs | 69 |
| Transports/Components/KillSwitch/KillSwitchTests.cs | 387 |
| Transports/ConsumerAgentTests.cs | 171 |
| Transports/EndpointProviderTests.cs | 504 |
| Transports/Fabric/MessageExchangeTests.cs | 130 |
| Transports/Fabric/MessageFabricTests.cs | 625 |
| Transports/Fabric/MessageFabricTopologyBuilderTests.cs | 91 |
| Transports/Fabric/MessageReceiverCollectionTests.cs | 127 |
| Transports/Fabric/TopicMessageExchangeTests.cs | 217 |
| Transports/HostConfigurationRetryExtensionsTests.cs | 205 |
| Transports/HostHandleApiTests.cs | 164 |
| Transports/ReceiveEndpointDispatcherTests.cs | 127 |
| Transports/ReceiveLifecycleTerminalityTests.cs | 812 |
| Transports/ReceiveMessageLimitsTests.cs | 55 |
| Transports/ReceivePipeDispatcherTests.cs | 228 |
| Transports/Receiving/ReceiveContextProxyTests.cs | 185 |
| Transports/Receiving/ReceiveLockContextTests.cs | 194 |
| Transports/RequestOutcomeTimeToLiveTests.cs | 134 |
| Transports/RiderCollectionTests.cs | 91 |
| Transports/SendEndpointCacheTests.cs | 140 |
| Transports/SendTransportTests.cs | 218 |
| Transports/Sending/MessageSendContextPropertyTests.cs | 296 |
| Transports/Sending/SentMessageMetadataTests.cs | 44 |
| Transports/TransportValueFormattingTests.cs | 226 |

## Complete additional source and graph reads

All source paths below are relative to src/; every comment is manually checked.
MediatorReceiveContext, BaseReceiveContext, serializer and body comments otherwise
match the current code and need no wholesale rewriting. The publish adapter is a
functional specialization, not an old API compatibility shim.

| Source path | Lines |
| --- | ---: |
| ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorReceiveContext.cs | 185 |
| ViciOne.ServiceBus/Transports/Receiving/BaseReceiveContext.cs | 275 |
| ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorSendEndpoint.cs | 400 |
| ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorPublishSendEndpoint.cs | 73 |
| ViciOne.ServiceBus/Serialization/Serializers/SystemTextJsonMessageSerializer.cs | 189 |
| ViciOne.ServiceBus/Serialization/Serializers/SystemTextJsonRawMessageSerializer.cs | 124 |
| ViciOne.ServiceBus/Serialization/Bodies/SystemTextJsonMessageBody.cs | 88 |
| ViciOne.ServiceBus/Serialization/Bodies/SystemTextJsonRawMessageBody.cs | 77 |
| ViciOne.ServiceBus/Serialization/Admission/BoundedPayloadSerializationBuffer.cs | 99 |

Additional repository-relative graph reads are Directory.Build.props (49),
Directory.Build.targets (149), tests/Directory.Build.props (51),
tests/Directory.Build.targets (149) and the Core csproj (45). They identify canonical
MTP execution settings, central packages, signing import, embedded requirements,
protobuf fixture generation and project-reference owners. Fixture protobuf tooling
is not used to generate source comments or review conclusions. Remaining import,
lock, execution and CI closure must be personally read/bound before admission.

## Coherent continuation and checkpoint

Secure the five owned paths with a normal scoped commit, new annotated checkpoint
tag and the approved atomic branch/tag push. Independently verify the exact branch,
tag object and peeled commit plus HEAD and clean owned work/index before reporting
remote security. No force, tag overwrite, protected-path staging or history-tail
replacement is permitted.

Then finish effective graph/full language-parser/GitReadSet admission and execute
connected causal remediation: cache constructor failure/resource ownership proof;
bounded lock/Yield observation and cleanup; exact receive proxy/fabric forwarding;
Mediator MIME isolation and settled notification contracts. Follow corrections with
meaningful independent compilable counterchanges and fresh bounded tests/builds.
Continue full productive src manual reading/comments, type/file/layout, API,
feature preservation, architecture, dummy/legacy/directives/format and genuine
coverage/CRAP/provider axes. Iterate complete reviews and fix newly established
findings; no unconditional 100% correctness or final A+ certificate is asserted.
