# V4 bounded-concurrency and task-lifecycle validation

## Bound inputs

- Integration baseline commit: `1ae37a770d96c6c85e6b274494f4558d74ae6ad8`
- Integration baseline tree: `59ad9b296216116b96a63b11f0adc201a3742112`
- Protected review aggregate SHA-256:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`
- V4 bundle SHA-256:
  `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`
- Semantic donor commit:
  `9a15dc1c1e9cfff7b1393561a6783b579d4eff85`
- `review/**` remained unchanged and untracked throughout the integration.

## Integrated behavior

The overlapping channel executors are replaced by one bounded `TaskExecutor` and one lazily
materialized `PartitionedTaskExecutor`. Default and explicit queues have hard admission bounds,
accepted work is drained during idempotent disposal, and admission is rejected as soon as disposal
starts. Awaitable operations propagate their exact result, exception, or cancellation token;
fire-and-forget operations own and isolate both the work failure and a secondary logger failure.

The former `TaskUtil` surface is split by responsibility into `TaskBlocking`, `TaskResults`,
`TaskCompletionSources`, `TaskCompletionSourceExtensions`, and `CancellationTokenExtensions`.
Task- and ValueTask-based executor entry points have distinct names so async lambdas cannot bind to
an unintended overload. All production and benchmark consumers were migrated atomically, and the
obsolete executor and utility types were removed.

The donor was not copied mechanically. The integration additionally:

- retains the unrelated `RequestRateAlgorithm.Run` contract in SQL and Amazon transports;
- makes partitioned disposal atomic with partition creation and shared across concurrent callers;
- makes the benchmark own and dispose both bounded executors;
- preserves command order explicitly in the concurrent batch-future test oracle;
- serializes every test that installs a process-wide `ActivityListener` through the existing global
  collection, eliminating cross-test sampling and fake-time interference.

## Executing evidence

- Focused `TaskExecutorTests`: 23/23 passed.
- Focused `PartitionedTaskExecutorTests`: 7/7 passed.
- Focused `TaskBlockingTests`: 8/8 passed.
- Focused `CancellationTokenExtensionsTests`: 6/6 passed.
- Final Release build of `ViciOne.ServiceBus.Tests.Unit.slnx`: 0 warnings, 0 errors.
- Final complete UnitArchitecture execution: 3,012/3,012 passed, 0 failed, 0 skipped.
- `CoreRequirements.json` parses successfully.
- `git diff --check` passes.
- Scoped whitespace verification passes for all 80 changed or added C# paths. The repository-wide
  formatter still reports inherited findings in unchanged files and is not used as a delta oracle.
- No active source, test, or benchmark reference to `TaskUtil`, `ChannelExecutor`,
  `IChannelExecutorPool`, or `PartitionChannelExecutorPool` remains.

The known macOS workspace sandbox denies .NET/MSBuild/MTP IPC. All authoritative build and test runs
therefore used the established external execution profile with isolated `DOTNET_CLI_HOME`, explicit
`DOTNET_ROOT`, disabled multilevel lookup, disabled node reuse, and the existing package cache. This
is an execution-environment boundary, not a product workaround.

## One-cause mutation evidence

Each mutation was applied independently, its named owner was run, and the source was restored before
the next mutation. The restored sources were then rebuilt and exercised by the complete suite.

| ID | Single changed cause | Causal native owner and result |
| --- | --- | --- |
| M01 | Remove the default admission bound | `DefaultExecutor_HasAHardBoundedAdmissionCapacity` failed |
| M02 | Force one executor worker | `ConcurrencyLimit_BoundsSimultaneouslyActiveWork` failed |
| M03 | Invert queued fire-and-forget cancellation | `CancellationAfterQueuedAdmission_SkipsFireAndForgetWork` failed |
| M04 | Invert awaitable queued cancellation | `CancellationAfterEnqueue_CancelsTheResultWithoutInvokingTheDelegate` failed |
| M05 | Propagate the secondary logger failure | `QueuedFaultAndSecondaryLoggerFault_DoNotTerminateTheWorker` failed with the exact logger exception |
| M06 | Do not close admission on the first dispose | `Enqueue_ReturnsAfterEnqueueAndDisposeWaitsForTheWork` failed by its bounded cancellation token |
| M07 | Allow partition creation after disposal starts | `ConcurrentDisposal_DrainsAcceptedWorkAndRejectsEveryPartition` failed |
| M08 | Route every key to partition zero | `DifferentPartitions_CanExecuteConcurrently` failed by its bounded cancellation token |
| M09 | Force per-partition concurrency to one | `ConcurrentDeliveryLimit_IsAppliedWithinEachPartition` failed by its bounded cancellation token |
| M10 | Remove the per-partition capacity bound | `Capacity_IsBoundedPerPartitionWithoutBlockingIndependentPartitions` failed |
| M11 | Drop cancellation after a blocking wait begins | `Wait_StopsAPendingWaitWhenItsTokenIsCanceledAfterEntry` failed by its bounded cancellation token |
| M12 | Replace the exact fault object | `Faulted_PreservesTheExactException` failed |
| M13 | Remove asynchronous TCS continuations | `Create_CombinesRequestedOptionsWithAsynchronousContinuations` failed |
| M14 | Disable linked cancellation propagation | `RegisterIfCanBeCanceled_CancelsTheTargetSource` failed |
| M15 | Complete `ExecuteAsync` at admission | `ExecuteTask_CompletesOnlyAfterTheAwaitedWorkCompleted` failed |
| M16 | Increase effective partition concurrency | `SamePartition_ExecutesInSubmissionOrderWithoutOverlap` failed with observed overlap |

M06 also established that concurrent-dispose coverage alone was not a sufficient oracle: another
concurrent caller could still close the writer. The exact single-dispose drain owner was therefore
retained as an independent requirement and killed the mutation. No mutation marker remains in the
restored tree.

## Remaining lifecycle work

This checkpoint closes bounded execution and task primitives. Explicit receive startup, retry,
terminal-fault, cancellation, and stop ownership remains active in the same authorized reviewer
integration assignment and will be integrated against V4 receive-terminality evidence before that
assignment is declared complete.
