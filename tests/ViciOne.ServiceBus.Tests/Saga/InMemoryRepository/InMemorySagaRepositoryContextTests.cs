using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga.InMemoryRepository;

public sealed class InMemorySagaRepositoryContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "removed-load-only-never-releases-a-saga-lease")]
    public async Task RemovedSaga_LoadOnlyReturnsNullWithoutReleasingAnotherOwnersLeaseAsync(bool owned)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        var instance = new SagaInstance<RepositoryState>(new RepositoryState());
        dictionary.Add(instance);
        if (owned)
            await instance.MarkInUseAsync(TestContext.Current.CancellationToken);
        instance.Remove();
        var context = new InMemorySagaRepositoryContext<RepositoryState>(dictionary, TestContext.Current.CancellationToken);
        RepositoryState? result = null;
        Exception? releaseError = null;

        Exception? loadError = await Record.ExceptionAsync(async () => result = await context.LoadAsync(instance.Instance.CorrelationId, TestContext.Current.CancellationToken));
        if (owned)
            releaseError = Record.Exception(instance.Release);

        Assert.Null(loadError);
        Assert.Null(result);
        Assert.Null(releaseError);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "removed-message-load-never-releases-a-saga-lease")]
    public async Task RemovedSaga_MessageLoadReturnsNullWithoutReleasingAnotherOwnersLeaseAsync(bool dictionaryHeld, bool owned)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context =
            await CreateContextAsync(dictionary, dictionaryHeld, TestContext.Current.CancellationToken);
        var instance = new SagaInstance<RepositoryState>(new RepositoryState());
        dictionary.Add(instance);
        if (owned)
            await instance.MarkInUseAsync(TestContext.Current.CancellationToken);
        instance.Remove();
        SagaConsumeContext<RepositoryState, RepositoryMessage>? result = null;
        Exception? releaseError = null;

        Exception? loadError = await Record.ExceptionAsync(async () => result = await context.LoadAsync(instance.Instance.CorrelationId, TestContext.Current.CancellationToken));
        if (owned)
            releaseError = Record.Exception(instance.Release);
        context.Dispose();

        Assert.Null(loadError);
        Assert.Null(result);
        Assert.Null(releaseError);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "repository-disposal-releases-dictionary-exactly-once")]
    public async Task RepeatedAndConcurrentDispose_ReleaseOnlyTheRepositorysDictionaryLeaseAsync(bool concurrent)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context = await CreateContextAsync(dictionary, true, TestContext.Current.CancellationToken);
        if (concurrent)
            await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(context.Dispose, TestContext.Current.CancellationToken)));
        else
        {
            context.Dispose();
            context.Dispose();
        }

        await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var contenderCancellation = new CancellationTokenSource();
        Task contender = dictionary.MarkInUseAsync(contenderCancellation.Token);
        try
        {
            Assert.False(contender.IsCompleted);
            context.Dispose();
            Assert.False(contender.IsCompleted);
        }
        finally
        {
            contenderCancellation.Cancel();
            if (!contender.IsCompletedSuccessfully)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => contender);
            else
                dictionary.Release();
            dictionary.Release();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "add-insert-load-preserve-state-and-transfer-leases")]
    public async Task AddInsertAndLoad_PreserveStateIdentityAndRefuseAnExistingInsertAsync(bool dictionaryHeld)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context = await CreateContextAsync(dictionary, dictionaryHeld, TestContext.Current.CancellationToken);
        var added = new RepositoryState();
        var inserted = new RepositoryState();
        SagaConsumeContext<RepositoryState, RepositoryMessage> addContext = await context.AddAsync(added, TestContext.Current.CancellationToken);
        Assert.Same(added, addContext.Saga);
        ((IDisposable)addContext).Dispose();
        SagaConsumeContext<RepositoryState, RepositoryMessage> insertContext = Assert.IsAssignableFrom<SagaConsumeContext<RepositoryState, RepositoryMessage>>(
            await context.InsertAsync(inserted, TestContext.Current.CancellationToken));
        Assert.Same(inserted, insertContext.Saga);
        ((IDisposable)insertContext).Dispose();

        Assert.Null(await context.InsertAsync(new RepositoryState { CorrelationId = inserted.CorrelationId }, TestContext.Current.CancellationToken));
        SagaConsumeContext<RepositoryState, RepositoryMessage> loaded = Assert.IsAssignableFrom<SagaConsumeContext<RepositoryState, RepositoryMessage>>(
            await context.LoadAsync(added.CorrelationId, TestContext.Current.CancellationToken));
        Assert.Same(added, loaded.Saga);
        ((IDisposable)loaded).Dispose();
        Assert.Null(await context.LoadAsync(NewId.NextGuid(), TestContext.Current.CancellationToken));
        Assert.Equal(2, dictionary.Count);
        Assert.Same(added, (await new InMemorySagaRepositoryContext<RepositoryState>(dictionary, default).LoadAsync(added.CorrelationId, TestContext.Current.CancellationToken)));
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "existing-insert-retains-initial-dictionary-lease")]
    public async Task ExistingInsert_RetainsTheInitialDictionaryLeaseUntilDisposalAsync(bool removed)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        var instance = new SagaInstance<RepositoryState>(new RepositoryState());
        dictionary.Add(instance);
        if (removed)
            instance.Remove();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context = await CreateContextAsync(dictionary, true, TestContext.Current.CancellationToken);

        Assert.Null(await context.InsertAsync(new RepositoryState { CorrelationId = instance.Instance.CorrelationId }, TestContext.Current.CancellationToken));
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        Task contender = dictionary.MarkInUseAsync(cancellation.Token);
        try
        {
            Assert.False(contender.IsCompleted);
            context.Dispose();
            await contender;
        }
        finally
        {
            cancellation.Cancel();
            await DrainDictionaryAcquisitionAsync(dictionary, contender, cancellation.Token);
        }
    }

    [Theory]
    [InlineData("Add")]
    [InlineData("Insert")]
    [InlineData("Save")]
    [InlineData("Update")]
    [InlineData("Undo")]
    [InlineData("Delete")]
    [InlineData("Discard")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "required-operation-inputs-are-validated")]
    public async Task RequiredOperationInput_IsRejectedWithoutChangingTheRepositoryAsync(string operation)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context = await CreateContextAsync(dictionary, false, TestContext.Current.CancellationToken);
        var instance = new SagaInstance<RepositoryState>(new RepositoryState());
        dictionary.Add(instance);

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() => InvokeAsync(context, operation, null, TestContext.Current.CancellationToken));

        Assert.Equal(operation is "Add" or "Insert" ? "instance" : "context", exception.ParamName);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        Assert.False(instance.IsRemoved);
        Assert.Equal(1, dictionary.Count);
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData("Save", false)]
    [InlineData("Save", true)]
    [InlineData("Update", false)]
    [InlineData("Update", true)]
    [InlineData("Undo", false)]
    [InlineData("Undo", true)]
    [InlineData("Delete", false)]
    [InlineData("Delete", true)]
    [InlineData("Discard", false)]
    [InlineData("Discard", true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "persistence-cancellation-selects-explicit-or-consume-token")]
    public async Task PersistenceCancellation_UsesTheExplicitTokenOrFallsBackToTheConsumeTokenAsync(string operation, bool explicitToken)
    {
        using var consumeCancellation = new CancellationTokenSource();
        using var operationCancellation = new CancellationTokenSource();
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context =
            await CreateContextAsync(dictionary, false, consumeCancellation.Token);
        var instance = new SagaInstance<RepositoryState>(new RepositoryState());
        dictionary.Add(instance);
        var consumeContext = new DefaultSagaConsumeContext<RepositoryState, RepositoryMessage>(CreateConsumeContext(TestContext.Current.CancellationToken), instance.Instance);
        CancellationToken expected = explicitToken ? operationCancellation.Token : consumeCancellation.Token;
        if (explicitToken)
            operationCancellation.Cancel();
        else
            consumeCancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InvokeAsync(context, operation, consumeContext, explicitToken ? operationCancellation.Token : default));

        Assert.Equal(expected, exception.CancellationToken);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        Assert.False(instance.IsRemoved);
    }

    [Theory]
    [InlineData("Save")]
    [InlineData("Update")]
    [InlineData("Undo")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "acknowledgement-preserves-live-state-without-cloning")]
    public async Task Acknowledgement_UsesAnExplicitLiveTokenAndKeepsTheSameMutableStateAsync(string operation)
    {
        using var consumeCancellation = new CancellationTokenSource();
        using var operationCancellation = new CancellationTokenSource();
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context = await CreateContextAsync(dictionary, false, consumeCancellation.Token);
        var state = new RepositoryState { Value = 17 };
        var instance = new SagaInstance<RepositoryState>(state);
        dictionary.Add(instance);
        var consumeContext = new DefaultSagaConsumeContext<RepositoryState, RepositoryMessage>(CreateConsumeContext(TestContext.Current.CancellationToken), state);
        consumeCancellation.Cancel();
        state.Value = 23;

        await InvokeAsync(context, operation, consumeContext, operationCancellation.Token);

        Assert.Same(state, dictionary[state.CorrelationId]!.Instance);
        Assert.Equal(23, state.Value);
        Assert.False(instance.IsRemoved);
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("Discard")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "delete-discard-remove-only-the-retained-state")]
    public async Task DeleteAndDiscard_RemoveTheRetainedStateAndReleaseTheDictionaryAsync(string operation)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context = await CreateContextAsync(dictionary, false, TestContext.Current.CancellationToken);
        var instance = new SagaInstance<RepositoryState>(new RepositoryState());
        dictionary.Add(instance);
        var consumeContext = new DefaultSagaConsumeContext<RepositoryState, RepositoryMessage>(CreateConsumeContext(TestContext.Current.CancellationToken), instance.Instance);

        await InvokeAsync(context, operation, consumeContext, TestContext.Current.CancellationToken);

        Assert.True(instance.IsRemoved);
        Assert.Null(dictionary[instance.Instance.CorrelationId]);
        Assert.Equal(0, dictionary.Count);
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("Discard")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "stale-context-cannot-delete-a-replacement")]
    public async Task StaleContext_CannotDeleteAReplacementWithTheSameCorrelationIdAsync(string operation)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context = await CreateContextAsync(dictionary, false, TestContext.Current.CancellationToken);
        var oldState = new RepositoryState();
        var replacement = new SagaInstance<RepositoryState>(new RepositoryState { CorrelationId = oldState.CorrelationId });
        dictionary.Add(replacement);
        var staleContext = new DefaultSagaConsumeContext<RepositoryState, RepositoryMessage>(CreateConsumeContext(TestContext.Current.CancellationToken), oldState);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(context, operation, staleContext, TestContext.Current.CancellationToken));

        Assert.Contains(oldState.CorrelationId.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Same(replacement, dictionary[oldState.CorrelationId]);
        Assert.False(replacement.IsRemoved);
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "missing-delete-has-exact-diagnostic-and-releases-lease")]
    public async Task MissingDelete_ReportsTheExactCorrelationIdAndReleasesTheDictionaryAsync()
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context = await CreateContextAsync(dictionary, false, TestContext.Current.CancellationToken);
        var state = new RepositoryState();
        var consumeContext = new DefaultSagaConsumeContext<RepositoryState, RepositoryMessage>(CreateConsumeContext(TestContext.Current.CancellationToken), state);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.DeleteAsync(consumeContext, TestContext.Current.CancellationToken));

        Assert.Equal($"Saga {state.CorrelationId} was not found in the in-memory repository.", exception.Message);
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData("Delete", false)]
    [InlineData("Delete", true)]
    [InlineData("Discard", false)]
    [InlineData("Discard", true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "removal-uses-initial-dictionary-lease-without-self-deadlock")]
    public async Task DeleteAndDiscard_UseTheInitialDictionaryLeaseAndReleaseItOnSuccessOrFailureAsync(string operation, bool missing)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        using InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context =
            await CreateContextAsync(dictionary, true, TestContext.Current.CancellationToken);
        var instance = new SagaInstance<RepositoryState>(new RepositoryState());
        if (!missing)
            dictionary.Add(instance);
        var consumeContext = new DefaultSagaConsumeContext<RepositoryState, RepositoryMessage>(CreateConsumeContext(TestContext.Current.CancellationToken), instance.Instance);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));

        if (missing)
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(context, operation, consumeContext, cancellation.Token));
            Assert.Equal($"Saga {instance.Instance.CorrelationId} was not found in the in-memory repository.", exception.Message);
            Assert.False(instance.IsRemoved);
        }
        else
        {
            await InvokeAsync(context, operation, consumeContext, cancellation.Token);
            Assert.True(instance.IsRemoved);
        }

        Assert.False(cancellation.IsCancellationRequested);
        Assert.Null(dictionary[instance.Instance.CorrelationId]);
        Assert.Equal(0, dictionary.Count);
        context.Dispose();
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "invalidation-during-acquisition-stops-retrying-retained-removed-state")]
    public async Task InvalidationDuringAcquisition_ReturnsNullWithoutRetryingTheRetainedRemovedSagaAsync()
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        var instance = new SagaInstance<RepositoryState>(new RepositoryState());
        dictionary.Add(instance);
        var factory = new InvalidatingFactory();
        await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var context = new InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage>(dictionary, factory, CreateConsumeContext(TestContext.Current.CancellationToken));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));

        SagaConsumeContext<RepositoryState, RepositoryMessage>? result = await context.LoadAsync(instance.Instance.CorrelationId, cancellation.Token);

        Assert.Null(result);
        Assert.Equal(1, factory.Calls);
        Assert.False(cancellation.IsCancellationRequested);
        Assert.True(instance.IsRemoved);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        context.Dispose();
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData(SagaConsumeContextMode.Add)]
    [InlineData(SagaConsumeContextMode.Insert)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "active-creation-retains-dictionary-ownership-during-disposal")]
    public async Task DisposeDuringCreation_KeepsTheDictionaryLeaseUntilTheFactoryCompletesAsync(SagaConsumeContextMode mode)
    {
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        var factory = new BlockingCreationFactory();
        await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var context = new InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage>(dictionary, factory, CreateConsumeContext(TestContext.Current.CancellationToken));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        var state = new RepositoryState();
        Task<SagaConsumeContext<RepositoryState, RepositoryMessage>?> creation = mode == SagaConsumeContextMode.Insert
            ? context.InsertAsync(state, cancellation.Token)
            : CreateAddedContextAsync(context, state, cancellation.Token);
        Task? contender = null;
        try
        {
            await factory.Entered.Task.WaitAsync(cancellation.Token);
            context.Dispose();
            contender = dictionary.MarkInUseAsync(cancellation.Token);
            Assert.False(contender.IsCompleted);
            Assert.Equal(0, dictionary.Count);
            factory.Continue.TrySetResult();
            SagaConsumeContext<RepositoryState, RepositoryMessage> result = Assert.IsAssignableFrom<SagaConsumeContext<RepositoryState, RepositoryMessage>>(await creation);
            Assert.Same(state, result.Saga);
            await contender;
            Assert.Same(state, dictionary[state.CorrelationId]!.Instance);
            Assert.Equal(1, factory.Calls);
        }
        finally
        {
            factory.Continue.TrySetResult();
            cancellation.Cancel();
            try
            {
                SagaConsumeContext<RepositoryState, RepositoryMessage>? result = await creation;
                (result as IDisposable)?.Dispose();
            }
            catch (OperationCanceledException exception)
            {
                Assert.True(exception.CancellationToken == cancellation.Token || exception.CancellationToken == TestContext.Current.CancellationToken);
            }
            finally
            {
                if (contender != null)
                    await DrainDictionaryAcquisitionAsync(dictionary, contender, cancellation.Token);
            }
        }
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "active-delete-retains-dictionary-ownership-during-disposal")]
    public async Task DisposeDuringDelete_KeepsTheDictionaryLeaseUntilRemovalCompletesAsync()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        using var continueRemoval = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dictionary = new IndexedSagaDictionary<BlockingState>();
        var state = new BlockingState();
        var instance = new SagaInstance<BlockingState>(state);
        dictionary.Add(instance);
        await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var context = new InMemorySagaRepositoryContext<BlockingState, RepositoryMessage>(dictionary,
            new InMemorySagaConsumeContextFactory<BlockingState>(), CreateConsumeContext(TestContext.Current.CancellationToken));
        var consumeContext = new DefaultSagaConsumeContext<BlockingState, RepositoryMessage>(CreateConsumeContext(TestContext.Current.CancellationToken), state);
        state.OnReadCorrelationId = () =>
        {
            entered.TrySetResult();
            continueRemoval.Wait(cancellation.Token);
        };
        Task removal = Task.Run(() => context.DeleteAsync(consumeContext, cancellation.Token), TestContext.Current.CancellationToken);
        Task? contender = null;
        try
        {
            await entered.Task.WaitAsync(cancellation.Token);
            context.Dispose();
            contender = dictionary.MarkInUseAsync(cancellation.Token);
            Assert.False(contender.IsCompleted);
            Assert.False(instance.IsRemoved);
            continueRemoval.Set();
            await removal;
            await contender;
            Assert.True(instance.IsRemoved);
            Assert.Equal(0, dictionary.Count);
        }
        finally
        {
            continueRemoval.Set();
            cancellation.Cancel();
            try
            {
                await removal;
            }
            catch (OperationCanceledException exception)
            {
                Assert.Equal(cancellation.Token, exception.CancellationToken);
            }
            finally
            {
                if (contender != null)
                {
                    if (contender.IsCompletedSuccessfully)
                        dictionary.Release();
                    else
                    {
                        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => contender);
                        Assert.Equal(cancellation.Token, exception.CancellationToken);
                    }
                }
            }
        }
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "concurrent-initial-operations-release-only-after-the-final-active-user")]
    public async Task ConcurrentInitialOperations_KeepTheDictionaryLeaseUntilTheLastFactoryCompletesAsync()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        var factory = new BlockingCreationFactory();
        await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var context = new InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage>(dictionary, factory, CreateConsumeContext(cancellation.Token));
        var firstState = new RepositoryState();
        var secondState = new RepositoryState();
        Task<SagaConsumeContext<RepositoryState, RepositoryMessage>> first = context.AddAsync(firstState, cancellation.Token);
        Task<SagaConsumeContext<RepositoryState, RepositoryMessage>> second = context.AddAsync(secondState, cancellation.Token);
        Task? contender = null;
        try
        {
            await Task.WhenAll(factory.Entered.Task, factory.SecondEntered.Task).WaitAsync(cancellation.Token);
            context.Dispose();
            contender = dictionary.MarkInUseAsync(cancellation.Token);
            Assert.False(contender.IsCompleted);
            factory.Continue.TrySetResult();
            Assert.Same(firstState, (await first).Saga);
            Assert.False(contender.IsCompleted);
            Assert.Same(firstState, dictionary[firstState.CorrelationId]!.Instance);
            Assert.Null(dictionary[secondState.CorrelationId]);
            factory.SecondContinue.TrySetResult();
            Assert.Same(secondState, (await second).Saga);
            await contender;
            Assert.Same(secondState, dictionary[secondState.CorrelationId]!.Instance);
            Assert.Equal(2, factory.Calls);
        }
        finally
        {
            factory.Continue.TrySetResult();
            factory.SecondContinue.TrySetResult();
            cancellation.Cancel();
            try
            {
                await Task.WhenAll(new[] { first, second }.Select(operation => DrainCreatedContextAsync(operation, cancellation.Token)));
            }
            finally
            {
                if (contender != null)
                    await DrainDictionaryAcquisitionAsync(dictionary, contender, cancellation.Token);
            }
        }
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData(SagaConsumeContextMode.Add)]
    [InlineData(SagaConsumeContextMode.Insert)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "faulted-active-creation-releases-deferred-disposal-lease-and-preserves-fault")]
    public async Task FaultedInitialCreation_ReleasesTheDeferredLeaseAndPreservesTheExactFaultAsync(SagaConsumeContextMode mode)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        var expected = new InvalidOperationException("creation failed before registration");
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        var factory = new BlockingCreationFactory { Failure = expected };
        await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var context = new InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage>(dictionary, factory, CreateConsumeContext(cancellation.Token));
        var state = new RepositoryState();
        Task<SagaConsumeContext<RepositoryState, RepositoryMessage>?> creation = mode == SagaConsumeContextMode.Insert
            ? context.InsertAsync(state, cancellation.Token)
            : CreateAddedContextAsync(context, state, cancellation.Token);
        Task? contender = null;
        try
        {
            await factory.Entered.Task.WaitAsync(cancellation.Token);
            context.Dispose();
            contender = dictionary.MarkInUseAsync(cancellation.Token);
            Assert.False(contender.IsCompleted);
            factory.Continue.TrySetResult();
            InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() => creation);
            Assert.Same(expected, observed);
            await contender;
            Assert.Equal(0, dictionary.Count);
            Assert.Equal(1, factory.Calls);
            context.Dispose();
        }
        finally
        {
            factory.Continue.TrySetResult();
            cancellation.Cancel();
            try
            {
                SagaConsumeContext<RepositoryState, RepositoryMessage>? result = await creation;
                (result as IDisposable)?.Dispose();
            }
            catch (InvalidOperationException exception)
            {
                Assert.Same(expected, exception);
            }
            catch (OperationCanceledException)
            {
                Assert.True(cancellation.IsCancellationRequested);
            }
            finally
            {
                if (contender != null)
                    await DrainDictionaryAcquisitionAsync(dictionary, contender, cancellation.Token);
            }
        }
        await AssertDictionaryLeaseIsAvailableAsync(dictionary);
    }

    private static async Task DrainCreatedContextAsync(Task<SagaConsumeContext<RepositoryState, RepositoryMessage>> operation, CancellationToken cancellationToken)
    {
        try
        {
            SagaConsumeContext<RepositoryState, RepositoryMessage> result = await operation;
            (result as IDisposable)?.Dispose();
        }
        catch (OperationCanceledException)
        {
            Assert.True(cancellationToken.IsCancellationRequested);
        }
    }

    private static async Task<SagaConsumeContext<RepositoryState, RepositoryMessage>?> CreateAddedContextAsync(
        InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context, RepositoryState state, CancellationToken cancellationToken) =>
        await context.AddAsync(state, cancellationToken);

    private static async Task AssertDictionaryLeaseIsAvailableAsync<TSaga>(IndexedSagaDictionary<TSaga> dictionary)
        where TSaga : class, ISaga
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        await dictionary.MarkInUseAsync(cancellation.Token);
        dictionary.Release();
    }

    private static async Task DrainDictionaryAcquisitionAsync(IndexedSagaDictionary<RepositoryState> dictionary, Task acquisition, CancellationToken cancellationToken)
    {
        try
        {
            await acquisition;
            dictionary.Release();
        }
        catch (OperationCanceledException exception)
        {
            Assert.Equal(cancellationToken, exception.CancellationToken);
        }
    }

    private static Task InvokeAsync(InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage> context, string operation,
        SagaConsumeContext<RepositoryState>? consumeContext, CancellationToken cancellationToken = default) => operation switch
        {
            "Add" => context.AddAsync(null!, cancellationToken),
            "Insert" => context.InsertAsync(null!, cancellationToken),
            "Save" => context.SaveAsync(consumeContext!, cancellationToken),
            "Update" => context.UpdateAsync(consumeContext!, cancellationToken),
            "Undo" => context.UndoAsync(consumeContext!, cancellationToken),
            "Delete" => context.DeleteAsync(consumeContext!, cancellationToken),
            "Discard" => context.DiscardAsync(consumeContext!, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

    private static ConsumeContext<RepositoryMessage> CreateConsumeContext(CancellationToken cancellationToken = default) =>
        InMemoryOutboxTestContextFactory.Create(new RepositoryMessage(), cancellationToken);

    private static async Task<InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage>> CreateContextAsync(
        IndexedSagaDictionary<RepositoryState> dictionary, bool dictionaryHeld, CancellationToken cancellationToken = default)
    {
        await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
        var context = new InMemorySagaRepositoryContext<RepositoryState, RepositoryMessage>(dictionary,
            new InMemorySagaConsumeContextFactory<RepositoryState>(), CreateConsumeContext(cancellationToken));
        if (!dictionaryHeld)
        {
            var seed = new SagaInstance<RepositoryState>(new RepositoryState());
            dictionary.Add(seed);
            SagaConsumeContext<RepositoryState, RepositoryMessage> loaded = (await context.LoadAsync(seed.Instance.CorrelationId, cancellationToken: default))!;
            ((IDisposable)loaded).Dispose();
            dictionary.Remove(seed);
        }
        return context;
    }

    private sealed class BlockingCreationFactory : ISagaConsumeContextFactory<IndexedSagaDictionary<RepositoryState>, RepositoryState>
    {
        private int _calls;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondContinue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls => Volatile.Read(ref _calls);
        public Exception? Failure { get; set; }

        public async Task<SagaConsumeContext<RepositoryState, T>> CreateSagaConsumeContextAsync<T>(IndexedSagaDictionary<RepositoryState> context,
            ConsumeContext<T> consumeContext, RepositoryState instance, SagaConsumeContextMode mode)
            where T : class
        {
            int call = Interlocked.Increment(ref _calls);
            if (call == 1)
            {
                Entered.TrySetResult();
                await Continue.Task.WaitAsync(consumeContext.CancellationToken);
            }
            else
            {
                SecondEntered.TrySetResult();
                await SecondContinue.Task.WaitAsync(consumeContext.CancellationToken);
            }
            if (Failure is Exception failure)
                throw failure;

            return await new InMemorySagaConsumeContextFactory<RepositoryState>().CreateSagaConsumeContextAsync(context, consumeContext, instance, mode);
        }
    }

    private sealed class BlockingState : ISaga
    {
        private Guid _correlationId = NewId.NextGuid();
        public Action? OnReadCorrelationId { get; set; }

        public Guid CorrelationId
        {
            get
            {
                OnReadCorrelationId?.Invoke();
                return _correlationId;
            }
            set => _correlationId = value;
        }
    }

    private sealed class InvalidatingFactory : ISagaConsumeContextFactory<IndexedSagaDictionary<RepositoryState>, RepositoryState>
    {
        public int Calls { get; private set; }

        public Task<SagaConsumeContext<RepositoryState, T>> CreateSagaConsumeContextAsync<T>(IndexedSagaDictionary<RepositoryState> context,
            ConsumeContext<T> consumeContext, RepositoryState instance, SagaConsumeContextMode mode)
            where T : class
        {
            Calls++;
            context[instance.CorrelationId]!.Remove();
            return new InMemorySagaConsumeContextFactory<RepositoryState>().CreateSagaConsumeContextAsync(context, consumeContext, instance, mode);
        }
    }

    private sealed class RepositoryState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public int Value { get; set; }
    }

    public sealed record RepositoryMessage;
}
