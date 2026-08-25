# C30 mutation validation

Each product mutation was applied alone, rebuilt in `Release`, executed through the native xUnit v4
Microsoft Testing Platform entry point and removed before the next mutation. The final product hashes
and a clean focused run prove that no mutation remains.

| Mutated invariant | Detecting result |
|---|---|
| Ignore the injected `TimeProvider` and use process time | The relative-delay case could not complete under fake time and the bounded MTP run failed. |
| Do not add the requested duration to the logical offset | The exact manual-advance case could not complete and the bounded MTP run failed. |
| Compare equal deadlines without the sequence tie-breaker | The 32-equal-deadline case could not complete; the bounded run was terminated after its expected timeout failure. |
| Remove timer rearming after cancellation | The cancellation case failed immediately: the next due time remained one minute instead of two. |
| Cancel without the caller token | The cancellation case failed on exact token identity. |
| Do not dispose the one timer | The disposal case observed one active timer instead of zero. |
| Complete rather than cancel pending delays during disposal | The disposal case failed because no `OperationCanceledException` was produced. |
| Rearm beyond .NET's supported maximum timer interval | The long-deadline case failed with the exact `ArgumentOutOfRangeException` from the timer implementation. |
| Accept a zero-duration advance | The zero-boundary theory row failed because no validation exception was produced. |
| Accept a negative relative delay | The negative-delay case failed because no validation exception was produced. |
| Treat an absolute deadline equal to now as a scheduled timer | The immediate-deadline case observed two timer changes instead of zero. |
| Reintroduce `Task.Run` before delayed-delivery registration | The compiled state-machine test failed because the direct `DeliverWithDelay` call disappeared; the behavioral test alone had exposed the race as nondeterministic, which is why the structural assurance was added. |
| Allocate an additional timer per provider | The timer-lifetime case observed two timers instead of one. |
| Change one requirement-projection variant | The projection gate reported both the missing compiled projection and the unmatched projected row. |

All fourteen attacks fail for their intended reason. No result relies on a skipped test, an elapsed
wall-clock tolerance or the retired Python policy system.
