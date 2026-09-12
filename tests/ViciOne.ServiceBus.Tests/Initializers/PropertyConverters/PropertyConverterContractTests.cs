using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class PropertyConverterContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVERTERS", "collection-element-cancellation-token")]
    public async Task CollectionConverters_ForwardTheCallerTokenToEveryElementConverterAsync()
    {
        using var cancellation = new CancellationTokenSource();
        InitializeContext<TestMessage> context = CreateContext();
        var arrayElement = new RecordingConverter<long, int>();
        var listElement = new RecordingConverter<long, int>();
        var dictionaryElement = new RecordingConverter<long, int>();
        var dictionaryKey = new RecordingConverter<long, int>();
        var combinedKey = new RecordingConverter<long, int>();
        var combinedElement = new RecordingConverter<string, int>();

        await new ArrayPropertyConverter<long, int>(arrayElement)
            .ConvertAsync(context, [1], cancellation.Token);
        await new ListPropertyConverter<long, int>(listElement)
            .ConvertAsync(context, [2], cancellation.Token);
        await new DictionaryPropertyConverter<int, long, int>(dictionaryElement)
            .ConvertAsync(context, [new KeyValuePair<int, int>(3, 4)], cancellation.Token);
        await new DictionaryKeyPropertyConverter<long, int, string>(dictionaryKey)
            .ConvertAsync(context, [new KeyValuePair<int, string>(5, "value")], cancellation.Token);
        await new DictionaryPropertyConverter<long, string, int, int>(combinedKey, combinedElement)
            .ConvertAsync(context, [new KeyValuePair<int, int>(6, 7)], cancellation.Token);

        Assert.Equal([cancellation.Token], arrayElement.Tokens);
        Assert.Equal([cancellation.Token], listElement.Tokens);
        Assert.Equal([cancellation.Token], dictionaryElement.Tokens);
        Assert.Equal([cancellation.Token], dictionaryKey.Tokens);
        Assert.Equal([cancellation.Token], combinedKey.Tokens);
        Assert.Equal([cancellation.Token], combinedElement.Tokens);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVERTERS", "completed-element-fault")]
    public async Task CompletedElementFault_IsPropagatedWithoutAnAggregateWrapperAsync()
    {
        var expected = new ExpectedConverterException("element failed");
        var converter = new ArrayPropertyConverter<long, int>(new FaultedConverter<long, int>(expected));

        ExpectedConverterException actual = await Assert.ThrowsAsync<ExpectedConverterException>(() =>
            converter.ConvertAsync(CreateContext(), [1], TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVERTERS", "pending-task-cancellation")]
    public async Task PendingTaskConversion_ObservesCancellationBeforeTheSourceCompletesAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        IPropertyConverter<int, Task<int>> converter = new TaskPropertyConverter<int>();

        Task<int> conversion = converter.ConvertAsync(CreateContext(), source.Task, cancellation.Token);
        cancellation.Cancel();
        source.TrySetResult(27);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => conversion);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVERTERS", "composite-converters-reject-null-collaborator-tasks")]
    public async Task CompositeConverters_RejectNullTasksReturnedByCollaboratorsAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var nullElement = new NullTaskConverter<long, int>();
        var nullKey = new NullTaskConverter<long, int>();
        Func<Task>[] operations =
        [
            async () => await new ArrayPropertyConverter<long, int>(nullElement).ConvertAsync(context, [1], cancellationToken),
            async () => await new ListPropertyConverter<long, int>(nullElement).ConvertAsync(context, [1], cancellationToken),
            async () => await new DictionaryPropertyConverter<int, long, int>(nullElement)
                .ConvertAsync(context, [new KeyValuePair<int, int>(1, 2)], cancellationToken),
            async () => await new DictionaryKeyPropertyConverter<long, int, string>(nullKey)
                .ConvertAsync(context, [new KeyValuePair<int, string>(1, "value")], cancellationToken),
            async () => await new DictionaryPropertyConverter<long, long, int, int>(nullKey, nullElement)
                .ConvertAsync(context, [new KeyValuePair<int, int>(1, 2)], cancellationToken),
            async () => await ((IPropertyConverter<Task<long>, int>)new TaskPropertyConverter<long, int>(nullElement))
                .ConvertAsync(context, 1, cancellationToken),
            async () => await ((IPropertyConverter<long, Task<int>>)new TaskPropertyConverter<long, int>(nullElement))
                .ConvertAsync(context, Task.FromResult(1), cancellationToken),
        ];

        foreach (Func<Task> operation in operations)
            await Assert.ThrowsAsync<InvalidOperationException>(operation);
    }

    private static InitializeContext<TestMessage> CreateContext()
    {
        var context = new BaseInitializeContext(TestContext.Current.CancellationToken);
        return context.CreateMessageContext(new TestMessage());
    }

    private sealed class RecordingConverter<TResult, TInput> : IPropertyConverter<TResult, TInput>
    {
        public List<CancellationToken> Tokens { get; } = [];

        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult<TResult?>(default);
        }
    }

    private sealed class FaultedConverter<TResult, TInput>(Exception exception) : IPropertyConverter<TResult, TInput>
    {
        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.FromException<TResult?>(exception);
    }

    private sealed class NullTaskConverter<TResult, TInput> : IPropertyConverter<TResult, TInput>
    {
        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => null!;
    }

    private sealed class TestMessage;

    private sealed class ExpectedConverterException(string message) : Exception(message);
}
