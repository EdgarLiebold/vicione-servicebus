# T46: combined transport-header contracts — in progress

Base4a4296133 (T45 audited and remote-confirmed). PO explicitly requests larger
coherent packets. ActiveMQ, RabbitMQ and SNS/SQS form one implementation/review
packet with one final full33 measurement; no such measurement has started.
Microsoft code-testing-agent research/plan/implement workflow applies, with
run-tests, find-untested-sources, assertion-quality and test-gap-analysis.

## Bounded research and pairing

Six source files and six existing tests copied byte-identically to
`/private/tmp/servicebus-t46-pairing`; Microsoft Roslyn analyzer executed once.
`artifacts/t46-pairing.json`, exit0:6sources/6tests,3paired/3unpaired.
Paired: SqsHeaderValueConverter via SqsMoveTransportTests; RabbitMqHeaderProvider
and MoveTransportHeaders via their named tests. Unpaired: ActiveMqHeaderProvider,
PrimitiveMapHeaders and SnsHeaderValueConverter. Static pairing is not runtime
coverage. Suggested ActiveMQ paths are under the LocalIntegration test project;
new provider/adapter cases use the existing unit project because they require
native SDK objects but no broker. Suggested SNS path is
`tests/Transports/ViciOne.ServiceBus.AmazonSqs.Tests/AmazonSqsTransport/SnsHeaderValueConverterTests.cs`;
shared converter assertions are in AmazonHeaderValueConverterTests instead.
Exact suggested paths and classifications remain in the analyzer output.

Six target sources and relevant existing header tests read completely.
Candidate detail: ignored `artifacts/t46-header-candidate-research.md`.
Read-only planning review corrected a candidate assumption: supported scalar
non-string values DO invoke the string filter after normalization. Only opaque
objects/byte arrays must bypass it. Existing SQS integration filter and scalar
normalization tests are retained, not duplicated as separate behavior claims.

## Acceptance mapping and implementation

| Contract | Test methods | Added cases |
| --- | --- | ---: |
| Native map overwrite/remove preserves neighbor values and wire types | PrimitiveMapHeadersTests.Set_ChangesOnlyThePermittedNativeProperty | 8 |
| String replace/remove, all readers, invalid writes, explicit unsupported typed retrieval | Remaining four PrimitiveMapHeadersTests methods | 4 |
| Native IDs cannot be replaced by application properties; both OpenWire and AMQP preserve UTC milliseconds and UTC epoch eligibility | ActiveMqHeaderProviderTests methods | 8 |
| Missing broker timestamp cannot be spoofed; scalar lookup agrees with enumeration | RabbitMqHeaderProviderTests.MissingTimestamp_CannotBeSuppliedByAnApplicationHeader and ScalarLookup_AgreesWithEnumerationWithoutLosingFalsyValues | 7 |
| Empty header table/string removal preserves neighbors | MoveTransportHeadersTests.EmptyTableAndStringRemoval_PreserveAbsenceAndUnrelatedValues | 1 |
| SNS/SQS filter receives normalized scalar and controls output in both overloads | AmazonHeaderValueConverterTests.SnsFilter_ReceivesInvariantScalarAndControlsProviderAttribute and SqsFilter_ReceivesInvariantScalarAndControlsProviderAttribute | 8 |
| Opaque objects never invoke filter and return default output | AmazonHeaderValueConverterTests.SnsUnsupportedValues_DoNotInvokeFilter and SqsUnsupportedValues_DoNotInvokeFilter | 4 |

Existing RabbitMQ overwrite test strengthened with immediate preservation/addition
assertions, before later overwrite/removal could mask failures. This candidate
escaping mutation is still static reasoning, not a confirmed survivor.

## Current evidence and findings

- ActiveMQ adapter focused12/12, no skips (`t46-activemq-primitive.log`).
- RabbitMQ focused20/20, no skips (`t46-rabbit-headers.log`).
- Initial Amazon build aborted with MSBuild4166 worker failures before tests.
  Serial build exposed duplicate RequirementCoverage attributes, corrected by
  provider-specific test entry methods. Corrected serial build exit0 and focused
  test process exit0,12/12 without skips (`t46-amazon-corrected.log`).
- ActiveMQ provider initial3/3fail (`t46-activemq-provider.log`). Two identity
  expectations used a malformed native ID or assumed the SDK returned null for
  an unset ID. Pinned2.2.0 decompilation confirms empty string instead; manually
  corrected test setup and expected native value, no product change for IDs.
  The corrected17-case run initially retained one native-ID parsing failure
  (`t46-activemq-corrected.log`,16/17). Final setup constructs native MessageId
  and ProducerId fields directly; expected public identifier remains literal.
- Real SDK timestamp test observes12:34:56.789Z becoming14:34:56.789Z. Product
  previously marked the native DateTime as UTC without conversion. Independent
  pinned-SDK review confirms OpenWire returns Local and AMQP returns Utc.
  Product correction converts Local to UTC before comparison with the epoch;
  Utc and Unspecified preserve the existing UTC interpretation.
- Corrected serial build has0warnings/0errors; focused ActiveMQ17/17, no skips,
  explicitly under `TZ=Europe/Berlin` (`t46-activemq-native-id.log`,exit0).
  Includes exact UTC milliseconds and timestamp eligibility at/before epoch.
- Read-only implementation review found no concrete blocker. It identified
  OpenWire-only execution as a proof limit. Timestamp and epoch cases now use
  both real SDK message implementations, without broker/connection operations.
  Expanded main run passes21/21, including requirement projection, without
  skips (`t46-activemq-both-providers.log`,exit0,`TZ=Europe/Berlin`).
- Isolated RabbitMQ baseline passes21/21 including requirement projection.
  Bypassing overwrite protection fails exactly the strengthened intermediate
  assertion: expected `first`, actual `ignored` (1/5 failures,exit2,
  `t46-rabbit-overwrite-mutant.log`). Mutation was manually reverted;
  restored focused verification passes21/21. This proves detection by the
  strengthened test, not a measured survivor in the historical suite.

## Adversarial checks and restored controls

All mutations were applied manually in the isolated GATE checkout, one product
change at a time. Each baseline was green; each mutation was reverted before
the next counterprobe. All executions use explicit `TZ=Europe/Berlin`.

| Injected fault | Failing/total cases | Restored control |
| --- | ---: | ---: |
| RabbitMQ overwrites an existing header despite overwrite=false | 1/5 | 21/21 |
| ActiveMQ marks Local as UTC instead of converting | 3/8 | 21/21 |
| SNS constructor discards the configured filter | 4/12 | 13/13 |
| SQS constructor discards the configured filter | 4/12 | 13/13 |

Restored controls include each changed requirement projection. Four meaningful
faults detected, zero observed survivors among those four; this is not an
exhaustive mutation score. The timestamp mutant's five passing controls are
three AMQP cases and two unchanged identity cases. No broker delivery or AMQP
wire-roundtrip proof is claimed by these SDK-object tests.

`artifacts/t46-counterprobe-evidence.json` binds11 execution logs by SHA-256 and
records exact counts. All four mutated product files are byte-identical to MAIN
after restoration. Read-only re-review of the real AMQP facade helper finds no
blocker: initialization and property access require no live connection.

Assertion-quality review follows the Microsoft .NET extension: no new method
is assertion-free or trivial-only (shared Amazon helpers are included). Checks
cover exact values/types, absent results, full collection contents, persisted
native-map effects, prohibited mutation of neighbors, error types/parameters
and exact filter-call arguments. The wire roundtrip has independent expected
native types/values. No assertion diversity is added solely to inflate metrics.
Test-gap review keeps unexecuted mutation ideas as static candidates; no
historical-survivor or exhaustive-coverage claim is made.

## Packet cadence

The PO reiterated larger packages during implementation. All40 added cases and
the product timestamp correction remain one transport-header packet. Narrow
tests resolve concrete failures during implementation; full33, final evidence
audit and documentation closure happen once for the combined packet. No new
global discovery or profiling pass is justified by an individual test change.

Verify-only whitespace check exits0. Its workspace warning concerns an analyzer
project reference without a matching metadata reference; the diagnostic repeat
confirms all three target test projects and ActiveMQ product project loaded,
with no formatting changes required (`t46-format-diagnostic.log`). No automatic
source/comment editing was performed. `git diff --check` also passes.

Remaining: canonical change list,
implementation commit, ONE full33 measurement, independent evidence audit,
documentation completion and push. Packet and global A+ remain open.
