# V4 telemetry and fault-envelope mutation validation

Each mutation changed one production mechanism in the canonical candidate, was compiled by the
focused `dotnet test` invocation, and made the named owner fail for the intended reason. The mutation
was reverted before the next mutation. The final analyzer build, focused controls and complete
2,996-case run were executed only after all mutation bytes had been removed.

| ID | One-cause production mutation | Causal owner result |
|---|---|---|
| M01 | raise the `FaultEvent<T>` exception cap from 16 to 17 | aggregate and explicit-collection boundary assertions failed |
| M02 | raise the `ReceiveFaultEvent` exception cap from 16 to 17 | receive-fault boundary assertion failed |
| M03 | raise the inner-exception cap from 16 to 17 | exact projected-chain depth failed |
| M04 | raise the data-entry cap from 32 to 33 | exact data cardinality failed |
| M05 | raise the diagnostic key cap from 256 to 257 | exact key-length assertion failed |
| M06 | raise the diagnostic text cap from 2,048 to 2,049 | exact bounded-text assertions failed |
| M07 | render complex data through application `ToString()` | hostile-value invocation counter failed |
| M08 | remove ambient-ID fallback propagation after child start failure | unsampled and throwing-sample propagation owners failed |
| M09 | remove ambient trace-state fallback propagation | unsampled and throwing-sample trace-state owners failed |
| M10 | rethrow an activity-creation/listener failure | throwing-sample delivery owner failed |
| M11 | rethrow an activity-start callback failure | throwing-`ActivityStarted` delivery owner failed |
| M12 | rethrow an activity-stop callback failure | throwing-`ActivityStopped` delivery owner failed |
| M13 | rethrow the secondary observation-logger failure | sample-plus-logger delivery owner failed |
| M14 | propagate reserved correlation baggage | reserved-baggage exclusion assertion failed |
| M15 | rethrow a hostile exception `Data` getter | hostile-getter isolation owner failed |

Compiler failures and infrastructure failures were not accepted as mutation evidence. All 15
mutants compiled and reached the intended focused test owner; every run returned the Microsoft
Testing Platform failure exit code because of a causal assertion or the deliberately surfaced
exception.
