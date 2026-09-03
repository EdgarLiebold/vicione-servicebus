# V4 multibus routing and final-tail mutation validation

Date: 2026-09-03

Each mutation changed exactly one production mechanism in the current candidate. It had to compile and
reach the intended native owner; compiler errors and infrastructure failures were never accepted as
mutation evidence. Every target was restored before the next mutant and before final positive validation.

| ID | One-cause production mutation | Causal killing observation |
|---|---|---|
| M01 | wait for inactivity before taking the observation snapshot | the late-arrival snapshot boundary failed |
| M02 | retain only the newest direct-harness handler observation | the complete ordered history assertion failed |
| M03 | replace a missing previous saga state with the current state | the explicit null previous-state assertion failed |
| M04 | return a mutable observation snapshot | the readonly snapshot contract failed |
| M05 | allow route registration after materialization | the route-table freeze owner failed |
| M06 | select the first inherited route when two candidates exist | the deterministic ambiguity owner failed |
| M07 | reuse one route table across buses | the opposite-route multibus isolation owner failed |
| M08 | accept a zero observation threshold | the exact positive-boundary validation failed |
| M09 | share the message-data policy between buses | the opposing-policy isolation owner failed |
| M10 | allow initializer convention registration after freeze | the immutable-registry boundary failed |
| M11 | omit `TBus` from outbox consumer identity | the two-bus identity owner observed a collision |
| M12 | resolve Quartz time zones without the scheduler owner | the owner-scoped resolver was not reached |
| M13 | keep assembly type lookup asynchronous | the synchronous configuration-boundary owner failed |
| M14 | omit send-endpoint-cache disposal | the exact asynchronous resource-disposal owner failed |
| M15 | register a request client without its typed bus binding | the three-bus request owner resolved the wrong boundary |
| M16 | store request-state ownership process-statically | the isolated owner-state test observed cross-bus state |
| M17 | leave global message topology mutable after bootstrap | the topology freeze owner failed |
| M18 | include Testing projects in the shipping solution | the shipping-composition architecture gate failed |
| M19 | restore an immediately duplicated source path | the C# source-layout architecture gate failed |
| M20 | schedule a recurring command with `CorrelationId` instead of `TokenId` | the exact technical-identity assertion failed |
| M21 | bypass the owner-scoped Quartz time-zone resolver | the delivered trigger used the wrong resolution path |
| M22 | remove route-provider forwarding from `BusInstance<TBus>` | the non-default three-bus request case failed with `ConfigurationException` |

The restored implementation has these representative SHA-256 values:

```text
66f58f59c96795c7504fd8077a42635ca76ac119b69cfa08372ec79d21548bf1  BusInstance.cs
b85c127ed8835e784c31557cf3ba88e0711b82f3c3877d55a87975eb0bead74b  IMessageRouteProvider.cs
9d0227ebca70e2ec8a2e7e3d2cebe9d267a45db3b98a29350422e008854a2c2f  MessageRouteTable.cs
f2aa118ab4caf0207f2f709efbc99c482ff12fe980e948135223cad89fd09131  ApplicationMessageTopology.cs
215860c1ab627d9c2d803f1b485d3ddfa1d07ad281a2f4bfc5aa59b6b5849edb  ActiveTestResult.cs
72dcba4f19aa04477a4cbd8cf951ad921443af54f09126f454a1e826640b0783  AssemblyTypeCache.cs
d2a2659155a6ccfb3af85e77ad32ba5ac906291666ce9e11c948559583815520  ScheduleMessageConsumer.cs
```

The final positive Core run passed 1,613/1,613 and the complete Unit/Architecture aggregate passed
3,290/3,290 with zero skips after all 22 mutants were removed.
