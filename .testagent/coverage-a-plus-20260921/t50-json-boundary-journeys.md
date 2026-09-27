# T50 — JSON value, envelope admission and forwarding boundaries

Status: implementation, combined read-only review, counterprobes, restored
controls and verify-only formatting complete. Baseline is completed/pushed
T49 `fd3c3297632dade31ad8cca589b29c2deadb89a0`; no T50 measurement yet.
The preceding goal turn made progress by completing T49 and verifying its push.

## Requirements and bounded research

- "schneide größere pakete": one connected serialization package spanning value
  conversion, envelope metadata, copy admission, forwarding and lazy contract
  materialization. Complete these families before the combined review/measurement.
- "nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und
  Regressionen hart prüfen": exact payloads, boundary sizes, failure ownership,
  retained metadata and independent controls; no converter-constructor padding.
- Mandatory Microsoft code-testing-agent Research → Plan → Implement workflow,
  find-untested-sources, test-gap-analysis and test-analysis-extensions/.NET applied.
  Apply run-tests and assertion-quality at their execution/review gates.
- One combined read-only adversarial review, discriminating isolated counterprobes,
  restoration controls, exact-commit full33, numerical audit, records and push.
  Global A+ and the subsequent all-repository Roslyn API/comment audit remain open.

One Roslyn pairing run over a byte-identical bounded mirror contains 60 production
files, 50 existing test files and two project files (112 hashed inputs). It finds
32 paired and 28 unpaired source files. Exact classifications, suggested paths and
covering tests are retained in `artifacts/t50-pairing.json`; input hashes are in
`artifacts/t50-pairing-inputs.json`. This is a static heuristic, not test coverage:
StringDecimalJsonConverter is unpaired despite existing decimal behavior tests.
The existing Serialization test directory is used for combined contract tests.

Primary source families: JSON dictionary/decimal converters; JsonEnvelopeMessageValue
and AdmittedCopyMessageBody; JSON serializers/contexts and forwarding serializer.
The broader mirror also retains supporting metadata/admission bodies. Encryption
and reliable-store implementations are not implementation targets for this packet.

Existing dictionary tests prove scalar kinds, ordinary nested values, duplicate
last-write behavior and valid URI round trips. Decimal tests prove only maximum
quoted representation. Forwarding tests prove one recursive overlay, array append
and shape replacement. Existing bounded-body tests prove generic locator/writer
limits and snapshots; inspect their applicability before adding JSON-specific cases.
Potential gaps are static hypotheses until tests/counterprobes establish evidence.

## Acceptance map and implementation order

| Family | Planned concrete evidence |
| --- | --- |
| Decimal wire contracts | `DecimalWireValues_PreserveInvariantMeaningAndCanonicalOutputAsync`: numeric/string/null/blank boundaries, exponent/sign and exact canonical output under de-DE culture. |
| Dictionary validation and recursive values | `NestedDictionary_RetainsTypedValuesAndCaseInsensitiveLookupAsync` plus the malformed-payload test: string/URI malformed shapes and keys, nested values, exact lookup and output. |
| Configured JSON envelope admission | `ConfiguredEnvelope_ChargesTheExactApplicationValueAndDeliversOnlyAdmittedBytesAsync` and `AmbiguousEnvelope_IsRejectedBeforeSendAdmissionAndLeavesTheNextMessageOperationalAsync`: custom metadata naming, case policy, exact body/whole-envelope boundaries, missing/duplicate/nonobject input, no PreSend/provider effect after rejection. |
| Forwarding with replacement metadata | `ForwardedInterface_PreservesUnknownPayloadAndKeepsTargetMetadataIndependentAsync` proves two real transport destinations. `ForwardedReplacement_PreservesNestedUnknownValuesAndIsolatesSiblingMessages` exercises serializers obtained through deserialized contexts, nested objects/arrays/nulls, retained source and sibling isolation. The latter is serializer-context evidence, not another transport journey. |
| Lazy malformed message materialization | `MalformedPayload_RejectsTheDeclaredContractWithoutContaminatingTheNextMessageAsync`: distinguish envelope parsing from payload conversion, exact ReceiveFault owner/type, no partial consumer effect and subsequent valid message. |

Use focused checks during the combined implementation. Do not run full33 after
each family or test file. Record concrete reviewer findings and results here;
update the acceptance map to the final exact test names before claiming completion.

## Implementation/review checkpoint

Three new files contain seven test methods and 44 cases, with seven exact
requirement bindings. Read-only review
identified a resolver-priority oracle weakness: metadata and naming policy both
said payload. The retained-AttributeProvider cases now use resolver name content
while policy says payload; stripped metadata retains the fallback-name cases.
All fixtures now use the configured OperationTimeout. The reviewer found no
additional concrete ownership/race blocker in this static pass.

Initial compilation exposed test-authoring errors (raw-string brace escaping,
observer Async names, transport body access, endpoint cancellation argument and
the xUnit Assert.Single predicate rule); these are corrected. The first 45-case
baseline passed 43 and failed two forwarding cases. It is not acceptance evidence.
Tests now decode received byte bodies explicitly, observe final contract names
after body materialization, and distinguish raw infrastructure transport headers
from the deliberately filtered application-header view. All 56 combined new,
projection and existing RawMessageContext/ForwardMessage controls subsequently
pass without failures/skips; Release compilation has zero warnings/errors.

## Confirmed product correction

Raw forwarding through an interface discarded the original concrete contract URN.
SystemTextJsonRawSerializerContext.GetMessageSerializer now passes its retained
SupportedMessageTypes into the existing forwarding serializer constructor, which
already makes a defensive copy. The exact regression fails on the original
implementation (one raw case; three forwarding controls pass). The correction
restores both concrete consumer deliveries while retaining distinct destination
metadata and the exact original contract-name sequence. No other product behavior
is changed. The changelog records this defect.

The read-only reviewer confirms the minimal source change and strengthened
oracles, including raw header filtering and immediate malformed-payload detection
through Task.WhenAny(fault, consumer). No concrete blocker remains in that review.

## Empirical counterprobes

| Isolated one-cause change | Observed focused outcome |
| --- | --- |
| Omit resolver MemberInfo priority | 3 failures / 12 passing controls; metadata-name cases reject rather than delivering the admitted message. |
| Accept invalid decimals as zero | 5 failures / 20 passing controls; incorrect consumer dispatch wins the terminal race instead of ReceiveFault. |
| Omit the forwarding customization pipe | 2 failures / 2 passing controls; the independent target correlation/header contract is violated. |

All counterprobe builds succeed without warnings/errors. Every deliberate change
is manually reversed and all three original source SHA-256 values are verified
against `artifacts/t50-source-restore-hashes.json`; no counterprobe remains applied.
Final restored controls pass 56/56, zero failures/skips and zero build warnings/errors.
Verify-only formatting passes with zero of 5,730 files changed after five manually
corrected line breaks. All 5,880 MAIN/GATE source/test files are byte-identical.
Next: implementation freeze, the sole full33 measurement, independent
numerical/integrity audit and authorized publication.

Read-only measurement-helper review confirms the T49 baseline, Core +44 (13,076
expected total executions), unchanged 33-profile/provider/CPU selection and four
fixture groups. Comparison requires exactly one changed source statement and
separates it as removed/added; it does not misclassify changed bytes as unchanged
line identities. Replace the pending exact-commit marker only after freezing.

Assertion-quality review: all seven methods contain meaningful equality plus
collection/structural, negative or side-effect assertions; exception paths verify
specific stages, sizes or fault ownership. None is assertion-free, trivial-only
or self-comparing. Integration cases exercise actual dispatch; local overlay cases
are explicitly scoped to serializer behavior. Claims exclude exhaustive RawOptions
combinations and global A+ closure. Exact measurements remain the T49 baseline.

## First full-profile failure and correction

The full33 attempt at `352ee8a7da6c09d24bac1b36b084a9839af10024`
terminated with exit 1 after twelve verified profiles. Quartz passed 281/284;
the three failures were the raw variants of serialized-payload delivery, chained
scheduling and registered-container delivery. Original logs and receipts remain
under `artifacts/t50-profile-12` and `artifacts/t50-profile-progress.json`.

Quartz persists contract URNs separately from application headers. Its replay
pipe restores those URNs to SendContext after deserialization. The unconditional
T50 forwarding change later overwrote them with an empty source contract array.
Read-only review confirmed this path and acknowledged that the earlier review
missed it. The parameterless raw serializer factory now passes null for an empty
original contract array; nonempty forwarding contracts are still preserved.
The explicit replacement-contract overload is unchanged.

The three existing raw regression cases and their envelope controls pass 6/6
after correction, with zero failures/skips and a zero-warning/error build.
Evidence: `/private/tmp/t50-quartz-correction-build.log` and
`/private/tmp/t50-quartz-correction-tests.log`. Combined Core controls also pass
56/56 with zero failures/skips and a zero-warning/error build; evidence is in
`/private/tmp/t50-correction-core-build.log` and
`/private/tmp/t50-correction-core-tests.log`. A fresh exact-commit full33 remains
pending. This failed attempt does not replace T49 metrics.
