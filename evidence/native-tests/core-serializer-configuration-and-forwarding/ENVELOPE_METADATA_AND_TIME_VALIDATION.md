# Envelope Metadata and Contextual Time Validation

Date: 2026-08-24
Decision: `PO-2026-08-24-03`

## Subject

- one internal metadata projection shared by System.Text.Json and MessagePack;
- standard .NET `TimeProvider` carried by `PipeContext`;
- no serializer-owned TTL clamp or grace period;
- at most one UTC snapshot per envelope projection;
- serializer-specific payload encoding remains unchanged.

`Microsoft.Extensions.TimeProvider.Testing` is test-only and does not occur in a product project or
product lock file.

## Final commands and results

All commands ran from the repository root with the pinned SDK.

```text
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore --no-incremental --disable-build-servers --verbosity minimal
PASS — 0 warnings, 0 errors

dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --max-parallel-test-modules 1 --verbosity minimal
PASS — 937 total, 937 passed, 0 failed, 0 skipped

dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-restore --no-incremental --disable-build-servers --verbosity minimal
PASS — 0 warnings, 0 errors

dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-build --no-restore --max-parallel-test-modules 1 --verbosity minimal
PASS — 3 total, 3 passed, 0 failed, 0 skipped

dotnet format whitespace ViciOne.ServiceBus.Tests.Unit.slnx --no-restore --verify-no-changes --include <seven changed C# files> --verbosity minimal
PASS
```

The three directly affected projects also passed unfiltered: Abstractions `134/134`, Core `412/412`
and MessagePack `54/54`, with no skipped tests.

## Effective one-cause mutations

Each mutation was applied alone, rebuilt, observed red for its intended reason, reverted immediately,
and followed by the final clean build and test runs above.

| Mutation | Expected guard | Observed result |
|---|---|---|
| replace the contextual provider with `TimeProvider.System` | deterministic contextual-time tests | 5 target tests failed |
| restore the former one-second clamp for nonpositive TTL | zero/negative TTL contract | 2 target tests failed |
| read the clock a second time for expiration | one-snapshot contract | 1 target test failed with an exact one-second divergence |
| map MessagePack fault address from source address | cross-format metadata parity | 2 MessagePack tests failed |

No mutation remains in the working tree.
