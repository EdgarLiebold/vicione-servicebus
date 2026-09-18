using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class SagaRepositoryLifecycleDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "lifecycle-pipe-null-boundaries-and-probe-identity")]
    public async Task SagaPipes_RejectNullOwnersAndForwardTheExactProbeContextAsync()
    {
        var repository = new RepositoryHarness();
        var next = new RecordingPipe();

        AssertParameter("policy", () => new SendSagaPipe<TestSaga, TestMessage>(null!, next, Guid.Empty));
        AssertParameter("next", () => new SendSagaPipe<TestSaga, TestMessage>(new PolicyHarness(), null!, Guid.Empty));
        AssertParameter("policy", () => new SendQuerySagaPipe<TestSaga, TestMessage>(null!, next));
        AssertParameter("next", () => new SendQuerySagaPipe<TestSaga, TestMessage>(new PolicyHarness(), null!));
        AssertParameter("repositoryContext", () => new MissingSagaPipe<TestSaga, TestMessage>(null!, next));
        AssertParameter("next", () => new MissingSagaPipe<TestSaga, TestMessage>(repository.Context, null!));

        var direct = new SendSagaPipe<TestSaga, TestMessage>(new PolicyHarness(), next, Guid.Empty);
        var query = new SendQuerySagaPipe<TestSaga, TestMessage>(new PolicyHarness(), next);
        var missing = new MissingSagaPipe<TestSaga, TestMessage>(repository.Context, next);

        AssertParameter("context", () => direct.Probe(null!));
        AssertParameter("context", () => query.Probe(null!));
        AssertParameter("context", () => ((IProbeSite)missing).Probe(null!));
        Assert.Equal(0, next.ProbeCount);

        var probeContext = new RecordingProbeContext();
        direct.Probe(probeContext);
        query.Probe(probeContext);
        ((IProbeSite)missing).Probe(probeContext);

        Assert.Equal(3, next.ProbeCount);
        Assert.All(next.ProbeContexts, context => Assert.Same(probeContext, context));
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => direct.SendAsync(null!))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => query.SendAsync(null!))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => missing.SendAsync(null!))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "direct-preinsert-load-missing-order-and-identity")]
    public async Task DirectDispatch_PreservesPreInsertLoadAndMissingOrderAndIdentitiesAsync()
    {
        Guid correlationId = Guid.NewGuid();
        var preinsertSaga = new TestSaga { CorrelationId = correlationId };
        var inserted = new SagaContextHarness(preinsertSaga);
        var insertEvents = new List<string>();
        inserted.Events = insertEvents;
        var insertRepository = new RepositoryHarness(events: insertEvents)
        {
            InsertHandler = saga => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(inserted.Context),
            LoadHandler = _ => throw new Xunit.Sdk.XunitException("A successful pre-insert must skip LoadAsync."),
        };
        var insertPolicy = new PolicyHarness
        {
            PreInsert = true,
            PreInsertSaga = preinsertSaga,
            Events = insertEvents,
        };
        var next = new RecordingPipe();

        await new SendSagaPipe<TestSaga, TestMessage>(insertPolicy, next, correlationId).SendAsync(insertRepository.Context);

        Assert.Equal(["Insert", "Existing", "Update", "DisposeAsync"], insertEvents);
        Assert.Same(insertRepository.Context, insertPolicy.PreInsertContext);
        Assert.Same(preinsertSaga, Assert.Single(insertRepository.InsertInstances));
        Assert.Same(inserted.Context, Assert.Single(insertPolicy.ExistingContexts));
        Assert.Same(next, Assert.Single(insertPolicy.ExistingNextPipes));

        var loadedSaga = new TestSaga { CorrelationId = correlationId };
        var loaded = new SagaContextHarness(loadedSaga, isCompleted: true);
        var collisionEvents = new List<string>();
        loaded.Events = collisionEvents;
        var collisionRepository = new RepositoryHarness(events: collisionEvents)
        {
            InsertHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(null),
            LoadHandler = id => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(loaded.Context),
        };
        var collisionPolicy = new PolicyHarness
        {
            PreInsert = true,
            PreInsertSaga = preinsertSaga,
            Events = collisionEvents,
        };

        await new SendSagaPipe<TestSaga, TestMessage>(collisionPolicy, next, correlationId).SendAsync(collisionRepository.Context);

        Assert.Equal(["Insert", "Load", "Existing", "Delete", "DisposeAsync"], collisionEvents);
        Assert.Equal(correlationId, Assert.Single(collisionRepository.LoadIds));
        Assert.Same(loaded.Context, Assert.Single(collisionRepository.DeleteContexts));

        var missingEvents = new List<string>();
        var missingRepository = new RepositoryHarness(events: missingEvents)
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(null),
        };
        var missingPolicy = new PolicyHarness { Events = missingEvents };

        await new SendSagaPipe<TestSaga, TestMessage>(missingPolicy, next, correlationId).SendAsync(missingRepository.Context);

        Assert.Equal(["Load", "Missing"], missingEvents);
        Assert.Equal(correlationId, Assert.Single(missingRepository.LoadIds));
        Assert.Same(missingRepository.Context, Assert.Single(missingPolicy.MissingContexts));
        Assert.IsType<MissingSagaPipe<TestSaga, TestMessage>>(Assert.Single(missingPolicy.MissingNextPipes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "existing-saga-success-action-matrix")]
    public async Task ExistingDispatch_SelectsExactlyUndoDeleteOrUpdateAsync()
    {
        async Task<(RepositoryHarness Repository, SagaContextHarness Context)> RunAsync(bool readOnly, bool completed)
        {
            var owned = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() }, completed);
            var repository = new RepositoryHarness
            {
                LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(owned.Context),
            };
            var policy = new PolicyHarness { IsReadOnly = readOnly };

            await new SendSagaPipe<TestSaga, TestMessage>(policy, new RecordingPipe(), owned.Saga.CorrelationId)
                .SendAsync(repository.Context);

            return (repository, owned);
        }

        (RepositoryHarness readOnlyRepository, SagaContextHarness readOnlyContext) = await RunAsync(true, false);
        Assert.Same(readOnlyContext.Context, Assert.Single(readOnlyRepository.UndoContexts));
        Assert.Empty(readOnlyRepository.DeleteContexts);
        Assert.Empty(readOnlyRepository.UpdateContexts);

        (RepositoryHarness completedRepository, SagaContextHarness completedContext) = await RunAsync(false, true);
        Assert.Same(completedContext.Context, Assert.Single(completedRepository.DeleteContexts));
        Assert.Empty(completedRepository.UndoContexts);
        Assert.Empty(completedRepository.UpdateContexts);

        (RepositoryHarness activeRepository, SagaContextHarness activeContext) = await RunAsync(false, false);
        Assert.Same(activeContext.Context, Assert.Single(activeRepository.UpdateContexts));
        Assert.Empty(activeRepository.UndoContexts);
        Assert.Empty(activeRepository.DeleteContexts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "existing-failure-no-invented-rollback-and-causal-cleanup")]
    public async Task ExistingFailure_DoesNotInventAPersistenceRollbackAndRetainsCausalFailureOrderAsync()
    {
        var operationFailure = new InvalidOperationException("existing operation failed");
        var disposalFailure = new ApplicationException("existing disposal failed");
        var owned = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() })
        {
            AsyncDisposeFailure = disposalFailure,
        };
        var repository = new RepositoryHarness
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(owned.Context),
        };
        var policy = new PolicyHarness
        {
            ExistingHandler = _ => Task.FromException(operationFailure),
        };

        AggregateException aggregate = await Assert.ThrowsAsync<AggregateException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(policy, new RecordingPipe(), owned.Saga.CorrelationId)
                .SendAsync(repository.Context));

        Assert.Collection(
            aggregate.InnerExceptions,
            exception => Assert.Same(operationFailure, exception),
            exception => Assert.Same(disposalFailure, exception));
        // The repository contract reserves Discard for newly added state. Undo is the successful
        // read-only action, and the in-memory implementation explicitly does not restore values.
        Assert.Empty(repository.SaveContexts);
        Assert.Empty(repository.DiscardContexts);
        Assert.Empty(repository.UndoContexts);
        Assert.Empty(repository.UpdateContexts);
        Assert.Empty(repository.DeleteContexts);
        Assert.Equal(1, owned.AsyncDisposeCount);
        Assert.Equal(0, owned.DisposeCount);

        var singleFailureContext = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        var singleFailureRepository = new RepositoryHarness
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(singleFailureContext.Context),
        };
        var singleFailurePolicy = new PolicyHarness
        {
            ExistingHandler = _ => Task.FromException(operationFailure),
        };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(singleFailurePolicy, new RecordingPipe(), singleFailureContext.Saga.CorrelationId)
                .SendAsync(singleFailureRepository.Context));
        Assert.Same(operationFailure, actual);
    }

    [Theory]
    [InlineData(RepositoryAction.Undo)]
    [InlineData(RepositoryAction.Delete)]
    [InlineData(RepositoryAction.Update)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "existing-persistence-action-failure-no-rollback")]
    public async Task ExistingPersistenceFailure_DisposesWithoutInventingAnotherRepositoryActionAsync(RepositoryAction action)
    {
        var actionFailure = new InvalidOperationException($"{action} failed");
        var owned = new SagaContextHarness(
            new TestSaga { CorrelationId = Guid.NewGuid() },
            isCompleted: action == RepositoryAction.Delete);
        var repository = new RepositoryHarness
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(owned.Context),
            UndoHandler = _ => action == RepositoryAction.Undo ? Task.FromException(actionFailure) : Task.CompletedTask,
            DeleteHandler = _ => action == RepositoryAction.Delete ? Task.FromException(actionFailure) : Task.CompletedTask,
            UpdateHandler = _ => action == RepositoryAction.Update ? Task.FromException(actionFailure) : Task.CompletedTask,
        };
        var policy = new PolicyHarness { IsReadOnly = action == RepositoryAction.Undo };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(policy, new RecordingPipe(), owned.Saga.CorrelationId)
                .SendAsync(repository.Context));

        Assert.Same(actionFailure, actual);
        Assert.Equal(action == RepositoryAction.Undo ? 1 : 0, repository.UndoContexts.Count);
        Assert.Equal(action == RepositoryAction.Delete ? 1 : 0, repository.DeleteContexts.Count);
        Assert.Equal(action == RepositoryAction.Update ? 1 : 0, repository.UpdateContexts.Count);
        Assert.Empty(repository.SaveContexts);
        Assert.Empty(repository.DiscardContexts);
        Assert.Equal(1, owned.AsyncDisposeCount);
        Assert.Equal(0, owned.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-QUERY", "query-sequential-identity-preserving-traversal")]
    public async Task QueryDispatch_LoadsAndProcessesUsableIdentitiesSequentiallyAsync()
    {
        Guid missingId = Guid.NewGuid();
        var first = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        var second = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        Guid[] ids = [missingId, first.Saga.CorrelationId, second.Saga.CorrelationId];
        var repository = new RepositoryHarness(ids, count: ids.Length)
        {
            LoadHandler = id => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(id switch
            {
                var value when value == first.Saga.CorrelationId => first.Context,
                var value when value == second.Saga.CorrelationId => second.Context,
                _ => null,
            }),
        };
        var firstEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var policy = new PolicyHarness
        {
            ExistingHandler = context =>
            {
                if (ReferenceEquals(context, first.Context))
                {
                    firstEntered.TrySetResult(true);
                    return releaseFirst.Task;
                }

                return Task.CompletedTask;
            },
        };

        Task operation = new SendQuerySagaPipe<TestSaga, TestMessage>(policy, new RecordingPipe())
            .SendAsync(repository.QueryContext);
        await firstEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal([missingId, first.Saga.CorrelationId], repository.LoadIds);
        Assert.Single(policy.ExistingContexts);
        Assert.Same(first.Context, policy.ExistingContexts[0]);
        Assert.Equal(0, second.AsyncDisposeCount);

        releaseFirst.SetResult(true);
        await operation;

        Assert.Equal(ids, repository.LoadIds);
        Assert.Equal([first.Context, second.Context], policy.ExistingContexts);
        Assert.Equal([first.Context, second.Context], repository.UpdateContexts);
        Assert.Equal(1, first.AsyncDisposeCount);
        Assert.Equal(1, second.AsyncDisposeCount);
        Assert.Empty(policy.MissingContexts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-QUERY", "query-empty-and-stale-results-route-missing-once")]
    public async Task QueryDispatch_RoutesEmptyAndStaleResultsToMissingExactlyOnceAsync()
    {
        var emptyRepository = new RepositoryHarness([], count: 0);
        var emptyPolicy = new PolicyHarness();

        await new SendQuerySagaPipe<TestSaga, TestMessage>(emptyPolicy, new RecordingPipe())
            .SendAsync(emptyRepository.QueryContext);

        Assert.Equal(0, emptyRepository.EnumerationCount);
        Assert.Empty(emptyRepository.LoadIds);
        Assert.Same(emptyRepository.Context, Assert.Single(emptyPolicy.MissingContexts));

        Guid[] staleIds = [Guid.NewGuid(), Guid.NewGuid()];
        var staleRepository = new RepositoryHarness(staleIds, count: staleIds.Length)
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(null),
        };
        var stalePolicy = new PolicyHarness();

        await new SendQuerySagaPipe<TestSaga, TestMessage>(stalePolicy, new RecordingPipe())
            .SendAsync(staleRepository.QueryContext);

        Assert.Equal(staleIds, staleRepository.LoadIds);
        Assert.Equal(1, staleRepository.EnumerationCount);
        Assert.Same(staleRepository.Context, Assert.Single(stalePolicy.MissingContexts));
        Assert.IsType<MissingSagaPipe<TestSaga, TestMessage>>(Assert.Single(stalePolicy.MissingNextPipes));
        Assert.Empty(stalePolicy.ExistingContexts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "missing-add-forwarding-and-save-discard-matrix")]
    public async Task MissingDispatch_ForwardsExactContextsAndSelectsSaveOrDiscardAsync()
    {
        async Task<(RepositoryHarness Repository, SagaContextHarness Added, SagaContextHarness Input, RecordingPipe Next)> RunAsync(bool completed)
        {
            var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
            var added = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() }, completed);
            var repository = new RepositoryHarness
            {
                AddHandler = _ => Task.FromResult(added.Context),
            };
            var next = new RecordingPipe();

            await new MissingSagaPipe<TestSaga, TestMessage>(repository.Context, next).SendAsync(input.Context);

            return (repository, added, input, next);
        }

        (RepositoryHarness activeRepository, SagaContextHarness activeAdded, SagaContextHarness activeInput, RecordingPipe activeNext) =
            await RunAsync(false);
        Assert.Same(activeInput.Saga, Assert.Single(activeRepository.AddInstances));
        Assert.Same(activeAdded.Context, Assert.Single(activeNext.SendContexts));
        Assert.Same(activeAdded.Context, Assert.Single(activeRepository.SaveContexts));
        Assert.Empty(activeRepository.DiscardContexts);
        Assert.Equal(1, activeAdded.AsyncDisposeCount);
        Assert.Equal(0, activeAdded.DisposeCount);

        (RepositoryHarness completedRepository, SagaContextHarness completedAdded, SagaContextHarness completedInput, RecordingPipe completedNext) =
            await RunAsync(true);
        Assert.Same(completedInput.Saga, Assert.Single(completedRepository.AddInstances));
        Assert.Same(completedAdded.Context, Assert.Single(completedNext.SendContexts));
        Assert.Same(completedAdded.Context, Assert.Single(completedRepository.DiscardContexts));
        Assert.Empty(completedRepository.SaveContexts);
        Assert.Equal(1, completedAdded.AsyncDisposeCount);
        Assert.Equal(0, completedAdded.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "completed-discard-failure-is-not-retried")]
    public async Task CompletedDiscardFailure_IsNotInvokedTwiceAndPreservesItsIdentityAsync()
    {
        var discardFailure = new InvalidOperationException("discard failed");
        var added = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() }, isCompleted: true);
        var repository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult(added.Context),
            DiscardHandler = _ => Task.FromException(discardFailure),
        };
        var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(repository.Context, new RecordingPipe()).SendAsync(input.Context));

        Assert.Same(discardFailure, actual);
        Assert.Single(repository.DiscardContexts);
        Assert.Empty(repository.SaveContexts);
        Assert.Equal(1, added.AsyncDisposeCount);
        Assert.Equal(0, added.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "missing-save-failure-compensation-and-disposal-order")]
    public async Task MissingSaveFailure_CompensatesOnceAndPreservesEveryFailureInCausalOrderAsync()
    {
        var saveFailure = new InvalidOperationException("save failed");
        var discardFailure = new ApplicationException("discard compensation failed");
        var disposalFailure = new NotSupportedException("dispose failed");
        var events = new List<string>();
        var added = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() })
        {
            Events = events,
            AsyncDisposeFailure = disposalFailure,
        };
        var repository = new RepositoryHarness(events: events)
        {
            AddHandler = _ => Task.FromResult(added.Context),
            SaveHandler = _ => Task.FromException(saveFailure),
            DiscardHandler = _ => Task.FromException(discardFailure),
        };
        var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(repository.Context, new RecordingPipe()).SendAsync(input.Context));

        Assert.Collection(
            actual.InnerExceptions,
            exception => Assert.Same(saveFailure, exception),
            exception => Assert.Same(discardFailure, exception),
            exception => Assert.Same(disposalFailure, exception));
        Assert.Equal(["Add", "Save", "Discard", "DisposeAsync"], events);
        Assert.Single(repository.SaveContexts);
        Assert.Single(repository.DiscardContexts);
        Assert.Equal(1, added.AsyncDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "missing-successful-save-disposal-failure-no-rollback")]
    public async Task MissingSuccessfulSave_WithDisposalFailureDoesNotInventDiscardAsync()
    {
        var disposalFailure = new InvalidOperationException("dispose after save failed");
        var added = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() })
        {
            AsyncDisposeFailure = disposalFailure,
        };
        var repository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult(added.Context),
        };
        var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(repository.Context, new RecordingPipe()).SendAsync(input.Context));

        Assert.Same(disposalFailure, actual);
        Assert.Same(added.Context, Assert.Single(repository.SaveContexts));
        Assert.Empty(repository.DiscardContexts);
        Assert.Equal(1, added.AsyncDisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "missing-operation-or-cancellation-cleanup-causal-order")]
    public async Task MissingFailure_PreservesOperationDiscardAndDisposalInCausalOrderAsync(bool cancellation)
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        Exception operationFailure = cancellation
            ? new OperationCanceledException("operation canceled", cancellationSource.Token)
            : new InvalidOperationException("operation failed");
        var discardFailure = new ApplicationException("discard failed");
        var disposalFailure = new NotSupportedException("dispose failed");
        var added = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() })
        {
            AsyncDisposeFailure = disposalFailure,
        };
        var repository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult(added.Context),
            DiscardHandler = _ => Task.FromException(discardFailure),
        };
        var next = new RecordingPipe
        {
            SendHandler = _ => Task.FromException(operationFailure),
        };
        var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });

        AggregateException aggregate = await Assert.ThrowsAsync<AggregateException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(repository.Context, next).SendAsync(input.Context));

        Assert.Collection(
            aggregate.InnerExceptions,
            exception => Assert.Same(operationFailure, exception),
            exception => Assert.Same(discardFailure, exception),
            exception => Assert.Same(disposalFailure, exception));
        Assert.Single(repository.DiscardContexts);
        Assert.Empty(repository.SaveContexts);
        Assert.Equal(1, added.AsyncDisposeCount);
        Assert.Equal(0, added.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "single-cancellation-token-state-cleanup-and-query-fail-fast")]
    public async Task SingleCancellation_PreservesTokenAndCanceledStateWhileCleaningUpAndStoppingQueryAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var operationCancellation = new OperationCanceledException("repository operation canceled", cancellation.Token);

        var existing = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        var existingRepository = new RepositoryHarness
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(existing.Context),
        };
        var existingPolicy = new PolicyHarness
        {
            ExistingHandler = _ => Task.FromException(operationCancellation),
        };
        Task existingOperation = new SendSagaPipe<TestSaga, TestMessage>(
                existingPolicy,
                new RecordingPipe(),
                existing.Saga.CorrelationId)
            .SendAsync(existingRepository.Context);

        OperationCanceledException existingActual =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => existingOperation);

        Assert.Equal(cancellation.Token, existingActual.CancellationToken);
        Assert.True(existingOperation.IsCanceled);
        Assert.Equal(1, existing.AsyncDisposeCount);
        Assert.Empty(existingRepository.UndoContexts);
        Assert.Empty(existingRepository.UpdateContexts);
        Assert.Empty(existingRepository.DeleteContexts);

        var added = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        var addedRepository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult(added.Context),
        };
        var canceledNext = new RecordingPipe
        {
            SendHandler = _ => Task.FromException(operationCancellation),
        };
        var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        Task missingOperation = new MissingSagaPipe<TestSaga, TestMessage>(addedRepository.Context, canceledNext)
            .SendAsync(input.Context);

        OperationCanceledException missingActual =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => missingOperation);

        Assert.Equal(cancellation.Token, missingActual.CancellationToken);
        Assert.True(missingOperation.IsCanceled);
        Assert.Single(addedRepository.DiscardContexts);
        Assert.Equal(1, added.AsyncDisposeCount);

        var queryFirst = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        var second = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        Guid[] ids = [queryFirst.Saga.CorrelationId, second.Saga.CorrelationId];
        var queryRepository = new RepositoryHarness(ids, count: ids.Length)
        {
            LoadHandler = id => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(
                id == queryFirst.Saga.CorrelationId ? queryFirst.Context : second.Context),
        };
        var queryPolicy = new PolicyHarness
        {
            ExistingHandler = _ => Task.FromException(operationCancellation),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new SendQuerySagaPipe<TestSaga, TestMessage>(queryPolicy, new RecordingPipe())
                .SendAsync(queryRepository.QueryContext));

        Assert.Equal([queryFirst.Saga.CorrelationId], queryRepository.LoadIds);
        Assert.Equal(1, queryFirst.AsyncDisposeCount);
        Assert.Equal(0, second.AsyncDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "lifecycle-single-failure-identity-and-sync-disposal-fallback")]
    public async Task Lifecycle_PreservesSingleFailureIdentityAndUsesSyncDisposalOnlyAsFallbackAsync()
    {
        var operationFailure = new InvalidOperationException("next failed");
        var added = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() }, ownership: Ownership.Sync);
        var repository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult(added.Context),
        };
        var next = new RecordingPipe
        {
            SendHandler = _ => Task.FromException(operationFailure),
        };
        var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(repository.Context, next).SendAsync(input.Context));

        Assert.Same(operationFailure, actual);
        Assert.Single(repository.DiscardContexts);
        Assert.Equal(0, added.AsyncDisposeCount);
        Assert.Equal(1, added.DisposeCount);

        var disposalFailure = new ApplicationException("dispose only failed");
        var active = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() })
        {
            AsyncDisposeFailure = disposalFailure,
        };
        var activeRepository = new RepositoryHarness
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(active.Context),
        };

        ApplicationException disposalActual = await Assert.ThrowsAsync<ApplicationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(new PolicyHarness(), new RecordingPipe(), active.Saga.CorrelationId)
                .SendAsync(activeRepository.Context));
        Assert.Same(disposalFailure, disposalActual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "logging-failures-remain-inside-owned-cleanup")]
    public async Task LoggingFailures_StillDisposeExistingAndDiscardNewSagaContextsAsync()
    {
        ILogContext? previous = LogContext.Current;
        var loggingFailure = new InvalidOperationException("logging failed");
        try
        {
            LogContext.ConfigureCurrentLogContext(new ThrowingLogger(loggingFailure));

            var existing = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
            var existingRepository = new RepositoryHarness([existing.Saga.CorrelationId], count: 1)
            {
                LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(existing.Context),
            };
            var existingPolicy = new PolicyHarness();

            InvalidOperationException existingActual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new SendQuerySagaPipe<TestSaga, TestMessage>(existingPolicy, new RecordingPipe())
                    .SendAsync(existingRepository.QueryContext));

            Assert.Same(loggingFailure, existingActual);
            Assert.Empty(existingPolicy.ExistingContexts);
            Assert.Empty(existingRepository.UndoContexts);
            Assert.Empty(existingRepository.UpdateContexts);
            Assert.Empty(existingRepository.DeleteContexts);
            Assert.Equal(1, existing.AsyncDisposeCount);

            var added = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
            var addedRepository = new RepositoryHarness
            {
                AddHandler = _ => Task.FromResult(added.Context),
            };
            var addedNext = new RecordingPipe();
            var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });

            InvalidOperationException addedActual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new MissingSagaPipe<TestSaga, TestMessage>(addedRepository.Context, addedNext).SendAsync(input.Context));

            Assert.Same(loggingFailure, addedActual);
            Assert.Empty(addedNext.SendContexts);
            Assert.Same(added.Context, Assert.Single(addedRepository.DiscardContexts));
            Assert.Empty(addedRepository.SaveContexts);
            Assert.Equal(1, added.AsyncDisposeCount);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "lifecycle-collaborator-null-task-boundaries")]
    public async Task SagaPipes_RejectNullCollaboratorTasksBeforeLosingOwnershipAsync()
    {
        var nullLoadRepository = new RepositoryHarness
        {
            LoadHandler = _ => null!,
        };
        InvalidOperationException loadFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(new PolicyHarness(), new RecordingPipe(), Guid.NewGuid())
                .SendAsync(nullLoadRepository.Context));
        Assert.Contains("null load task", loadFailure.Message, StringComparison.OrdinalIgnoreCase);

        var existing = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        var existingRepository = new RepositoryHarness
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(existing.Context),
        };
        var nullPolicy = new PolicyHarness { ExistingHandler = _ => null! };
        InvalidOperationException existingFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(nullPolicy, new RecordingPipe(), existing.Saga.CorrelationId)
                .SendAsync(existingRepository.Context));
        Assert.Contains("null existing-saga task", existingFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, existing.AsyncDisposeCount);

        var nullAddRepository = new RepositoryHarness
        {
            AddHandler = _ => null!,
        };
        var input = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        InvalidOperationException addFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(nullAddRepository.Context, new RecordingPipe()).SendAsync(input.Context));
        Assert.Contains("null add task", addFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(nullAddRepository.DiscardContexts);

        var nullPreInsertPolicy = new PolicyHarness { PreInsert = true, ReturnNullPreInsertSaga = true };
        var untouchedPreInsertRepository = new RepositoryHarness();
        InvalidOperationException preInsertFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(nullPreInsertPolicy, new RecordingPipe(), Guid.NewGuid())
                .SendAsync(untouchedPreInsertRepository.Context));
        Assert.Contains("null pre-insert instance", preInsertFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(untouchedPreInsertRepository.InsertInstances);

        var nullInsertRepository = new RepositoryHarness { InsertHandler = _ => null! };
        var insertPolicy = new PolicyHarness
        {
            PreInsert = true,
            PreInsertSaga = new TestSaga { CorrelationId = Guid.NewGuid() },
        };
        InvalidOperationException insertFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(insertPolicy, new RecordingPipe(), Guid.NewGuid())
                .SendAsync(nullInsertRepository.Context));
        Assert.Contains("null insert task", insertFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(nullInsertRepository.InsertInstances);

        var nullMissingRepository = new RepositoryHarness
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(null),
        };
        var nullMissingPolicy = new PolicyHarness { MissingHandler = (_, _) => null! };
        InvalidOperationException missingFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(nullMissingPolicy, new RecordingPipe(), Guid.NewGuid())
                .SendAsync(nullMissingRepository.Context));
        Assert.Contains("null missing-saga task", missingFailure.Message, StringComparison.OrdinalIgnoreCase);

        var nullQueryLoadRepository = new RepositoryHarness([Guid.NewGuid()], count: 1)
        {
            LoadHandler = _ => null!,
        };
        InvalidOperationException queryLoadFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendQuerySagaPipe<TestSaga, TestMessage>(new PolicyHarness(), new RecordingPipe())
                .SendAsync(nullQueryLoadRepository.QueryContext));
        Assert.Contains("null load task", queryLoadFailure.Message, StringComparison.OrdinalIgnoreCase);

        var nullAddedContextRepository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>>(null!),
        };
        InvalidOperationException addedContextFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(nullAddedContextRepository.Context, new RecordingPipe())
                .SendAsync(input.Context));
        Assert.Contains("null added saga context", addedContextFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(nullAddedContextRepository.DiscardContexts);

        var downstreamOwned = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        var nullDownstreamRepository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult(downstreamOwned.Context),
        };
        var nullDownstream = new RecordingPipe { SendHandler = _ => null! };
        InvalidOperationException downstreamFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(nullDownstreamRepository.Context, nullDownstream)
                .SendAsync(input.Context));
        Assert.Contains("null task", downstreamFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(nullDownstreamRepository.DiscardContexts);
        Assert.Equal(1, downstreamOwned.AsyncDisposeCount);

        var nullSaveOwned = new SagaContextHarness(new TestSaga { CorrelationId = Guid.NewGuid() });
        var nullSaveRepository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult(nullSaveOwned.Context),
            SaveHandler = _ => null!,
        };
        InvalidOperationException saveFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(nullSaveRepository.Context, new RecordingPipe())
                .SendAsync(input.Context));
        Assert.Contains("null save task", saveFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(nullSaveRepository.DiscardContexts);
        Assert.Equal(1, nullSaveOwned.AsyncDisposeCount);

        var nullDiscardOwned = new SagaContextHarness(
            new TestSaga { CorrelationId = Guid.NewGuid() },
            isCompleted: true);
        var nullDiscardRepository = new RepositoryHarness
        {
            AddHandler = _ => Task.FromResult(nullDiscardOwned.Context),
            DiscardHandler = _ => null!,
        };
        InvalidOperationException discardFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingSagaPipe<TestSaga, TestMessage>(nullDiscardRepository.Context, new RecordingPipe())
                .SendAsync(input.Context));
        Assert.Contains("null discard task", discardFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(nullDiscardRepository.DiscardContexts);
        Assert.Equal(1, nullDiscardOwned.AsyncDisposeCount);
    }

    [Theory]
    [InlineData(RepositoryAction.Undo)]
    [InlineData(RepositoryAction.Delete)]
    [InlineData(RepositoryAction.Update)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "existing-persistence-null-task-boundaries")]
    public async Task ExistingPersistenceActions_RejectNullTasksAndStillDisposeAsync(RepositoryAction action)
    {
        var owned = new SagaContextHarness(
            new TestSaga { CorrelationId = Guid.NewGuid() },
            isCompleted: action == RepositoryAction.Delete);
        var repository = new RepositoryHarness
        {
            LoadHandler = _ => Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(owned.Context),
            UndoHandler = _ => action == RepositoryAction.Undo ? null! : Task.CompletedTask,
            DeleteHandler = _ => action == RepositoryAction.Delete ? null! : Task.CompletedTask,
            UpdateHandler = _ => action == RepositoryAction.Update ? null! : Task.CompletedTask,
        };
        var policy = new PolicyHarness { IsReadOnly = action == RepositoryAction.Undo };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SendSagaPipe<TestSaga, TestMessage>(policy, new RecordingPipe(), owned.Saga.CorrelationId)
                .SendAsync(repository.Context));

        Assert.Contains($"null {action.ToString().ToLowerInvariant()} task", actual.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, owned.AsyncDisposeCount);
    }

    private static void AssertParameter(string expected, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(expected, exception.ParamName);
    }

    private sealed class RepositoryHarness
    {
        readonly IReadOnlyList<Guid> _ids;
        readonly List<string>? _events;

        public RepositoryHarness(IEnumerable<Guid>? ids = null, int? count = null, List<string>? events = null)
        {
            _ids = (ids ?? []).ToArray();
            _events = events;
            Count = count ?? _ids.Count;
            QueryContext = CreateProxy<ISagaRepositoryQueryContext<TestSaga, TestMessage>>(Invoke);
        }

        public ISagaRepositoryContext<TestSaga, TestMessage> Context => QueryContext;
        public ISagaRepositoryQueryContext<TestSaga, TestMessage> QueryContext { get; }
        public int Count { get; }
        public int EnumerationCount { get; private set; }
        public Func<TestSaga, Task<SagaConsumeContext<TestSaga, TestMessage>>> AddHandler { get; init; } =
            _ => throw new Xunit.Sdk.XunitException("Unexpected AddAsync call.");
        public Func<TestSaga, Task<SagaConsumeContext<TestSaga, TestMessage>?>> InsertHandler { get; init; } =
            _ => throw new Xunit.Sdk.XunitException("Unexpected InsertAsync call.");
        public Func<Guid, Task<SagaConsumeContext<TestSaga, TestMessage>?>> LoadHandler { get; init; } =
            _ => throw new Xunit.Sdk.XunitException("Unexpected LoadAsync call.");
        public Func<SagaConsumeContext<TestSaga, TestMessage>, Task> SaveHandler { get; init; } = _ => Task.CompletedTask;
        public Func<SagaConsumeContext<TestSaga, TestMessage>, Task> DiscardHandler { get; init; } = _ => Task.CompletedTask;
        public Func<SagaConsumeContext<TestSaga, TestMessage>, Task> UndoHandler { get; init; } = _ => Task.CompletedTask;
        public Func<SagaConsumeContext<TestSaga, TestMessage>, Task> UpdateHandler { get; init; } = _ => Task.CompletedTask;
        public Func<SagaConsumeContext<TestSaga, TestMessage>, Task> DeleteHandler { get; init; } = _ => Task.CompletedTask;
        public List<TestSaga> AddInstances { get; } = [];
        public List<TestSaga> InsertInstances { get; } = [];
        public List<Guid> LoadIds { get; } = [];
        public List<SagaConsumeContext<TestSaga, TestMessage>> SaveContexts { get; } = [];
        public List<SagaConsumeContext<TestSaga, TestMessage>> DiscardContexts { get; } = [];
        public List<SagaConsumeContext<TestSaga, TestMessage>> UndoContexts { get; } = [];
        public List<SagaConsumeContext<TestSaga, TestMessage>> UpdateContexts { get; } = [];
        public List<SagaConsumeContext<TestSaga, TestMessage>> DeleteContexts { get; } = [];

        object? Invoke(MethodInfo method, object?[]? arguments)
        {
            arguments ??= [];
            switch (method.Name)
            {
                case "get_Count":
                    return Count;
                case "GetEnumerator":
                    EnumerationCount++;
                    return method.ReturnType == typeof(IEnumerator<Guid>)
                        ? _ids.GetEnumerator()
                        : ((IEnumerable)_ids).GetEnumerator();
                case "AddAsync":
                    {
                        _events?.Add("Add");
                        var saga = Assert.IsType<TestSaga>(arguments[0]);
                        AddInstances.Add(saga);
                        return AddHandler(saga);
                    }
                case "InsertAsync":
                    {
                        _events?.Add("Insert");
                        var saga = Assert.IsType<TestSaga>(arguments[0]);
                        InsertInstances.Add(saga);
                        return InsertHandler(saga);
                    }
                case "LoadAsync":
                    {
                        _events?.Add("Load");
                        var correlationId = Assert.IsType<Guid>(arguments[0]);
                        LoadIds.Add(correlationId);
                        return LoadHandler(correlationId);
                    }
                case "SaveAsync":
                    return Record("Save", arguments, SaveContexts, SaveHandler);
                case "DiscardAsync":
                    return Record("Discard", arguments, DiscardContexts, DiscardHandler);
                case "UndoAsync":
                    return Record("Undo", arguments, UndoContexts, UndoHandler);
                case "UpdateAsync":
                    return Record("Update", arguments, UpdateContexts, UpdateHandler);
                case "DeleteAsync":
                    return Record("Delete", arguments, DeleteContexts, DeleteHandler);
                default:
                    return DefaultReturn(method.ReturnType, QueryContext);
            }
        }

        Task Record(
            string name,
            object?[] arguments,
            List<SagaConsumeContext<TestSaga, TestMessage>> contexts,
            Func<SagaConsumeContext<TestSaga, TestMessage>, Task> handler)
        {
            _events?.Add(name);
            var context = Assert.IsAssignableFrom<SagaConsumeContext<TestSaga, TestMessage>>(arguments[0]);
            contexts.Add(context);
            return handler(context);
        }
    }

    private sealed class PolicyHarness : ISagaPolicy<TestSaga, TestMessage>
    {
        public bool IsReadOnly { get; init; }
        public bool PreInsert { get; init; }
        public bool ReturnNullPreInsertSaga { get; init; }
        public TestSaga? PreInsertSaga { get; init; }
        public ConsumeContext<TestMessage>? PreInsertContext { get; private set; }
        public List<string>? Events { get; init; }
        public Func<SagaConsumeContext<TestSaga, TestMessage>, Task> ExistingHandler { get; init; } = _ => Task.CompletedTask;
        public Func<ConsumeContext<TestMessage>, IPipe<SagaConsumeContext<TestSaga, TestMessage>>, Task> MissingHandler { get; init; } =
            (_, _) => Task.CompletedTask;
        public List<SagaConsumeContext<TestSaga, TestMessage>> ExistingContexts { get; } = [];
        public List<IPipe<SagaConsumeContext<TestSaga, TestMessage>>> ExistingNextPipes { get; } = [];
        public List<ConsumeContext<TestMessage>> MissingContexts { get; } = [];
        public List<IPipe<SagaConsumeContext<TestSaga, TestMessage>>> MissingNextPipes { get; } = [];

        public bool PreInsertInstance(ConsumeContext<TestMessage> context, [NotNullWhen(true)] out TestSaga? instance)
        {
            PreInsertContext = context;
            if (PreInsert)
            {
                if (ReturnNullPreInsertSaga)
                {
                    instance = null!;
                    return true;
                }

                instance = PreInsertSaga
                    ?? throw new Xunit.Sdk.XunitException("A pre-insert test policy requires a saga instance.");
                return true;
            }

            instance = null;
            return false;
        }

        public Task ExistingAsync(
            SagaConsumeContext<TestSaga, TestMessage> context,
            IPipe<SagaConsumeContext<TestSaga, TestMessage>> next)
        {
            Events?.Add("Existing");
            ExistingContexts.Add(context);
            ExistingNextPipes.Add(next);
            return ExistingHandler(context);
        }

        public Task MissingAsync(
            ConsumeContext<TestMessage> context,
            IPipe<SagaConsumeContext<TestSaga, TestMessage>> next)
        {
            Events?.Add("Missing");
            MissingContexts.Add(context);
            MissingNextPipes.Add(next);
            return MissingHandler(context, next);
        }
    }

    private sealed class RecordingPipe : IPipe<SagaConsumeContext<TestSaga, TestMessage>>
    {
        public Func<SagaConsumeContext<TestSaga, TestMessage>, Task> SendHandler { get; init; } = _ => Task.CompletedTask;
        public List<SagaConsumeContext<TestSaga, TestMessage>> SendContexts { get; } = [];
        public int ProbeCount { get; private set; }
        public List<ProbeContext> ProbeContexts { get; } = [];

        public Task SendAsync(SagaConsumeContext<TestSaga, TestMessage> context)
        {
            SendContexts.Add(context);
            return SendHandler(context);
        }

        public void Probe(ProbeContext context)
        {
            ProbeCount++;
            ProbeContexts.Add(context);
        }
    }

    private sealed class SagaContextHarness
    {
        public SagaContextHarness(TestSaga saga, bool isCompleted = false, Ownership ownership = Ownership.AsyncAndSync)
        {
            Saga = saga;
            IsCompleted = isCompleted;
            Context = ownership switch
            {
                Ownership.AsyncAndSync => CreateProxy<AsyncSyncSagaContext>(Invoke),
                Ownership.Sync => CreateProxy<SyncSagaContext>(Invoke),
                _ => throw new ArgumentOutOfRangeException(nameof(ownership)),
            };
        }

        public TestSaga Saga { get; }
        public bool IsCompleted { get; }
        public SagaConsumeContext<TestSaga, TestMessage> Context { get; }
        public List<string>? Events { get; set; }
        public Exception? AsyncDisposeFailure { get; init; }
        public Exception? DisposeFailure { get; init; }
        public int AsyncDisposeCount { get; private set; }
        public int DisposeCount { get; private set; }

        object? Invoke(MethodInfo method, object?[]? arguments)
        {
            switch (method.Name)
            {
                case "get_Saga":
                    return Saga;
                case "get_IsCompleted":
                    return IsCompleted;
                case "get_CorrelationId":
                    return (Guid?)Saga.CorrelationId;
                case "get_Message":
                    return new TestMessage();
                case "DisposeAsync":
                    Events?.Add("DisposeAsync");
                    AsyncDisposeCount++;
                    return AsyncDisposeFailure is null
                        ? ValueTask.CompletedTask
                        : new ValueTask(Task.FromException(AsyncDisposeFailure));
                case "Dispose":
                    Events?.Add("Dispose");
                    DisposeCount++;
                    if (DisposeFailure is not null)
                        throw DisposeFailure;
                    return null;
                default:
                    return DefaultReturn(method.ReturnType, Context);
            }
        }
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> handler)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, ContractProxy>();
        ((ContractProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    private static object? DefaultReturn(Type returnType, object? self = null)
    {
        if (returnType == typeof(void))
            return null;
        if (self is not null && returnType.IsInstanceOfType(self))
            return self;
        if (returnType == typeof(Task))
            return Task.CompletedTask;
        if (returnType == typeof(ValueTask))
            return ValueTask.CompletedTask;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            Type resultType = returnType.GetGenericArguments()[0];
            object? result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [result]);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private class ContractProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return Handler is null
                ? DefaultReturn(targetMethod.ReturnType, this)
                : Handler(targetMethod, args);
        }
    }

    private sealed class RecordingProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public void Add(string key, string? value) => throw new NotSupportedException();
        public void Add(string key, object? value) => throw new NotSupportedException();
        public void Set(object values) => throw new NotSupportedException();
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw new NotSupportedException();
        public ProbeContext CreateScope(string key) => this;
    }

    private sealed class ThrowingLogger(Exception failure) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => throw failure;
    }

    private interface AsyncSyncSagaContext : SagaConsumeContext<TestSaga, TestMessage>, IAsyncDisposable, IDisposable
    {
    }

    private interface SyncSagaContext : SagaConsumeContext<TestSaga, TestMessage>, IDisposable
    {
    }

    private enum Ownership
    {
        AsyncAndSync,
        Sync,
    }

    public enum RepositoryAction
    {
        Undo,
        Delete,
        Update,
    }

    private sealed class TestSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class TestMessage
    {
    }
}
