# Iteration 119 — nested API source reconciliation checkpoint

Status: bounded source review and manual comment remediation; the original
whole-product A+ goal and final iteration acceptance remain open.

## Authority and secured input

Starting product commit: `e282a224f1f5f8a6ced6b7162e27bb16997575b7`, branch
`feature/servicebus-a-plus-api`. Its annotated checkpoint tag is
`servicebus-a-plus-iteration-119-governed-traversal-generic-contracts-checkpoint-2026-09-15`.
The previous checkpoint's actual normal atomic push and independent remote
branch/tag/peeled-tag comparison succeeded. This continuation starts from that
secured tracked tree; it does not infer a backup from the editor change display.

The unchanged direct-Lead order is
`PO-2026-09-08-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`, with exact slice
`work/delivery/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/DEVELOPMENT_SLICE.json`,
SHA256 `5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.
The main Lead's complete normative reads are reused only after actual unchanged
hash checks. The new Licensing exception is not a ServiceBus authorization.
Source/project discovery remains scoped to explicitly owned tracked paths;
protected `review/**` and `TestResults/**` are neither inspected nor staged.

## Complete personal source reading and comments

The main Lead completely reads the following 24 source files, 1,872 starting
lines. All existing comments in these files are checked against their complete
implementation; 23 files receive manually authored XML documentation changes.
`SagaMetadataCache.cs` has no comment needing correction. The final selected
source scope contains 1,869 lines. Partial/truncated earlier output is recovered
before a file is counted as a complete read.

| Owner | Complete files |
|---|---|
| Core middleware | `DynamicFilter.cs`, `OutputPipeFilter.cs`, `TeeFilter.cs` |
| Sagas configuration | `SagaConnector.cs`, `SagaConnectorCache.cs`, `SagaMetadataCache.cs`, `ISagaMessageSpecification.cs` |
| SagaConnector partials | `SagaConnector.CorrelatedSagaMessageConnector.cs`, `SagaConnector.QuerySagaMessageConnector.cs`, `SagaConnector.SagaMessageConnector.cs`, `SagaConnector.SagaMessageSpecification.cs`, `SagaConnector.SagaMessageSplitFilterSpecification.cs`, `SagaConnector.SagaPipeSpecificationProxy.cs`, `SagaConnector.SagaSplitFilterSpecification.cs` |
| State-machine connector | `StateMachineInterfaceType.cs` and all five `StateMachineInterfaceType.*.cs` partials: identifier builder, fault-identifier builder, connector factory, saga message connector, event correlation configurator |
| Sagas policies/correlation | `Saga/AnyExistingSagaPolicy.cs`, `Saga/NewOrExistingSagaPolicy.cs`, `SagaStateMachine/MessageEventCorrelation.cs` |
| Abstractions configuration | `Configuration/Consumers/ISagaMessageConfigurator.cs` |

These are selected complete personal reads, not a claim that all product source
has now been read. The internal advisor's separate source reads are not counted
as personal Lead reads.

Corrections describe actual context conversion, connector contracts, saga-context
versus outer message-context middleware, configuration observation, repository
dispatch, policy selection and continuation behavior. In particular:

- Empty DynamicFilter registration completes without invoking `next`; multiple
  outputs receive empty continuations and successful dispatch then invokes `next`.
- Key selection operates on the converted output context through its shared
  input-context contract, not necessarily the original input object.
- `SelectId` replaces message identifier selection and returns the configurator;
  it neither returns an identifier nor replaces the saga filter factory.
- The property-expression `CorrelateById<T>` overload configures a non-default
  value property query, rather than necessarily a Guid identity lookup.
- `existingCorrelation` copies only matching message/filter factories, not all
  policy flags, creation settings or missing-instance configuration.
- AnyExistingSagaPolicy does not create instances; its default missing pipeline
  is empty, but a configured missing pipeline need not complete silently.
- TeeFilter counts directly connected pipelines; its keyed router is one such
  pipeline, not a count of all key registrations.

No generator or script writes source, tests or comments. A read-only whole-file
comparison removes only XML-documentation lines from HEAD/current contents:
all 23 changed source files then match byte-for-byte. Thus this checkpoint changes
no executable statement, type/member signature, visibility, directive, dependency
or project structure. This is a bounded feature-preservation observation for
the present edit, not acceptance of all prior API changes.

## Collision-preserving API disposition

The actual fresh API output from the secured input remains available at
`/private/tmp/vsb-iteration119-api-contract.9mkrjp/framework-fresh-packed-public-api.txt`,
SHA256 `96436e3b855dee3c678da4430a01c0947b14126227faff5a37be040455790676`.
The unchanged committed baseline is
`docs/api/packed-public-api.txt`, SHA256
`59ea05a49d8d99e64715ac60b79bc68f9b657948f0742fd9c3d3e972babd054b`.
No automatic baseline update/copy is performed.

The independent internal advisor confirms the main collision-preserving
diagnostic: 3,287 old blocks / 3,262 represented keys; 3,230 fresh blocks / 3,230
keys. Twenty-five extra old blocks occupy six colliding represented keys.
Every old block is retained in comparison; no overwriting dictionary is used
as acceptance evidence. Equal whole-family block counts distinguish corrected
nested identities from the 57 actual prior implementation visibility retirements.

This personal pass covers seven nested SagaConnector types, five nested
StateMachineInterfaceType types and three nested DynamicFilter types. Together
with the previous complete PipeConfigurator source reads, 22 of the 35 newly
qualified nested identities have corresponding complete personal source reads.
The remaining 13 state-machine nested identities still require personal complete
source/comment disposition. This fraction concerns only those 35 identities;
it is not a whole-API, whole-source or goal-completion percentage.

[The internal implementation-visibility review](INTERNAL_RETIRED_IMPLEMENTATION_SOURCE_REVIEW.md)
records 50 fully read implementation files and 19 retained entrypoint/interface
files. All 57 implementations remain present but internal. Their prior direct
construction/concrete inspection/subclassing is genuinely removed: that is an
intentional breaking visibility change, not a formatting correction. Retained
built-in retry/rescue/observation/classification/redelivery entrypoints are mapped;
a specialized custom consume-retry extension equivalence remains unproved.
This advice is internal static counterreview, not external losslessness acceptance.

## Connected findings — not silently closed by comment edits

| ID | Severity / evidence | Source and required next disposition |
|---|---|---|
| NST-01 | P1, direct static failure path | `SagaConnector<TSaga>.ConnectSaga<T>` disposes acquired handles in an unprotected catch loop. A disposal throw can mask the original connection failure and stop remaining cleanup. Preserve primary failure and release every acquired handle; prove multiple cleanup failures and exact ownership with causal runtime tests. |
| NST-02 | P1, direct static failure path | `OutputPipeFilter.SendToOutputAsync` awaits fault observers inside the catch. A callback failure can replace the original output failure and prevent subsequent fault notification. Define observer-fault precedence and notification completion, then prove synchronous/faulted/pending boundaries without business replay. Pre-send callbacks being outside the catch is a separate contract question, not automatically declared a defect. |
| NST-03 | P2, direct annotation/behavior mismatch | `SagaMessageSpecification.BuildMessagePipe` and `Message` treat non-nullable Action callbacks as optional through `?.Invoke`; interface signatures present them as required. Choose an explicit consistent optional or required contract across all implementations/callers and test null plus non-null configuration. |
| NST-04 | P2, direct nullable chain mismatch | Event correlation initializes non-nullable `_missingPipe` with `null!`, passes it through a non-nullable MessageEventCorrelation parameter, then reaches the intentionally nullable/default-empty AnyExistingSagaPolicy. Nullable `_messageFilter` and `_sagaFilterFactory` also have unnecessary `null!` initializers. Make absence honest across the complete chain; separately diagnose null results from the required missing-pipeline builder. |
| NST-05 | P2, incomplete boundary diagnosis | Selected public nested connectors/factories and policies store or dereference required inputs without immediate named guards; identifier extractors are captured for later dispatch. Review downstream guards and all public SPI result contracts before adding complete required-input/result oracles. Factory returning null during PreInsertInstance can contradict its conditional non-null result promise. This pass does not inject invalid SPI results or claim a runtime test gap from naming alone. |
| NST-06 | P2, static concurrency risk requiring proof | DynamicFilter locks registration but probes its live Dictionary without that lock. Concurrent registration can invalidate dictionary enumeration. Review dispatch array publication and reentrancy too; do not claim an empirically reproduced race or fix from static inspection alone. |
| NST-07 | P2, semantic API inconsistency | The Guid-selector CorrelateById overload uses identity dispatch; its arbitrary struct/property-expression overload uses property-query dispatch. SelectId only selects a message filter. Design coherent Greenfield naming/configuration while retaining both identity and property-query capabilities, then inspect all callers/package-only consumers. |
| NST-08 | P2, public structural/lifecycle question | `SagaConnector<TSaga,TMessage>` is an instantiable public partial container with no outer instance behavior; BuildConsumerPipe appends its consume filter on every build. Decide intentional container/SPI exposure and single-build versus repeat-build semantics with full caller/package evidence before structural changes. No API is removed in this checkpoint. |
| NST-09 | P3, direct redundant allocation | KeyOutputFilter's base constructor creates an unkeyed OutputPipeFilter/TeeFilter which the keyed subclass replaces with its own filter property. Remove the unused allocation only with verified virtual/protected extension semantics. This is not a false claim of virtual dispatch from the base constructor's get-only property assignment. |
| NST-10 | P2, qualified internal static extension risk | Previously derivable RetryConsumeContext implementations and specialized consume-aware wrappers/specifications are internal. Public policy/context contracts remain, but equivalent custom projection/deferred-fault composition has no newly executed package-only journey in this review. Establish equivalent Greenfield extension capability or supply the missing proper contract without restoring compatibility-only accidental exposure. |

These IDs are follow-up observations, not invented empirical test failures.
Existing tests may cover adjacent behavior; only targeted direct runtime evidence
and suitable compiled one-cause candidates can close the behavior findings.
Core's existing tests are executed for regression verification, not newly authored
under an unverified full-owner test-design admission.

## Current validation and artifact binding

Owned raw directory: `/private/tmp/vsb-iteration119-nested-source.9Z3lqp`.
The source/read and no-executable-change checks already execute successfully.
Strict Release builds, existing complete native tests and scoped read-only
whitespace verification are recorded here only after terminal observation.
The serial build process terminates 0 after all three strict Release owner
builds; each log contains zero warnings and errors. Native owner processes are
then started against those fresh artifacts and each terminates 0:

| Current check | Actual terminal result |
|---|---|
| Architecture strict Release build | Exit 0; zero warnings/errors; 54.48s |
| Core strict Release build | Exit 0; zero warnings/errors; 36.36s |
| Abstractions strict Release build | Exit 0; zero warnings/errors; 5.92s |
| Complete Core native owner | Exit 0; 4,007/4,007 passed, zero failures/skips; 25.752s |
| Complete Abstractions native owner | Exit 0; 749/749 passed, zero failures/skips; 1.165s |
| Complete Architecture native owner | Exit 0; 358/358 passed, zero failures/skips; 3m 12.174s |
| Read-only selected whitespace | Exit 0 for Core 3 files, Sagas 19, Abstractions 1; no warnings or writes |
| XML-documentation-only whole-file comparison | Exit 0; 23 edited sources, zero non-XML changes |
| Final exact retired identity report comparison | Exit 0; 57 actual/57 recorded unique identities, sets equal |
| Actual fresh package/developer-journey gate | Exit 1 only at unchanged committed API comparison, after 31 packages, 18 journey executions, three isolated consumer executions and 30 runtime assembly inventories |

Invocation follows the run-tests skill after complete personal skill reading
and actual SDK/global.json/project/root/test build and central package detection:
.NET 10.0.302, xUnit 4, Microsoft Testing Platform v2; native executables accept
`--results-directory` and `--report-xunit-ctrf` directly. No VSTest logger,
hybrid runner, unrecognized flag, additional package or filtered-zero-test
observation contributes acceptance. Shared config keeps failSkips/failWarns true.

The fresh Architecture CTRF explicitly records
`ViciOne.ServiceBus.Architecture.Tests.Product.AsyncApiConventionArchitectureTests.EveryMethodName_MatchesItsAsynchronousContractBidirectionally`
as passed (149,072ms). The full owner also includes generic identity/constraints,
runtime framework/traversal, requirement projection and source/test layout guards.
This is current actual execution, not a prediction from the older 358-case run.
It does not certify comments or all functional paths as 100% correct.

The unchanged actual package script is invoked with only
`PUBLIC_API_CONTRACT_OUTPUT=/private/tmp/vsb-iteration119-nested-source.9Z3lqp/fresh-packed-public-api.txt`.
Neither update switch is used. The native gate handle terminates 1, not 134,
after all package/consumer/runtime inventory stages succeed. Its fresh API
contains 20,045 lines and SHA256
`96436e3b855dee3c678da4430a01c0947b14126227faff5a37be040455790676`,
identical to the secured input's fresh inventory. Thus these comment edits
introduce no inventory-visible API delta; the old unreviewed baseline difference
still deliberately fails comparison. This is not a green package/API gate.
Transient freshly packed feeds are cleaned by the script's existing owned-temp
trap; source, committed baseline, tracked lock graphs and retained raw output
are not removed. Successful isolated testing-consumer execution is not cloud
provider acceptance.

No new runtime behavior, test method, catalogue tuple or mutation candidate is
introduced by this documentation-only checkpoint. The previous generic/traversal
mutation kills remain historical evidence, not newly executed mutation coverage.

The selected final source files and terminal validation artifacts are bound by
[the checkpoint hash manifest](NESTED_API_SOURCE_RECONCILIATION_HASHES.md).
The first validation confirms 109 listed bindings, including 93 source rows,
zero missing files/hash mismatches and both exact canonical advisor manifest
digests. Final package/tool bindings are added and checked after gate completion.
The final expanded manifest check terminates 0 with 118 bindings, zero missing
files/mismatches. Final tracked diff-check exits 0; repeated whole-file source
comparison still finds 23 changed source files and zero non-XML changes. All
owned build/test/format/gate handles are terminal before Git capture. The package
handle's intentional baseline exit 1 is not changed to a fictitious green verdict.
The preceding 95-binding manifest remains a historical secured input; it is not
rewritten to pretend its old source hashes describe new comments.

## Remaining original goal and connected sequence

1. Personally read and disposition the remaining nested state-machine sources,
   all their comments and the corresponding previously colliding old blocks.
2. Resolve advanced retry extension equivalence and complete fresh generic/member
   metadata disposition before any manually reviewed API baseline change.
3. Close NST-01–NST-09 through appropriate owner admission, causal tests, clean
   runtime/API corrections and actual compiled mutation checks, without feature loss.
4. Continue linked saga cancellation/acquisition/unwind/real rollback/Undo and
   cross-provider cleanup findings, timer/retry and real durable provider acceptance.
5. Finish all-source personal reads, all comments/type/file/folder/API/legacy/dummy/
   directive reviews and current global line/branch coverage plus multidimensional
   final gates. Neither overlapping owner graphs nor internal static advice prove
   100% product coverage, cloud acceptance or absolute correctness.

The folder question does not change this goal or authorize unnecessary nesting
of independent assemblies into Core. `docs/api-surface.md` remains the explicit
ownership decision. This checkpoint is a backup of bounded progress, not an
A+ completion, external acceptance or release-ready tag.
