using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga.InMemoryRepository;

public sealed class InMemorySagaConsumeContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "consume-context-disposal-releases-saga-exactly-once")]
    public async Task RepeatedAndConcurrentDispose_ReleaseOnlyTheConsumeContextsSagaLeaseAsync(bool concurrent)
    {
        var instance = new SagaInstance<ContextState>(new ContextState());
        await instance.MarkInUseAsync(TestContext.Current.CancellationToken);
        var context = new InMemorySagaConsumeContext<ContextState, ContextMessage>(CreateConsumeContext(), instance);

        Exception? disposalError = await Record.ExceptionAsync(async () =>
        {
            if (concurrent)
                await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(context.Dispose, TestContext.Current.CancellationToken)));
            else
            {
                context.Dispose();
                context.Dispose();
            }
        });
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        await instance.MarkInUseAsync(cancellation.Token);
        Exception? laterDisposalError = Record.Exception(context.Dispose);
        Exception? releaseError = Record.Exception(instance.Release);

        Assert.Null(disposalError);
        Assert.Null(laterDisposalError);
        Assert.Null(releaseError);
        Assert.Same(instance.Instance, context.Saga);
        Assert.Equal(instance.Instance.CorrelationId, context.CorrelationId);
    }

    [Theory]
    [InlineData("instance")]
    [InlineData("default-instance")]
    [InlineData("default-context")]
    [InlineData("saga")]
    [InlineData("context")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "saga-and-consume-context-construction-require-state")]
    public void RequiredConstructionInput_IsRejectedWithItsExactParameterName(string input)
    {
        ConsumeContext<ContextMessage> consumeContext = CreateConsumeContext();
        var instance = new SagaInstance<ContextState>(new ContextState());
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            switch (input)
            {
                case "instance": _ = new SagaInstance<ContextState>(null!); break;
                case "default-instance": _ = new DefaultSagaConsumeContext<ContextState, ContextMessage>(consumeContext, null!); break;
                case "default-context": _ = new DefaultSagaConsumeContext<ContextState, ContextMessage>(null!, instance.Instance); break;
                case "saga": _ = new InMemorySagaConsumeContext<ContextState, ContextMessage>(consumeContext, null!); break;
                case "context": _ = new InMemorySagaConsumeContext<ContextState, ContextMessage>(null!, instance); break;
                default: throw new ArgumentOutOfRangeException(nameof(input));
            }
        });

        Assert.Equal(input.StartsWith("default-", StringComparison.Ordinal) ? input[8..] : input, exception.ParamName);
        Assert.False(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "completion-cancellation-preserves-state-before-success")]
    public async Task Completion_CancelledExplicitTokenDoesNotCompleteButALaterSuccessfulCallDoesAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var state = new ContextState();
        var context = new DefaultSagaConsumeContext<ContextState, ContextMessage>(CreateConsumeContext(), state);
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.SetCompletedAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.False(context.IsCompleted);
        await context.SetCompletedAsync(TestContext.Current.CancellationToken);
        await context.SetCompletedAsync(TestContext.Current.CancellationToken);
        Assert.True(context.IsCompleted);
        Assert.Same(state, context.Saga);
    }

    private static ConsumeContext<ContextMessage> CreateConsumeContext() =>
        InMemoryOutboxTestContextFactory.Create(new ContextMessage(), TestContext.Current.CancellationToken);

    private sealed class ContextState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    public sealed record ContextMessage;
}
