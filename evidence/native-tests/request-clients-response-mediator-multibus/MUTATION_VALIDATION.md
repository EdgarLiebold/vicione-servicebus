# Request, response, mediator, and multi-bus mutation validation

Each mutation was applied alone, built in `Release`, executed through the native xUnit 4 / MTP 2
test executable, observed failing for the stated reason, and then removed. No mutant remains in the
working tree.

| Mutation | Owning test | Observed failure |
|---|---|---|
| Use transport TTL instead of the client deadline for the request timer | `DisabledTransportTimeToLive_DoesNotDisableTheClientDeadline` | The disabled TTL was used as a timer value and raised `InvalidOperationException`. |
| Ignore the dependency-injection `TimeProvider` when constructing the mediator | `DependencyInjectionMediator_UsesTheRegisteredTimeProviderForRequestDeadlines` | The registered virtual timer never appeared; the bounded safety wait failed after two seconds. |
| Omit the request `Accept` header | `ResponseMatchingTests` | Seven response-selection cases rejected responses as unsupported. |
| Make `ClientRequestHandle.Dispose` a no-op | `ResponseMatchingTests` | Four cases found losing response tasks still pending instead of canceled. |
| Calculate response/fault TTL from system time instead of the context provider | `RequestOutcomeTimeToLiveTests` | Both exact remaining-TTL assertions failed. |
| Remove the shared request-endpoint outbox bypass | `RequestSendEndpoint_BypassesADeferredEndpointBeforeSending` | `DeferredSendCount` was `1` instead of `0`; failure occurred before any timeout. |
| Let the rejecting scoped filter call the consumer | `ScopedConsumeFilter_ProducesAnExactRequestFaultWithoutInvokingTheConsumer` | No `RequestFaultException` was produced and the consumer returned a response. |
| Register the secondary request client on the default bus | `ScopedConsumer_PreservesCausationWhileRoutingThroughTheSecondaryBus` | The secondary request used the default address and failed with its one-second request timeout. |
| Remove the initialized-values guard from `RequestClient.Create` | `EveryRequestEntryPoint_RejectsItsMissingMessageBeforeSending` | The `CreateValues` theory case did not throw synchronously. |
| Remove the third response-task constructor guard | `MissingResponseTasks_AreRejectedAtThePublicConstructorBoundary` | A `NullReferenceException` replaced the required exact `ArgumentNullException`. |
| Delegate `ConversationId` to `RequestId` in the two-response wrapper | `TwoResponseWrapper_DelegatesContextAndPreservesBothBranchTasks` | The exact conversation identifier differed from the owning response context. |

The first attempted outbox mutation removed `SkipOutbox` only from
`SendRequestSendEndpoint<TRequest>`. The inherited integration scenario survived because it creates
the nested client from the raw bus; this exposed a real test gap. The resulting A+ correction moved
the invariant to the shared `RequestSendEndpoint<TRequest>` base and added the direct failing
mutation above.
