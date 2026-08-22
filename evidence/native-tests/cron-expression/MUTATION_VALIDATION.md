# Cron-expression mutation validation

Review subject: technical commit `3c628228` (`Reconstruct native cron scheduling tests`).

Every mutation was applied to a disposable detached worktree. The accepted worktree was not edited by
the mutations. Restore and build used the committed lock files, `Release`, and no incremental build.
All test commands were unfiltered native xUnit 4 / Microsoft Testing Platform 2 runs.

## Product regression

Mutation: remove the `expr.IsEmpty` guard from
`src/ViciOne.ServiceBus/JobService/JobService/Scheduling/CronExpression.cs`.

Commands executed:

```text
dotnet restore tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --locked-mode --disable-build-servers
dotnet build tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj -c Release --no-restore --no-incremental --disable-build-servers
dotnet test --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj -c Release --no-build --no-restore --results-directory artifacts/test-results/mutation-product-whitespace
```

Result: exit code 2; 153 total, 151 passed, 2 failed, 0 skipped. Both
`ExtraWhitespace_DoesNotChangeTheSchedule` cases failed for their own reason: repeated spaces shifted
the fields to a schedule with no next occurrence, and repeated tabs placed `?` in an illegal field.

## Requirement omission

Mutation: remove the `REQ-VSB-CRON-PARSING | extra-whitespace` row from the embedded requirement
projection while leaving the compiled coverage attribute unchanged.

Commands executed after restoring the first mutation's source file to the review commit:

```text
dotnet build tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj -c Release --no-restore --no-incremental --disable-build-servers
dotnet test --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj -c Release --no-build --no-restore --results-directory artifacts/test-results/mutation-requirement-row
```

Result: exit code 2; 153 total, 152 passed, 1 failed, 0 skipped. The ordinary xUnit projection test
reported the exact compiled method that was absent from the projection.

## Profile-closure omission

Mutation: remove `tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj` from
`ViciOne.ServiceBus.Tests.Unit.slnx` while leaving the project and expected closure intact.

Commands executed after restoring the requirement projection to the review commit:

```text
dotnet restore tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj --locked-mode --disable-build-servers
dotnet build tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj -c Release --no-restore --no-incremental --disable-build-servers
dotnet test --project tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj -c Release --no-build --no-restore --results-directory artifacts/test-results/mutation-unit-solution-closure
```

Result: exit code 2; 80 total, 77 passed, 3 failed, 0 skipped. The tests independently rejected the
unowned executable project, the incomplete exact Unit profile closure, and the missing evaluated test
project properties.

## Verdict

PASS. Each mutation failed closed through the native test architecture. No mutation, raw result, or
generated build artifact is part of the accepted source tree.
