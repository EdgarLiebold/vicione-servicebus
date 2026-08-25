# Observer and message-flow mutation validation

Each probe changed one product behavior, rebuilt the affected Release project without restore, ran
only the named native xUnit/MTP test, and was required to fail. Each probe was reverted with an
explicit inverse patch. SHA-256 comparison of every touched product file closed the restoration.

| Probe | Single mutation | Expected detecting test | Result |
|---|---|---|---|
| M1 | expose the internal `Connectable<T>` snapshot | `Connections_HaveIndependentIdempotentHandlesAndDefensiveSnapshots` | FAIL: returned array was the same instance |
| M2 | let synchronous fan-out failures escape before the remaining callbacks start | `ForEachAsync_InvokesEveryConnectionAndReportsEveryFailureShape` | FAIL: synchronous exception escaped at dispatch |
| M3 | omit transport `PostSend` | `BusAndEndpointObservers_SeeTheSameSuccessfulSendAndDisconnectIndependently` | FAIL: only `Pre` remained |
| M4 | map publish `PostSend` to `PrePublish` | `PublishObserver_SeesPublishedEventsAndRequestsButNeverReportsThemAsSends` | FAIL: second stage was `Pre`, not `Post` |
| M5 | replace the exact receive-fault exception | `FailureAfterSuccessfulConsumption_ReportsReceiveFaultThenCompletesTheHandledDelivery` | FAIL: no event carried the original exception |
| M6 | replace the exact `OnError` exception | `OnNextFailure_IsReportedToOnErrorWithoutSuppressingIndependentConsumers` | FAIL: exception identity differed |
| M7 | omit mediator `SendFault` | `HandlerFailure_ReportsTheExactSendFaultWithoutPostSend` | FAIL: only `Pre` remained |
| M8 | omit successful publish records from the harness timeline | `Timeline_RendersTheProducedAndConsumedMessageFlowWithItsAddress` | FAIL: expected publish count was one, actual count zero |

Final restoration hashes:

```text
c2278d530079bd0adfbd4fb7ab356cbd4c35c0dca1b9529898a1a7e5feb2bd9a  src/ViciOne.ServiceBus.Abstractions/Util/Connectable.cs
be44327e76c3000410e24090ece2a24f652628a72fe729ee4182fe3ab3ad6cf9  src/ViciOne.ServiceBus/Transports/SendTransport.cs
9f3a2967a06c8a33b26669625f55edb82529243c05a6918ea4fa6f4912a3312d  src/ViciOne.ServiceBus.Abstractions/Observers/Observables/PublishObservable.cs
00fc2442f64816952af3d156bd9fdef020cf06bc77607a5a1d583c99a9f1dc2c  src/ViciOne.ServiceBus.Abstractions/Observers/Observables/ReceiveObservable.cs
a290f77d384f75cdb71ee6caf472ac44b998b52b7e94771187f3838d47cf9f71  src/ViciOne.ServiceBus/Middleware/ObserverMessageFilter.cs
b8deb3b1c2c484f96f2e36f9405d7472ec60dd71b61bcb1c74f9f4e553413c03  src/ViciOne.ServiceBus/Mediator/Contexts/MediatorSendEndpoint.cs
0b78989651c1e051cd300166fcc4146d0ff0c6285a62cacf8bff00818ebc136d  src/ViciOne.ServiceBus/Testing/Implementations/BusTestPublishObserver.cs
```
