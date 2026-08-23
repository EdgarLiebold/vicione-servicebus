# Endpoint-resource cache mutation validation

This record covers the one-cause mutations used while reconstructing the endpoint-resource cache
cohort. Each mutation was applied to the working tree, exercised against the native Release test
artifact, observed to fail, and immediately reverted. The final acceptance run is performed only
after every source and build mutation has been removed.

| Mutation | Expected protected behavior | Observed result |
|---|---|---|
| Remove the newly created node from `Index<TKey, TValue>` before returning it | A successful creation remains immediately readable | 315 total, 2 failed, exit 2 |
| Compare `TimeProvider.GetTimestamp()` units directly with `TimeSpan.Ticks` | TTL uses elapsed time in the provider's timestamp frequency | 315 total, 1 failed, exit 2 |
| Make `UsageCachePolicy<TValue>` return zero usage | Frequently reused values influence retention | 315 total, 2 failed, exit 2 |
| Double the `ValueTracker<TValue>` new-value bucket | Tracker capacity and eviction effects remain exact | 315 total, 2 failed, exit 2 |
| Omit eviction of a faulted cache value | A failed factory cannot poison later creation | 315 total, 2 failed, exit 2 |
| Delete the TTL-configuration requirement-projection row | Every declared behavior remains projected | 315 total, 1 failed, exit 2 |
| Bypass the pending factory and call the fallback factory directly | Concurrent callers share the winning creation | 315 total, 2 failed, exit 2 |
| Remove the native core project's distinct `ArtifactsProjectName` | Same-named inherited and native projects cannot share intermediate artifacts | 83 total, 2 failed, exit 2 |

The final mutation has two causally related failures: the explicit artifact-identity assertion and
the existing portable-symbol assertion. Removing the identity makes both projects evaluate and
restore through the same intermediate graph, which also corrupts the native project's evaluated
test-entry state. This is one infrastructure defect with two independent observable consequences,
not two mutations.
