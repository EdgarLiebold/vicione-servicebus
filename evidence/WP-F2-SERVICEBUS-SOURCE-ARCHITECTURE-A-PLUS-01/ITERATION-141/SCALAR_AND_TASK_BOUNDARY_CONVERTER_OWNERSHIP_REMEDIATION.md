# Iteration 141 — scalar and task-boundary converter ownership remediation

## Outcome and scope

This connected checkpoint corrects the remaining accepted-task ownership defects in variable, nullable and task-boundary scalar converter chains. It remains a bounded part of the original whole-fork A+ source/API/architecture objective, not a completion claim.

Parent checkpoint: `c8eb6ddd02b27711d78df8c190f6e515109d6b14`, normally committed, annotated-tagged, atomically pushed and independently verified. Its unfiltered Core multiset contains 4,772 passing cases. Iteration 141 adds 20 native cases without deleting or renaming a parent test or requirement binding.

Variable resolution and downstream conversion still receive the exact caller token. Once one of those collaborator tasks has been accepted, the converter now observes that original task to success, ordinary fault or producer cancellation instead of detaching through caller-local `WaitAsync`. Direct and converting task adapters retain local cancellation while they wait for a task supplied as input; no downstream work has been accepted then. `MessageData.Value` is the same caller-owned task-valued boundary and remains locally cancellable. The reverse task adapter continues to preserve the accepted conversion task as the exact wrapped result value.

No public signature, package, SDK pin, target framework or supported feature changed. Only three internal converter files change, and their functional remarks document the ownership boundary enforced by the code.

## Read admission and research

The main agent fully read every file in `.testagent/iteration141/source-admission.tsv`: 8 files / 540 current lines, manifest SHA-256 `5b60e4482a7d250a0654427e0d3efd36fd7520548500b7b334c81fbb4c50e034`. This includes all four converter scope files, the variable/message-data interfaces and the already-remediated provider analogues. The owning test admission contains 6 files / 1,252 current lines, manifest SHA-256 `52e4484991b048a5b0d047c6fe80d4f041c59b531ad22a8e81e8e6baebe6057d`.

Chaining those manifests to the Iteration-140 source/test chain hashes yields `8de8c8dfb0f28b93eba2ffe0d7dc8407d883acc7ff572521451a557b4e68521d` and `c6ab9271b724cf05aba63dea68367c8904f48f14b3972314c51a4669b582870e`. The chain records exact deltas without fabricating de-duplicated cumulative file or line totals.

The mandatory static pairing workflow ran once in isolated directory `/private/tmp/vsb-iteration141-pairing.piXBNB`. It classified 90 source files and 50 tests: 78 paired and 12 unpaired. All four converter scope files are paired. Pairing is a navigation heuristic, not behavioral or coverage proof.

## Test-first baseline and final assertions

ScalarPropertyConverterOwnershipTests is handwritten and contributes eight methods / 20 cases:

| Method | Cases | Evidence |
| --- | ---: | --- |
| DirectVariableConverter_OwnsAcceptedValueOutcomeAsync | 3 | Accepted variable success, exact ordinary fault and producer cancellation. |
| ConvertedVariableConverter_OwnsEachAcceptedStageAsync | 6 | Variable and downstream conversion stages × three outcomes. |
| ToNullableConverter_OwnsAcceptedConversionOutcomeAsync | 3 | Accepted nullable conversion × three outcomes. |
| ConvertingTaskAdapter_OwnsAcceptedDownstreamOutcomeAsync | 3 | Caller-owned source completes, then accepted conversion × three outcomes. |
| TaskInputs_RemainCallerOwnedAndLocallyCancellableAsync | 2 | Direct/converting input waits cancel locally without completing the source or starting conversion. |
| MessageDataValueInput_RemainsCallerOwnedAndLocallyCancellableAsync | 1 | Message-data input wait cancels locally without completing its value task. |
| TaskWrapping_PreservesTheAcceptedConversionTaskIdentityAsync | 1 | Exact pending conversion task is preserved as the wrapped value. |
| ScalarOwnershipConverters_RejectMissingDependenciesAndAcceptedTasksAsync | 1 | Constructor and null-task guards retain exact diagnostics. |

After the fixture compiled, unchanged productive waits produced 4 passed / 15 failed across the original 19 cases. The four passing cases were the intentional caller-owned boundaries and task-identity behavior; every accepted-task outcome case failed. Three earlier compiler diagnostics were test-authoring failures and are not counted as behavioral evidence.

The corrected candidate passed 19/19. Complete assertion and pseudo-mutation review added the dependency/null-task guard, producing the final 20/20 focused result. Existing TaskPropertyConverterTests, VariablePropertyConverterTests, ScalarPropertyConverterContractTests, MessageDataPropertyConverterContractTests and PropertyConverterContractTests separately passed 20/20 before complete host validation.

The fixture contains 76 direct assertions: 40 equality, 11 reference identity, 8 asynchronous throws, 3 synchronous throws, 3 cancellation throws, 5 true, 5 false and 1 exact-type assertion. They cover exact context/input/token forwarding, pending and terminal states, exact result/task/exception identity, producer cancellation token, caller-owned source incompletion, absence of downstream calls and exact dependency/null-task diagnostics. Cleanup independently settles and observes all controlled tasks. No assertion-free, tautological or fire-and-forget test remains, and every Task-returning test has the Async suffix.

CoreRequirements.json retains the exact 3,016-entry parent prefix and appends eight unique entries. The complete Core host's compiled metadata projection passed, providing bidirectional method/binding evidence rather than a JSON-only check.

## Compiled single-cause mutations

Eight counted mutations compiled cleanly and were killed by unchanged relevant tests:

| Mutation | Relevant result |
| --- | ---: |
| Reinstate direct-variable accepted-task detachment | 3 failed / 3 |
| Reinstate converted-variable input-stage detachment | 3 failed / 6 |
| Reinstate converted-variable downstream-stage detachment | 3 failed / 6 |
| Reinstate nullable downstream detachment | 3 failed / 3 |
| Reinstate converting-task downstream detachment | 3 failed / 3 |
| Remove direct task-input local cancellation | 1 failed / 2 |
| Remove converting task-input local cancellation | 1 failed / 2 |
| Remove message-data value local cancellation | 1 failed / 1 |

The first mutation test attempt compiled successfully but the sandbox denied a testhost named pipe; its escalated rerun supplied the behavioral result. Every mutation was restored immediately. Final product hashes match the source admission, and the post-mutation fixture passed 20/20. The scope contains exactly three remaining `WaitAsync(cancellationToken)` calls: TaskPropertyConverter lines 32 and 83 plus MessageDataPropertyConverter line 74, each on caller-owned input.

## Requirement-to-evidence map

| Requirement | Native evidence | Causal evidence |
| --- | --- | --- |
| Direct variable owns accepted value task | 3 outcomes | Original 3 failures; reinstated wait killed. |
| Converted variable owns both accepted stages | 6 stage/outcome cases | Original 6 failures; each stage wait killed independently. |
| Nullable adapter owns accepted conversion | 3 outcomes | Original 3 failures; reinstated wait killed. |
| Converting task adapter owns accepted downstream work | 3 outcomes | Original 3 failures; reinstated downstream wait killed. |
| Task inputs remain caller-owned and locally cancellable | 2 adapter cases | Both original cases passed; each removed wait killed. |
| Message-data value remains caller-owned | 1 boundary case | Original case passed; removed wait killed. |
| Reverse task wrapping preserves identity | 1 identity case | Original case passed; exact reference/pending state retained. |
| Dependencies and accepted task results are non-null | 1 guard case | Exact types, parameter names and diagnostics retained. |

## Terminal validation and selected coverage

| Gate | Result | Duration |
| --- | --- | ---: |
| ViciOne.ServiceBus.slnx Release build, no restore, warnings as errors | 0 warnings / 0 errors | 55.26 s |
| ViciOne.ServiceBus.Tests.Unit.slnx Release build, no restore, warnings as errors | 0 warnings / 0 errors | 1:21.77 |
| Engineering formatting verification | Exit 0, no findings | — |
| Unit formatting verification | Exit 0, no findings | — |
| Unfiltered native Core | 4,792 passed; 0 failed/skipped/other | 27.162 s |

Builds used disabled build servers, one MSBuild node, no node reuse and no shared compiler. They followed `docs/build.md`: solution builds did not combine `--no-incremental` with the strict gate.

Exact multiset reconciliation retained every one of the 4,772 parent names/multiplicities and added exactly 20 intended cases with method multiplicities 3, 6, 3, 3, 2, 1, 1 and 1. Canonical sorted-name/newline SHA-256 is `12058bd2ca6ef40a649b7f8990a14388996e1d20cafd1457e1952caf97bb63b4`; final Core CTRF SHA-256 is `ee7896c0c69a358ce67917df1412a7bfa3e8e1c3a7dc89bcd6b335d857806c8d`.

The current coverage configuration SHA-256 is `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`. A separate complete native run passed 4,792/4,792 and measured the selected nine-assembly product graph at 49,676/61,225 lines (81.1368%) and 17,154/23,326 branches (73.5403%). Cobertura SHA-256 is `3a0be2f99c09ab7ba56e4f9152c7fae83844463ab63e85ceec4045cf5d0d9264`; coverage CTRF SHA-256 is `410fb3d3359c353da182f6a9238fc1cc3fbecc3659ede10d9ac5480ba015e1a0`.

The documented CRAP formula evaluated 18,148 methods: 121 exceed CRAP 30; 4,480 are below the 80% line / 70% branch method thresholds, including 3,690 at 0% measured method-line coverage. Their complete canonical identity/newline SHA-256 is `175955020d1b4ece85dc99376330ad1c6177b0d3d0def84b66260eddbe7514ab`. RequestRateAlgorithm's constructor remains the largest selected hotspot (complexity 34, 0% coverage, CRAP 1,190). This is selected Core-graph coverage, not whole-fork coverage, and unrelated hotspots are not attributed to this converter packet.

Raw evidence remains under `/private/tmp/vsb-iteration141-final-core`, `/private/tmp/vsb-iteration141-coverage`, the isolated pairing directory and mutation directories. Nothing is staged into protected review, TestResults or legacy trees.

## Checkpoint and original-goal continuation

The checkpoint includes three corrected product files, the new ownership fixture, the requirement-ledger append, five iteration records and this evidence report. The intended annotated tag is `servicebus-a-plus-iteration-141-scalar-and-task-boundary-converter-ownership-remediation-2026-09-16`. Normal commit, atomic private push and independent branch/tag-object/peeled-tag verification occur after final non-protected diff review; this report does not pre-claim them.

Whole-fork personal source/comment reading, global API/new-parameter behavior coverage, global naming/type/file/directory audits, legacy/dummy/directive absence, whole-fork coverage/CRAP and real durable-provider/external acceptance remain open. The next connected packet will classify remaining initializer-layer asynchronous boundaries; successful scalar converter gates do not certify those paths.
