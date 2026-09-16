# Iteration 141 — validation and test-quality dispositions

## Current bounded result

The original whole-fork A+ source/API/architecture objective remains active. This packet corrects five caller-detachable waits after accepted variable resolution or downstream property conversion. It preserves the three intentional locally cancellable waits over caller-owned task-valued inputs: direct task input, converting task input and `MessageData.Value`.

No public signature, dependency, target framework or supported feature changed. The correction is limited to direct observation of accepted collaborator tasks plus functional ownership remarks.

The exact source admission contains 8 fully read files / 540 current lines and SHA-256 `5b60e4482a7d250a0654427e0d3efd36fd7520548500b7b334c81fbb4c50e034`. The owning-test admission contains 6 files / 1,252 current lines and SHA-256 `52e4484991b048a5b0d047c6fe80d4f041c59b531ad22a8e81e8e6baebe6057d`. Chaining them to Iteration 140 yields source chain hash `8de8c8dfb0f28b93eba2ffe0d7dc8407d883acc7ff572521451a557b4e68521d` and test chain hash `c6ab9271b724cf05aba63dea68367c8904f48f14b3972314c51a4669b582870e`.

## Causal baseline and correction

The handwritten ScalarPropertyConverterOwnershipTests fixture contributes 8 methods / 20 native cases. Before productive correction, the original 19-case fixture passed 4 caller-owned-boundary/identity cases and failed all 15 accepted-task outcome cases. Three initial test compiler errors were authoring failures, were corrected, and are not counted as behavioral evidence.

The corrected 19-case candidate passed 19/19. Assertion and pseudo-mutation review added the combined dependency/null-task guard, producing the final 20/20 focused result. Five existing relevant fixtures separately passed 20/20. CoreRequirements.json retains the exact 3,016-entry parent prefix and appends eight unique bidirectional bindings, giving 3,024 entries.

## Assertion, gap and mutation analysis

All eight public test methods contain meaningful assertions: 76 direct calls spanning equality, reference identity, exact type, booleans, synchronous throws, asynchronous throws and cancellation. They cover exact context/input/token forwarding, pending and terminal state, exact result/task/exception identity, producer cancellation token, caller-owned source incompletion, absence of premature downstream calls, and exact dependency/null-task diagnostics. Cleanup independently settles and observes every controlled task. All Task-returning tests have the Async suffix.

Static pairing ran once in an isolated Initializers-only copy: 90 source files / 50 tests / 78 paired / 12 unpaired. All four converter scope files are paired. This is navigation evidence, not behavioral or coverage proof.

Eight hand-authored single-cause productive mutations compiled with zero warnings/errors and were killed by unchanged tests: five reinstated caller-detachable waits after accepted work and three removed locally cancellable waits over caller-owned task inputs. Each mutation was restored immediately. The post-mutation focused fixture passed 20/20. The final scope has exactly three `WaitAsync(cancellationToken)` calls, all at the deliberate caller-owned boundaries.

## Requirement-to-evidence map

| Requirement | Final native evidence | Mutation witness |
| --- | --- | --- |
| Direct variable owns accepted value task | `DirectVariableConverter_OwnsAcceptedValueOutcomeAsync` — 3 cases | Reinstated detachment fails 3/3. |
| Converted variable owns both accepted stages | `ConvertedVariableConverter_OwnsEachAcceptedStageAsync` — 6 cases | Each reinstated stage detachment fails its 3 cases. |
| Nullable adapter owns accepted conversion | `ToNullableConverter_OwnsAcceptedConversionOutcomeAsync` — 3 cases | Reinstated detachment fails 3/3. |
| Converting task adapter owns accepted downstream work | `ConvertingTaskAdapter_OwnsAcceptedDownstreamOutcomeAsync` — 3 cases | Reinstated detachment fails 3/3. |
| Task inputs remain caller-owned | `TaskInputs_RemainCallerOwnedAndLocallyCancellableAsync` — 2 cases | Removing either local wait fails its case. |
| Message-data value remains caller-owned | `MessageDataValueInput_RemainsCallerOwnedAndLocallyCancellableAsync` — 1 case | Removing its local wait fails 1/1. |
| Reverse task wrapping preserves task identity | `TaskWrapping_PreservesTheAcceptedConversionTaskIdentityAsync` — 1 case | Exact inner reference and pending state asserted. |
| Dependencies and accepted task results are non-null | `ScalarOwnershipConverters_RejectMissingDependenciesAndAcceptedTasksAsync` — 1 case | Exact exception types, parameter names and messages asserted. |

## Terminal validation

Repository-prescribed warning-as-error, no-restore Release builds passed with disabled build servers, one MSBuild node, no node reuse and no shared compiler: ViciOne.ServiceBus.slnx in 55.26 seconds and ViciOne.ServiceBus.Tests.Unit.slnx in 1:21.77, both with zero warnings/errors. Both formatting verification gates exited 0 without findings.

The unfiltered native Core profile passed 4,792/4,792 in 27.162 seconds, with no failed, skipped or other results. Exact bag reconciliation retained all 4,772 parent names/multiplicities and added exactly the intended 20 cases with method multiplicities 3, 6, 3, 3, 2, 1, 1 and 1. Canonical sorted-name/newline SHA-256 is `12058bd2ca6ef40a649b7f8990a14388996e1d20cafd1457e1952caf97bb63b4`; CTRF SHA-256 is `ee7896c0c69a358ce67917df1412a7bfa3e8e1c3a7dc89bcd6b335d857806c8d`.

The selected coverage configuration SHA-256 is `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`. A separate 4,792/4,792 run measured 49,676/61,225 lines (81.1368%) and 17,154/23,326 branches (73.5403%) across the selected nine-assembly graph. Cobertura SHA-256 is `3a0be2f99c09ab7ba56e4f9152c7fae83844463ab63e85ceec4045cf5d0d9264`; coverage CTRF SHA-256 is `410fb3d3359c353da182f6a9238fc1cc3fbecc3659ede10d9ac5480ba015e1a0`.

The documented CRAP formula evaluated 18,148 methods, found 121 methods above CRAP 30 and 4,480 below the 80% line / 70% branch method thresholds, including 3,690 with 0% method-line coverage. The complete canonical below-threshold identity/newline SHA-256 is `175955020d1b4ece85dc99376330ad1c6177b0d3d0def84b66260eddbe7514ab`. The largest selected hotspot remains RequestRateAlgorithm's constructor at complexity 34, 0% coverage and CRAP 1,190. These are selected Core-graph metrics, not whole-fork coverage or attribution to this converter packet.

Raw artifacts remain under `/private/tmp/vsb-iteration141-final-core`, `/private/tmp/vsb-iteration141-coverage`, the isolated pairing directory and mutation directories. Protected review, TestResults and legacy trees were not read or modified.

## Remaining original-goal gates

This packet does not prove whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork coverage/CRAP or real durable-provider/external acceptance. Normal commit, annotated tag, atomic private push and independent remote verification remain the final checkpoint operations before the next connected audit packet.
