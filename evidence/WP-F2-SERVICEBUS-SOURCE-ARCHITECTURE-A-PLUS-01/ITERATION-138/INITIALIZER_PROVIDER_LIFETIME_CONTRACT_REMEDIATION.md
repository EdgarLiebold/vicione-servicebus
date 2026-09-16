# Iteration 138 — initializer provider lifetime contract remediation

## Outcome and scope

This connected iteration corrects the lifetime contract of ProviderPropertyInitializer, ProviderHeaderInitializer, SetHeaderInitializer and both AsyncPropertyProvider generic variants. The main agent personally read and understood all four owning source files and all their comments (305 current lines), then authored every correction and functional English comment by hand. No authoring generator was used. This is a bounded completion packet within the original whole-fork A+ API/source/architecture goal, not a declaration that the whole goal is complete.

Parent checkpoint: 0667256e619d3f0434ec3887425bb15e11b6229d, normally committed, annotated-tagged, atomically pushed and independently verified against the branch, tag object and peeled tag refs. The parent Core case multiset contains 4,545 passed cases. Iteration 138 adds 100 cases without deleting an existing method or requirement binding.

Started provider operations are now directly observed to their original success, ordinary fault or provider cancellation. Caller cancellation is forwarded cooperatively but cannot locally abandon an accepted provider task. The converting async adapter also observes its accepted converter task. Caller-owned task-valued input remains locally cancellable: stopping that wait does not settle or cancel its original producer. TaskPropertyProvider deliberately exports the exact original task-valued operation without flattening it and is unchanged.

Pre-start context/destination guards, precancellation effects, exact null-task diagnostics, successful null/default handling, accepted-stage continuation and assignment destinations are preserved. A runtime message type mismatch still prevents property assignment, but only after the accepted provider has settled; its fault or cancellation cannot be hidden by the skipped write. A null inner task still yields default without conversion; a non-null completed inner task whose result is null still invokes the converter with null. The latter is existing functionality protected by four additional cases, not a newly added productive feature.

The only removed parameter is an unused private ProviderHeaderInitializer.ApplyAsync cancellation parameter. Public signatures and supported features are retained. No compatibility bridge, dependency/SDK/pinning change, convenience suppression directive or source-directory migration is introduced. Internal generic type names and their owning filenames remain coherent. Full-fork dummy/legacy/directive and directory/type audits are not inferred from these four files.

## Test construction and causal baseline

The 704-line InitializerProviderLifetimeTests fixture is handwritten and was personally read in full, including its helpers and independent cleanup. It uses actual initialization/send contexts, independently controlled original tasks, separate caller/provider cancellation tokens, exact exception and reference identities, collaborator effect witnesses and destination preservation. Runtime theory data and loops are parameterization, not an authoring generator.

The unchanged productive parent first compiled with zero warnings/errors. The initial 96-case fixture produced 64 passed and 32 failed cases: nine provider-backed assignment failures, fourteen outer-provider failures, three held-converter failures, three converter-after-canceled-outer failures and three derived-runtime ownership failures. The corrected candidate compiled freshly with zero warnings/errors and passed the exact same 96 case names/multiplicities. Lead source/assertion review then identified the missing non-null-task/null-result combination; four cases expanded the fixture to 100. A fresh expanded-candidate compiler had zero warnings/errors, and all 100 native cases passed with no pending, skipped or other results.

The existing ProviderInitializer_AssignsOwnedMessagesAcrossEveryProviderTaskStateAsync case remains present. Its incorrect provider-abandonment assertion now requires pending ownership, exact token forwarding, original cooperative cancellation and independent original/root observation. All its other assertions remain. The 3,002-row CoreRequirements ledger retains the exact semantic 2,990-row parent prefix and adds 12 unique requirement/variant/method bindings, reconciled in both directions with the fixture attributes.

All methods below belong to ViciOne.ServiceBus.Tests.Initializers.InitializerProviderLifetimeTests:

| Method | Native cases | Contract evidence |
| --- | ---: | --- |
| Initializers_OwnHeldProviderAndOriginalOutcomeAsync | 18 | Three assignment stages, three original outcomes, both caller-cancellation states; pending ownership, exact context/token and destination effects. |
| Initializers_ValidateArgumentsAndPreCancellationBeforeProviderEffectsAsync | 6 | Synchronous guards before dependency effects, precancellation and exact forwarded caller token. |
| Initializers_PreserveImmediateOutcomesAndNullTaskDiagnosticsAsync | 3 | Each stage internally covers immediate success, null value, fault, provider cancellation and null task with exact diagnostics. |
| AsyncAdapters_OwnOuterProviderAndOriginalOutcomeAsync | 32 | Both variants, eight outer/inner outcome combinations and both caller-cancellation states; null task versus null result and exact conversion effects. |
| AsyncAdapters_KeepCallerOwnedInnerValuesLocallyCancellableAsync | 12 | Both variants, three original inner outcomes and both caller-cancellation states; local cancellation leaves input unsettled and starts no converter. |
| AsyncConverter_OwnsHeldConversionAndOriginalOutcomeAsync | 6 | Accepted converter ownership, exact context/input/token and original outcome. |
| AsyncConverter_OwnsHeldConversionAfterCanceledOuterResolutionAsync | 3 | Successful accepted outer resolution starts and owns conversion with the original already-canceled token. |
| TaskAdapter_ExportsExactOriginalOperationWithoutAwaitingAsync | 6 | Exact original task reference, no flattening, pending/faulted/canceled input and unchanged successful export. |
| AsyncAdapters_PreserveAbsentInputAndPreCancellationEffectsAsync | 4 | Strict no-input availability witness and zero provider/converter effects. |
| AsyncAdapters_RejectNullOuterAndConverterTasksAsync | 3 | Exact provider/converter null-task diagnostics, faulted root and stage call counts. |
| AsyncAdapters_ValidateConstructionAndContextBeforeDependenciesAsync | 1 | Both variants' constructor/context guards precede cancellation and dependency effects. |
| PropertyAssignment_PreservesRuntimeTypeOwnershipAfterHeldResolutionAsync | 6 | Derived message skips assignment but retains provider ownership and original outcome. |
| Total | 100 | Case count is not a correctness or whole-fork coverage percentage. |

## Review quality and independence limits

Research and planned acceptance mappings were written before productive correction in .testagent/iteration138/research.md and plan.md. The main agent applied assertion-quality and source-to-assertion pseudo-mutation review with the complete .NET analysis reference; dispositions are recorded in status.md. The 12 methods contain 103 direct Assert source calls, excluding helper assertions and runtime multiplicity. No assertion-free, trivial-only or tautological method was found. Assertions cover values/diagnostic text, identity, null presence, original exceptions and tokens, runtime type, negative effects and actual task/destination states; unnecessary collection/tolerance assertions were not added for diversity alone.

Bounded pending observations fail unless the root remains incomplete. Cleanup independently releases and observes originals and roots. Its catch filter only absorbs an already-completed operation's exception, so an unsettled watchdog timeout remains a failure. Asynchronous exception assertions are awaited.

An authorized internal Lead counterreview fully read 305 source lines, the initial 701-line candidate fixture and the 325-line existing fixture, totaling 1,331 lines; all seven admission hashes matched before and after reading. It reconciled exact additive requirement bindings. The one static Medium null-inner-result coverage gap was subsequently resolved and the amended method/data through cleanup rebound to final fixture SHA-256 0036393dedbbc13f90292eff745e18f1d19db796db381fe1caff9f2676cf42ce. No remaining concrete finding was reported in that bounded admission. This is internal static advice, not an independent external product-team acceptance or an agent-executed runtime certification.

The narrow static pairing invocation terminated with exit 2 because its parser dependency was unavailable. It produced no JSON classification or suggested paths; no untested-source or coverage conclusion is inferred, and no SDK/package installation or restore change was made to obtain one.

## Compiled mutation validation

Eleven hand-authored, single-cause mutations compiled successfully with zero warnings/errors and executed the unchanged 100-case focused multiset. Every attempt was killed by behavioral assertions and exited 2 because of test failures, never because of compilation failure, missing discovery, skip or warning. Across 1,100 case executions, 62 cases failed at the intended contracts; individual attempt failure counts were 6, 3, 3, 8, 11, 6, 4, 4, 2, 13 and 2.

The mutations reinstated each of the three provider-abandonment waits; reinstated direct and converting outer-provider abandonment; reinstated accepted-converter abandonment; removed local cancellation from each caller-owned task-value wait; removed derived-runtime assignment ownership; forwarded the wrong inherited context token; and skipped conversion of a non-null task whose result is null. Failing methods aligned with each cause. Every mutation restored all seven frozen source/test/ledger files in a finally block, and a separate post-loop check matched every SHA-256 exactly. No compiled mutation remains in the worktree.

## Terminal validation and checkpoint

Post-restoration non-incremental Release builds completed cleanly for ViciOne.ServiceBus.slnx and ViciOne.ServiceBus.Tests.Unit.slnx: zero warnings/errors in 1 minute 25.10 seconds and 3 minutes 57.93 seconds respectively. Each build has its own retained binlog. The unfiltered native Core execution then passed 4,645/4,645 cases in 24.969 seconds, with no failed, pending, skipped or other results. Exact multiset reconciliation retained every one of the 4,545 parent case names/multiplicities and added exactly the final 100-case focused multiset. The final multiset SHA-256 is f0324a1ed3be38f84eaaf367eb5b7b30fd4db3b8f583f81080704dd153af479a; CTRF SHA-256 is e538cd157a4e641674688c8357a8de393b0d0a9b6b602214022110162902e3b7.

The unchanged productive coverage configuration SHA-256 is c049632b61e7fcf5be95962df1629c620a9f66e77366dae0e7ddf7c91f0d4a13. Its selected nine-assembly package sums equal the Cobertura root exactly:

| Product assembly | Lines covered / valid | Branches covered / valid |
| --- | ---: | ---: |
| ViciOne.ServiceBus.Abstractions | 4,938 / 8,310 | 1,609 / 2,996 |
| ViciOne.ServiceBus.Sagas | 4,828 / 7,512 | 1,488 / 2,571 |
| ViciOne.ServiceBus.JobService | 4,353 / 4,552 | 2,067 / 2,303 |
| ViciOne.ServiceBus.Courier | 2,526 / 2,835 | 682 / 904 |
| ViciOne.ServiceBus | 28,549 / 35,518 | 10,122 / 13,629 |
| ViciOne.ServiceBus.Futures | 1,354 / 1,495 | 424 / 496 |
| ViciOne.ServiceBus.Testing | 3,176 / 3,365 | 882 / 1,131 |
| ViciOne.ServiceBus.Initializers | 100 / 100 | 4 / 4 |
| ViciOne.ServiceBus.Mediator | 836 / 918 | 247 / 314 |
| Selected total | 50,660 / 64,605 (78.41498%) | 17,525 / 24,348 (71.97716%) |

This is a deliberately stated selected-Core measurement, not whole-fork coverage or a CRAP score. The Cobertura SHA-256 is f43be58c0331a9b102f55f1e697034822df4e1ec265484f569326c104d0e6c6e. Product/unit binlog SHA-256 values are 5ac62e0ae228d272225abd465f9992dcd1019d92005eeb796b4ad8e659974ff7 and 255772276ab20ee819ccb4c9dd1ee2d47dbd87b9bcdb94032dda8595b999e12b.

The main agent's cumulative connected-source full-read admission now contains 61 files / 7,077 current lines with canonical metadata SHA-256 d1378d00ac5556d5800c3fd23ada20a950a5658bf8e021f97a0a52444f423632. The cumulative owning-test/support admission contains 628 files / 153,444 current lines with canonical metadata SHA-256 d9aa130da53b069af2a04ddaf378221621ea423e0d963a36edd0c37333cdd49c. These cumulative admissions record personally completed reads across iterations; they do not certify that every source file in the whole fork has already been read or corrected.

The local raw directory is /private/tmp/vsb-iteration138-provider-lifetime.kaBFjx. Build logs SHA-256 values are 2a9b42e77c0890cb3ce252dc384a646bc5d5727a71249556801757abb3606088, 3ca0e477843ed7ad9b8f47b465c38f611a5dc41bf664b7727028a76fd00bcffa and 6547e90dcb53f49d956e9df2dda3657556e1beb090bdedae1f7c98b846791386 for product, unit-profile and native Core respectively. Raw diagnostics are local evidence and are not staged as product source.

The normal checkpoint is designed to include exactly the seven source/test/ledger paths, three iteration-specific test-agent records and this evidence report. Tag servicebus-a-plus-iteration-138-initializer-provider-lifetime-contract-remediation-2026-09-16 must point to the resulting commit. The normal atomic private push and independent branch/tag-object/peeled-tag verification occur after this report is frozen; the report does not pre-claim that external transaction.

## Original-goal continuation

Whole-fork complete personal source/comment reading, global API/new-parameter behavior coverage, global bidirectional async naming and type/file/directory consistency, legacy/dummy/directive absence, full-fork line/branch coverage and CRAP, and real durable-provider/external acceptance remain original-goal gates. Existing RabbitMQ and InMemory acceptance scopes must not be represented as all-provider acceptance. This iteration does not execute or certify those real-provider gates.

The next connected scalar packet is already being personally read and internally source-counterreviewed: PropertyConverterPropertyProvider, VariablePropertyProvider, ObjectPropertyProvider, FromNullablePropertyProvider and ToNullablePropertyProvider. Their accepted producer/converter/variable waits, nullable/default/runtime conversion behavior and comments require their own corrections and causal tests; caller-owned task inputs must not be indiscriminately converted to cooperative waits. Nested-message, fallback and collection/key converter chains remain part of the same original-goal queue.
