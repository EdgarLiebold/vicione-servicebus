# Testing Harnesses — Mutation Validation

Date: 2026-08-24

Every one-cause product mutant was compiled before its owning native test was executed. The product
source was restored after each run and before the unfiltered validation. These are behavioral
mutations, not changes to the test framework or its result accounting.

| One-cause mutation | Expected owner | Observed result |
|---|---|---|
| Keep a disposed condition registered | disposal/removal behavior | Exactly the owning condition test failed. |
| Swallow the exception raised by a test handler | handler failure observation | Exactly the owning failure-propagation test failed. |
| Permit a container harness without the required hosted service | fail-closed container startup | Exactly the missing-hosted-service test failed. |
| Remove telemetry wait completion after the callback | callback lifecycle | The owning callback case failed at its bounded test timeout. |
| Stop hosted services in registration order | reverse shutdown ordering | Exactly the lifecycle-order test failed. |
| Record an `InsertOnInitial` saga through the old missing-policy path | truthful saga creation observation | Exactly the owning creation-observation test failed. |
| Call the underlying repository directly from `SendQuery` | query match observation | The focused class produced 5 total: 4 passed and exactly the new query-correlation case failed because its matched-saga observation was empty. |
| Restore a public no-op setter on `PublishedMessageFilter.Includes` | immutable filter configuration | The focused filter class produced 6 total: exactly the read-only configuration case failed. |
| Ignore the sent-message exclusion set | include/exclude composition | The focused filter class produced 6 total: exactly the received/sent parity case failed. |
| Drop the exception from the untyped consume-observer recording | untyped consume-fault identity | The focused failure case failed because the recorded exception was `null`. |
| Complete the typed fault task successfully instead of faulting it | typed consume-fault identity | The focused failure case failed because the exact exception was no longer thrown. |
| Read process time instead of the injected provider for a sent recording | sent-message observation time | The focused sent-recording case failed on the exact elapsed value. |
| Read process time instead of the injected provider for a published recording | published-message observation time | The focused published-recording case failed on the exact elapsed value. |
| Read process time instead of the injected provider for a received recording | received-message observation time | The focused bus-observer case failed on the exact recorded start time. |
| Remove the received-recording context guard | fail-closed public construction | The focused dependency case observed `NullReferenceException` instead of the exact `ArgumentNullException`. |
| Remove duplicate-ID suppression from the shared message list | first-wins observation identity | Four focused real-list scenarios failed on duplicate dictionary insertion, including the direct duplicate owner. |
| Drop the exception when the send observer records a fault | exact send-fault identity | Exactly the observer success/fault case failed because the recorded exception was `null`. |
| Replace the sent-recording provider with `DateTime.UtcNow` | configured observation time | Exactly the observer success/fault case failed on its literal elapsed-time contract. |
| Ignore the asynchronous-list filter for existing entries | filter execution and failure visibility | Exactly the filter-failure case failed because its required exception was absent. |
| Remove the receive-endpoint observer constructor guard | fail-fast mandatory dependency | Exactly the required-dependency case failed because construction accepted `null`. |
| Remove the endpoint-local publish-observer connection from `Ready` | dynamic-endpoint publication observation | Exactly the dynamic-endpoint case failed immediately because its completed publication was absent from the endpoint-bound observer. |
| Remove the required publish-handler filter guard | fail-fast mandatory dependency | Exactly the dependency case failed because a missing filter was accepted and an endpoint was created. |
| Ignore the publish-handler predicate | exact filtered selection | Exactly the filtered-publication case failed because it returned the deliberately rejected message. |
| Await endpoint readiness without the harness time budget | bounded readiness on the configured clock | Exactly the readiness case failed immediately after virtual time advanced because the connection remained incomplete. |
| Reverse task-completion-source projection order | DI registration identity and order | Exactly the task-registration case failed on reference identity. |
| Skip global endpoint callbacks in the general transport connector | uniform dynamic endpoint configuration | The pre-correction dynamic-endpoint case failed with an empty callback observation; the restored path passes both callback forms exactly once for both overloads. |

No mutant survived. The final `TestSagaRepositoryDecorator` hash is bound in
`EXECUTION_VALIDATION.md`.

Verdict: **PASS**.
