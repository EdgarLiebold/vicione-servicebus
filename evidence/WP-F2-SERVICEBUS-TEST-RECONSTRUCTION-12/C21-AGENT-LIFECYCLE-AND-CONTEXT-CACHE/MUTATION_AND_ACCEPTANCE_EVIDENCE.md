# C21 agent lifecycle and context cache — mutation and acceptance evidence

## Stationary subject

- parent commit: `27f782b2d979f25c6da51ac69a9f0e06426a0f34`
- technical commit: `00e02b67ddfc4f9297afef8c03e03f1f6b962c2f`
- technical tree: `026afa9721296acb610bbcb19d258356240f3d0d`
- profile: `UnitArchitecture`
- inherited source: `tests/ViciOne.ServiceBus.Tests/Middleware/Agents/Agent_Specs.cs`
- inherited source blob SHA-256: `e44fdabe1f4caae44d6465b2581d1db61d9bdea67c79e750f0691ea3890d0120`

The technical commit changes no file below `src`, no project or solution file, no central build or
package file, and no lock file. The accepted product and dependency graph are unchanged.

## Source-owner result

The inherited mixed NUnit fixture is replaced by six ordinary xUnit/MTP facts under two actual
product source owners:

- four `Agent` and `Supervisor` lifecycle facts in the Abstractions `Middleware` subtree;
- two `PipeContextSupervisor` cache facts in the Core `Agents` subtree.

The lifecycle tests explicitly observe readiness, completion, stop tokens, child terminal state,
and supervisor counts. The inherited 50-iteration ready-fault race exposure remains, without local
timeouts, sleeps, random input, or wall-clock assertions. Cleanup runs in `finally` on the faulted
readiness path.

The cache tests use deterministic context IDs and synchronous test-owned disposal accounting. They
prove that both a pipeline exception and explicit invalidation dispose context `1`, after which the
third send uses context `2`. This preserves the inherited executable behavior and corrects a
contradictory ledger sentence that said a pipe fault did not invalidate the cache despite expecting
the newly created context value `2`.

No test owns discovery, result, coverage, timing, retry, or verdict behavior. Every fact owns one
passive requirement identity, and all six inherited ledger obligations have a terminal disposition.

## Bound file hashes

| File | SHA-256 |
| --- | --- |
| `tests2/ViciOne.ServiceBus.Abstractions.Tests/Middleware/SupervisorLifecycleTests.cs` | `b1b1b2cbfb875b38d4f854006e6d8d08fe887966b0881a1c5234fb505cc15f18` |
| `tests2/ViciOne.ServiceBus.Abstractions.Tests/Requirements/AbstractionsRequirements.json` | `a308c1aa3803a322dd0f3d446a3f2ea2a3c45c3c091147ab3fb3521be30e2f8c` |
| `tests2/ViciOne.ServiceBus.Tests/Agents/PipeContextSupervisorTests.cs` | `67fe979e4ff2ebcff19386520a6e2efbd96121c9209ebf62833c007d97f6aadb` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `45aae79d954a0664b252be577947d637ecc57c5a570bfddab55b1edc0eb32d9a` |
| `evidence/native-tests/agent-lifecycle-and-context-cache/INHERITED_BEHAVIOR_DISPOSITION.json` | `e78a41c2c9f899e9d289eaaae71fa7f61af83ffda7f9c10af07ff7d6525d16ee` |

## False-green attacks

Each mutation changed one production behavior, one scenario action, or one passive requirement
identity, built successfully, failed the responsible ordinary test, and was restored before the
next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Make `SetNotReady` complete successfully instead of propagating its exception | 1 failed, 126 passed |
| Fault the empty-supervisor completion task | 1 failed, 126 passed |
| Complete the one-child supervisor branch without stopping its child | 3 failed, 124 passed |
| Ignore agents added after the supervisor is already ready | 1 failed, 126 passed |
| Retain the cached context after a pipeline fault | 1 failed, 280 passed |
| Omit the explicit invalidation action from the second send | 1 failed, 280 passed |
| Change one passive requirement variant without changing its embedded projection | 1 failed, 280 passed |

No surviving or unselected mutation is represented as evidence.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded `dotnet format whitespace --verify-no-changes` | exit 0 |
| UnitArchitecture Release build of the final tree | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run of the final tree | 793 total; 793 passed; 0 failed; 0 skipped |
| LocalIntegration Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered LocalIntegration run | 3 total; 3 passed; 0 failed; 0 skipped |
| JSON parsing, diff whitespace, and forbidden timing/random/skip scan | pass |
| Requirement projection | 6 new source attributes and 6 unique projection rows |
| Inherited closure | 6 of 6 obligations terminally mapped to 6 executing facts |
| Product/package graph | no product, project, package, solution, central-build, or lock-file delta |

The final UnitArchitecture command was:

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore --results-directory artifacts/test-results/unit --minimum-expected-tests 793
```

No restore was needed or claimed for C21: the technical commit does not change any dependency input
or lock file, and both final Release builds and profiles use the already accepted locked assets
with `--no-restore`.
