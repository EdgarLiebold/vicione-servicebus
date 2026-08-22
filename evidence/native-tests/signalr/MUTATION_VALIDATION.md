# SignalR mutation validation

Baseline: technical commit `121c7531` (`Reconstruct SignalR tests on native MTP`).

Every mutation was applied in a detached disposable Git worktree. The baseline branch and product
tree were not changed. Each mutant was rebuilt in `Release` and the complete 32-case SignalR project
was then run through xUnit 4 on Microsoft Testing Platform 2:

```text
dotnet build tests2/ViciOne.ServiceBus.SignalR.Tests/ViciOne.ServiceBus.SignalR.Tests.csproj \
  --configuration Release --no-restore --no-incremental
dotnet test --project tests2/ViciOne.ServiceBus.SignalR.Tests/ViciOne.ServiceBus.SignalR.Tests.csproj \
  --configuration Release --no-build --no-restore --no-progress
```

| Mutated behavior | Expected detector | Result |
|---|---|---|
| Remove the MessagePack entry from the embedded requirement projection | `SignalRRequirements_MatchCompiledRequirementMetadata` | Red: 31 passed, 1 failed; the unprojected compiled test was named exactly |
| Change group exclusion from ordinal to case-insensitive comparison | `SendGroupExcept_WithDifferentIdCasing_DoesNotExcludeTheConnection` | Red: 31 passed, 1 failed |
| Remove group cleanup from `OnDisconnectedAsync` | `Disconnect_RemovesEveryGroupMembership` | Red: 31 passed, 1 failed; expected one member, observed two |
| Omit MessagePack serialization from the backplane payload | `SendAll_PreservesMessagePackInvocationsAcrossTheBackplane` | Red: 31 passed, 1 failed |
| Override only `WriteAsync(HubMessage)`, as the inherited test double did, but not `WriteAsync(SerializedHubMessage)` | both write-failure tests | Red: 30 passed, 2 failed; no warning was observed because no serialized write failure was injected |

The last case was also the causal reproduction of a defect in the inherited tests. Backplane
consumers write `SerializedHubMessage`; overriding only the other overload made the inherited failure
scenario look active while its intended exception path was never exercised. The native test double
overrides both overloads and both failure tests require the exact warning and exception type.
