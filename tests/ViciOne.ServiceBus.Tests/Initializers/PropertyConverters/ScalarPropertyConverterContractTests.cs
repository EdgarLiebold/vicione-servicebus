using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class ScalarPropertyConverterContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-ADAPTERS", "nullable-object-type-and-named-value-matrix")]
    public async Task ScalarAdapters_CoverValueNullFailureAndCancellationPartitionsAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;

        IPropertyConverter<int, int?> fromNullable = new FromNullablePropertyConverter<int>();
        Assert.Equal(42, await fromNullable.ConvertAsync(context, 42, token));
        Assert.Equal(0, await fromNullable.ConvertAsync(context, null, token));

        var recordingConverter = new RecordingConverter<long, int>(value => value * 10L);
        IPropertyConverter<long, int?> convertedFromNullable = new FromNullablePropertyConverter<long, int>(recordingConverter);
        Assert.Equal(420L, await convertedFromNullable.ConvertAsync(context, 42, token));
        Assert.Equal(0L, await convertedFromNullable.ConvertAsync(context, null, token));
        Assert.Equal([42, 0], recordingConverter.Inputs);
        Assert.Equal([token, token], recordingConverter.Tokens);

        var toNullable = new ToNullablePropertyConverter<int>();
        Assert.Equal(42, await toNullable.ConvertAsync(context, 42, token));
        var convertedToNullable = new ToNullablePropertyConverter<long, int>(new RecordingConverter<long, int>(value => value * 10L));
        Assert.Equal(420L, await convertedToNullable.ConvertAsync(context, 42, token));

        var toObject = new ToObjectPropertyConverter<string>();
        var value = new string('x', 1);
        Assert.Same(value, await toObject.ConvertAsync(context, value, token));
        Assert.Null(await toObject.ConvertAsync(context, null, token));

        var typeConverter = new TypePropertyConverter<int, string>(new ParsingTypeConverter());
        IPropertyConverter<int, string> typeAdapter = typeConverter;
        Assert.Equal(42, await typeAdapter.ConvertAsync(context, "42", token));
        Assert.Equal(0, await typeAdapter.ConvertAsync(context, "invalid", token));

        var named = new NamedInitializerValuePropertyConverter<NamedValue>();
        Assert.Equal("ready", await named.ConvertAsync(context, new NamedValue("ready"), token));
        Assert.Null(await named.ConvertAsync(context, null, token));
        var convertedNamed = new NamedInitializerValuePropertyConverter<int, NamedValue>(typeConverter);
        Assert.Equal(42, await convertedNamed.ConvertAsync(context, new NamedValue("42"), token));
        Assert.Equal(0, await convertedNamed.ConvertAsync(context, null, token));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await AssertCanceledAsync(() => fromNullable.ConvertAsync(context, null, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => toNullable.ConvertAsync(context, 42, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => convertedToNullable.ConvertAsync(context, 42, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => toObject.ConvertAsync(context, value, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => typeAdapter.ConvertAsync(context, "42", cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => named.ConvertAsync(context, null, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => convertedNamed.ConvertAsync(context, null, cancellation.Token), cancellation.Token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-ADAPTERS", "composite-null-task-boundaries")]
    public async Task CompositeScalarAdapters_RejectMissingCollaboratorTasksAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;

        IPropertyConverter<long, int?> fromNullable = new FromNullablePropertyConverter<long, int>(new NullTaskConverter<long, int>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => fromNullable.ConvertAsync(context, 42, token));
        var toNullable = new ToNullablePropertyConverter<long, int>(new NullTaskConverter<long, int>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => toNullable.ConvertAsync(context, 42, token));
        var named = new NamedInitializerValuePropertyConverter<long, NamedValue>(new NullTaskConverter<long, string>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => named.ConvertAsync(context, new NamedValue("42"), token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TASK-ADAPTERS", "completed-pending-fault-cancellation-and-conversion-matrix")]
    public async Task TaskAdapters_PreserveEverySourceAndConversionStateAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;
        var direct = new TaskPropertyConverter<int>();
        IPropertyConverter<Task<int>, int> wrapDirect = direct;
        IPropertyConverter<int, Task<int>> awaitDirect = direct;

        Task<int>? wrapped = await wrapDirect.ConvertAsync(context, 42, token);
        Assert.NotNull(wrapped);
        Assert.Equal(42, await wrapped);
        Assert.Equal(42, await awaitDirect.ConvertAsync(context, Task.FromResult(42), token));
        Assert.Equal(0, await awaitDirect.ConvertAsync(context, null, token));

        var pendingSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> pendingResult = awaitDirect.ConvertAsync(context, pendingSource.Task, token);
        Assert.False(pendingResult.IsCompleted);
        pendingSource.SetResult(43);
        Assert.Equal(43, await pendingResult);

        var expected = new ExpectedConverterException("source failed");
        ExpectedConverterException sourceFailure = await Assert.ThrowsAsync<ExpectedConverterException>(() =>
            awaitDirect.ConvertAsync(context, Task.FromException<int>(expected), token));
        Assert.Same(expected, sourceFailure);

        using var sourceCancellation = new CancellationTokenSource();
        sourceCancellation.Cancel();
        await AssertCanceledAsync(() => awaitDirect.ConvertAsync(
            context, Task.FromCanceled<int>(sourceCancellation.Token), token), sourceCancellation.Token);

        var innerConverter = new RecordingConverter<long, int>(value => value * 10L);
        var converted = new TaskPropertyConverter<long, int>(innerConverter);
        IPropertyConverter<Task<long>, int> wrapConverted = converted;
        IPropertyConverter<long, Task<int>> awaitConverted = converted;
        Task<long>? convertedTask = await wrapConverted.ConvertAsync(context, 5, token);
        Assert.NotNull(convertedTask);
        Assert.Equal(50L, await convertedTask);
        Assert.Equal(60L, await awaitConverted.ConvertAsync(context, Task.FromResult(6), token));
        Assert.Equal(0L, await awaitConverted.ConvertAsync(context, null, token));

        var pendingInput = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingConverter = new PendingConverter<long, int>();
        IPropertyConverter<long, Task<int>> pendingAdapter = new TaskPropertyConverter<long, int>(pendingConverter);
        Task<long> pendingConversion = pendingAdapter.ConvertAsync(context, pendingInput.Task, token);
        pendingInput.SetResult(7);
        await pendingConverter.Started.Task.WaitAsync(token);
        Assert.False(pendingConversion.IsCompleted);
        pendingConverter.Complete(70L);
        Assert.Equal(70L, await pendingConversion);

        using var callerCancellation = new CancellationTokenSource();
        var neverCompletes = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> canceledByCaller = awaitDirect.ConvertAsync(context, neverCompletes.Task, callerCancellation.Token);
        callerCancellation.Cancel();
        await AssertCanceledAsync(() => canceledByCaller, callerCancellation.Token);

        using var preCancellation = new CancellationTokenSource();
        preCancellation.Cancel();
        await AssertCanceledAsync(() => wrapDirect.ConvertAsync(context, 8, preCancellation.Token), preCancellation.Token);
        await AssertCanceledAsync(() => awaitDirect.ConvertAsync(context, Task.FromResult(8), preCancellation.Token), preCancellation.Token);
        await AssertCanceledAsync(() => wrapConverted.ConvertAsync(context, 8, preCancellation.Token), preCancellation.Token);
        await AssertCanceledAsync(() => awaitConverted.ConvertAsync(context, Task.FromResult(8), preCancellation.Token), preCancellation.Token);

        var nullTaskConverter = new TaskPropertyConverter<long, int>(new NullTaskConverter<long, int>());
        IPropertyConverter<Task<long>, int> wrapWithNullConverter = nullTaskConverter;
        IPropertyConverter<long, Task<int>> awaitWithNullConverter = nullTaskConverter;
        await Assert.ThrowsAsync<InvalidOperationException>(() => wrapWithNullConverter.ConvertAsync(context, 9, token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            awaitWithNullConverter.ConvertAsync(context, Task.FromResult(9), token));

        var pendingNullSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<long> pendingNullConversion = awaitWithNullConverter.ConvertAsync(context, pendingNullSource.Task, token);
        pendingNullSource.SetResult(10);
        await Assert.ThrowsAsync<InvalidOperationException>(() => pendingNullConversion);

        var pendingCompletedSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<long> pendingCompletedConversion = awaitConverted.ConvertAsync(context, pendingCompletedSource.Task, token);
        pendingCompletedSource.SetResult(11);
        Assert.Equal(110L, await pendingCompletedConversion);
    }

    static async Task AssertCanceledAsync(Func<Task> operation, CancellationToken expectedToken)
    {
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(operation);
        Assert.Equal(expectedToken, exception.CancellationToken);
    }

    static InitializeContext<TestMessage> CreateContext() =>
        new BaseInitializeContext(TestContext.Current.CancellationToken).CreateMessageContext(new TestMessage());

    private sealed class ParsingTypeConverter : ITypeConverter<int, string>
    {
        public bool TryConvert(string? input, out int result) => int.TryParse(input, out result);
    }

    private sealed class RecordingConverter<TResult, TInput>(Func<TInput, TResult> convert) : IPropertyConverter<TResult, TInput>
    {
        public List<TInput?> Inputs { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Inputs.Add(input);
            Tokens.Add(cancellationToken);
            return Task.FromResult<TResult?>(convert(input!));
        }
    }

    private sealed class PendingConverter<TResult, TInput> : IPropertyConverter<TResult, TInput>
    {
        readonly TaskCompletionSource<TResult?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Started.TrySetResult();
            return _completion.Task;
        }

        public void Complete(TResult result) => _completion.TrySetResult(result);
    }

    private sealed class NullTaskConverter<TResult, TInput> : IPropertyConverter<TResult, TInput>
    {
        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => null!;
    }

    private sealed record NamedValue(string Name) : INamedInitializerValue;

    private sealed class TestMessage;

    private sealed class ExpectedConverterException(string message) : Exception(message);
}
