# Iteration 119 token-getter failure packet

Status: the unchanged checkpoint falsely completes both new cases; corrected87/87
and existing76/76 pass. A separate compilable unsafe-filter counterchange is killed
by exactly both new cases and independently byte-restored. Expanded payload/factory
packet subsequently passes91/23/76 and full Core3,805; evidence is recorded separately.
The original source-architecture goal/order stays active; checkpoint
`de97b0608d23b693efad8eed0aef4f1a4c2946e6` and its annotated intermediate tag are
verified on origin before these edits. Previous reviewer freeze and all root
build/test/format executions are terminal. Protected review/results are untouched.

## Research and acceptance map

The author personally reads the complete current RetryFilter/RedeliveryRetryExecution
and policy contracts, and the complete local custom-policy fixture. The internal
review establishes an exact selected-token getter failure swallowed by a CLR exception
filter; it does not execute the counterexample. Preserve the legitimate distinction
between requested source/policy cancellation and a foreign requested-token OCE that
may remain business-retry eligible. Never swallow or reclassify getter infrastructure.

`RetryCancellationClassification_PreservesTokenGetterFailureWithoutAnotherBusinessAttemptAsync`
exercises the entire combination: source None, successful preparation with selected
token None, retry business raises a requested foreign-token OCE, second selected getter
read throws exact E, and a later safe decision could permit another retry. Single
and nested cases assert exact E, two business attempts, zero effects/next-decision
calls/completion, one factory/cleanup, outer [create] and released ownership storage.
The one exact REQ-VSB-RETRY-CONTRACT projection is registered manually.

## Plan

Run all87 ownership cases against unchanged product: the two new cases must fail
causally while old85 stay green. Then move policy/context ownership/cancellation
evaluation out of exception filters and into guarded catch-body infrastructure,
consistently across generic initial/retry and ordinary/activity common redelivery.
Keep filters only where they inspect already captured framework token structs, not
caller-supplied getters. Preserve requested-token identity, nested budget behavior,
cleanup and all prior stage-fault oracles. Repeat87/76/full Core, controlled exact
regression counterchange, frozen internal read-only review and proportional gates.
No source/test/comment generator; no executable edit during running proof processes.

## Executed red and green

The unchanged checkpoint product compiles with zero warnings/errors. Its87-case
ownership run fails exactly both new cases because no exception is thrown; old85
pass, zero skip. Raw red:
`/private/tmp/vsb-iteration119-token-getter-causal-red/token-getter-causal-red.ctrf.json`,
SHA-256 `e6b02b52d804fbdf4d69d9e7bfa1f70ac977770b7adc8107082d0394295368d6`.
The correction compiles with zero warnings/errors and passes87/87 ownership cases
and76/76 existing RetryFilter cases, zero skip. No accepted red case has a fixture
setup error. ShouldPropagate guards caller-supplied payload/token getters in ordinary
catch-body infrastructure; remaining filters read captured framework token structs.
