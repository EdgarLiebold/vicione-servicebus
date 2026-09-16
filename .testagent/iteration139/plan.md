# Iteration 139 — handwritten test and correction plan

1. Add a focused handwritten ownership/resource fixture and additive CoreRequirements bindings before productive source changes. Preserve the existing PropertyProviderContractTests case name and binding while correcting only its affected assertions and cleanup. Freshly compile, then execute the complete focused class plus the existing contract class; record behavioral failures separately from compiler failures.
2. Prove all eight accepted waits with independently controlled originals. For each stage, cover success, ordinary fault and provider cancellation with caller cancellation both absent and present; assert exact context, input, token, original exception/cancellation identity, pending ownership and downstream side effects. Keep AsyncPropertyProvider's caller-owned inner wait locally cancellable.
3. Add dictionary-object regressions that preserve the exact runtime array, custom collection and reference-object instances. Add a convention boundary case in which an unsupported custom struct target returns false with a null initializer instead of failing generic activation.
4. Add a collectible-runtime-type test that keeps ObjectPropertyProvider alive while proving a runtime Type used only as a negative converter key can be collected. Retain the existing sequential converter-reuse assertion. Do not impose an exact converter-construction count under contention.
5. Apply the smallest productive correction: directly await accepted tasks while still forwarding the caller token; use local runtime identity in ObjectPropertyProvider; replace its strong cache with a collectibility-safe weak-key table; restrict the dictionary object fallback to reference types; rename the nested converter interface and fix spacing/comments. Do not change PropertyProviderFactory.Matching or any public signature.
6. Re-run focused tests after a fresh compiler. Re-open every new or changed assertion against the final source, apply the .NET assertion-quality and pseudo-mutation reviews, and close every concrete gap before terminal validation.
7. Hand-author compiled one-cause mutations for each wait class, identity bypass, unsupported-struct guard and weak-key cache. Every mutation must compile, run an unchanged relevant test multiset, fail behaviorally, restore frozen bytes and leave no residue.
8. Run warning-as-error non-incremental Release builds for product and unit solutions, then the unfiltered native Core profile with exact parent-multiset reconciliation. Collect selected coverage without representing it as whole-fork coverage; separately retain the open whole-fork coverage/CRAP and real-provider gates.
9. Update exact source/test admissions, iteration status and the detailed evidence report. Review the complete diff, normally commit, create an annotated Iteration-139 tag, atomically push branch and tag without force, and independently verify remote branch, tag object and peeled tag.

## Requirement-to-test map

| Requirement | Planned method |
| --- | --- |
| PropertyConverterPropertyProvider owns accepted source and converter outcomes after caller cancellation | PropertyConverterProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync |
| VariablePropertyProvider owns accepted provider and variable outcomes after caller cancellation | VariableProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync |
| ObjectPropertyProvider owns accepted source and runtime-converter outcomes after caller cancellation | ObjectProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync |
| Both nullable adapters own accepted provider outcomes after caller cancellation | NullableProviders_OwnAcceptedProviderAndOriginalOutcomeAsync |
| Existing caller-owned task inputs remain locally cancellable while the corrected providers remain pending | ComposedProviders_ObserveCallerCancellationWhileDependenciesRemainPendingAsync |
| IDictionary<string, object> preserves exact array, custom collection and reference instances | DictionaryObjectValues_PreserveAssignableRuntimeInstancesAsync |
| Unsupported custom struct dictionary targets fail cleanly without reflective constraint errors | DictionaryObjectFallback_RejectsUnsupportedStructWithoutActivationFailure |
| Runtime conversion caching does not retain a collectible negative runtime type for provider lifetime | ObjectProvider_DoesNotRetainCollectibleRuntimeTypes |

Each asynchronous original is independently released and observed in a finally block. Bounded pending checks fail unless the tested root remains incomplete. Exact case counts and coverage percentages are evidence summaries, never correctness substitutes.
