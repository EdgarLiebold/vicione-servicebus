# T87 fault recovery request journey

This packet tests a request started inside the catch path of a typed saga
event. The recovery request uses an exception-aware address provider and an
asynchronous message factory. Existing tests covered normal request/response
correlation and underlying faulted request activities; this exact combined
journey had no integrated behavioral test.

`FaultRecoveryRequest_RoutesTheOriginalFailureOrSuppressesAFailedFactoryAsync`
runs success and factory-failure outcomes through a real in-memory saga and
service endpoint. Before releasing the factory, it proves no request was
sent. The provider and factory both receive the original exception instance.
The chosen endpoint differs from the configured fallback. On success, the
test checks exact request body, destination, response address and RequestId,
then verifies that a response with a third, different payload ID reaches the
original saga and updates it exactly once. Saga, request-payload and
response-payload IDs are distinct, and neither payload ID creates a decoy
saga. On factory failure, the original factory exception remains the root
cause, no request is sent and the saga stays in its initial state with no
repair or result.

Read-only Red Team review found two P2 oracle gaps in the first version:
identical saga and request-payload IDs masked a wrong RequestId source, and
the factory-failure branch did not check saga state. Both were corrected.
Final re-review was PASS with no remaining concrete P1/P2 issue.

The exact test commit is `45d9c2f16`. The focused theory passes 2/2, and the
complete Core xUnit v3/MTP project passes 6,948/6,948 with no failures or
skips on that commit, including the requirement projection gate. No product
source changed. T85 remains the latest complete 33-profile product-wide
Line/Branch/CRAP measurement; global Line and Branch A+ remain open. T87 is
the second focused packet after that checkpoint.
