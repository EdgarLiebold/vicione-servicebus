# C29 mutation validation

Each mutation was applied alone to product code, rebuilt in `Release`, executed through the native
xUnit v4 Microsoft Testing Platform executable and reverted before the next mutation. Source hashes
were checked after restoration. No surviving product state contains a mutation.

| Mutated product invariant | Detecting test and observed result |
|---|---|
| Normalize caller cancellation only when the immediate linked token is thrown | The child-linked variant of `CallerCancellation_PreservesTheExactCallerTokenAndIsNotReportedAsATimeout` failed on exact token identity. |
| Propagate an expired deadline as raw task cancellation | Both `ConfiguredDeadline_CancelsTheActivePipelineStageOnlyWhenContextTimeAdvances` variants failed on exact exception type. |
| Remove the originating cancellation from the timeout exception | Both virtual-deadline variants failed because `InnerException` was null. |
| Return without awaiting `ConsumeCompleted` | `SuccessfulPipeline_WaitsForConsumeCompletionAndDisposesItsDeadline` failed because the send completed early. |
| Leave the deadline source undisposed | The strengthened timer-lifetime case failed with one active timer after completion. |
| Ignore the explicitly configured provider in the common specification | `PipelineTimeout_PublishesOneFaultAndDoesNotContinueTheHandler` timed out without a fault when only configured virtual time advanced. |
| Ignore the explicit provider in the public filter overload | `ExplicitTimeProvider_OverridesTheProviderAttachedToTheContext` failed on the exact active-timer owner. |
| Publish the raw cancellation inside `Fault<T>` | `PipelineTimeout_PublishesOneFaultAndDoesNotContinueTheHandler` failed on timeout fault classification. |
| Publish a fault when the transport cancellation token is requested | `TransportStop_CancelsTheHandlerWithoutPublishingAFault` failed because the fault collection was not empty. |
| Disconnect the handler configuration observer | The handler row of `EverySupportedScope_CreatesOneValidSpecification` failed because no observer was connected. |
| Accept a zero timeout in the common specification | All seven `EverySupportedScope_RejectsANonpositiveTimeout` rows failed because validation was empty. |
| Read the mutable specification duration after the pipeline was built | `BuiltPipeline_IsUnaffectedByLaterConfiguratorMutation` failed because the fault reported the later one-hour value instead of the compiled two-minute snapshot. |

All twelve one-cause mutations failed for their intended reason. The final restored timeout cohort
passes 30/30.
