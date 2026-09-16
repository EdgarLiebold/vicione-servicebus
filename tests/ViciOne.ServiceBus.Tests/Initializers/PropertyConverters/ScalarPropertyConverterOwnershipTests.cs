using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class ScalarPropertyConverterOwnershipTests
{
    public static IEnumerable<object[]> StagesAndOutcomes()
    {
        for (var stage = 0; stage < 2; stage++)
            for (var outcome = 0; outcome < 3; outcome++)
                yield return [stage, outcome];
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-LIFETIME", "direct-variable-owns-accepted-value-task")]
    public async Task DirectVariableConverter_OwnsAcceptedValueOutcomeAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = CanceledSource();
        var source = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var variable = new ControlledVariable<long>(source.Task);
        var converter = new VariablePropertyConverter<long, ControlledVariable<long>>();
        InitializeContext<OwnerMessage> context = CreateContext();
        var root = converter.ConvertAsync(context, variable, caller.Token);
        var expected = new ExpectedConverterException("variable failed");
        try
        {
            Assert.Equal(1, variable.Calls);
            Assert.Same(context, variable.Contexts.Single());
            Assert.Equal(caller.Token, variable.Tokens.Single());
            caller.Cancel();
            await AssertPendingAsync(root);
            Complete(source, outcome, 41L, expected, producerCancellation.Token);
            long? result = await AssertOutcomeAsync(root, outcome, expected, producerCancellation.Token);
            if (outcome == 0)
                Assert.Equal(41L, result);
        }
        finally
        {
            source.TrySetResult(41L);
            await ObserveAsync(source.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(StagesAndOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-LIFETIME", "converted-variable-owns-both-accepted-stages")]
    public async Task ConvertedVariableConverter_OwnsEachAcceptedStageAsync(int stage, int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = CanceledSource();
        var variableSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var conversionSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (stage == 1)
            variableSource.SetResult(7);
        var variable = new ControlledVariable<int>(variableSource.Task);
        var inner = new ControlledConverter<string, int>(_ => stage == 0
            ? Task.FromResult<string?>("seven")
            : conversionSource.Task);
        var converter = new VariablePropertyConverter<string, ControlledVariable<int>, int>(inner);
        InitializeContext<OwnerMessage> context = CreateContext();
        var root = converter.ConvertAsync(context, variable, caller.Token);
        var expected = new ExpectedConverterException(stage == 0 ? "variable failed" : "conversion failed");
        try
        {
            Assert.Equal(1, variable.Calls);
            Assert.Same(context, variable.Contexts.Single());
            Assert.Equal(caller.Token, variable.Tokens.Single());
            Assert.Equal(stage, inner.Calls);
            if (stage == 1)
            {
                Assert.Same(context, inner.Contexts.Single());
                Assert.Equal(7, inner.Inputs.Single());
                Assert.Equal(caller.Token, inner.Tokens.Single());
            }
            caller.Cancel();
            await AssertPendingAsync(root);
            if (stage == 0)
                Complete(variableSource, outcome, 7, expected, producerCancellation.Token);
            else
                Complete(conversionSource, outcome, "seven", expected, producerCancellation.Token);
            string? result = await AssertOutcomeAsync(root, outcome, expected, producerCancellation.Token);
            if (outcome == 0)
            {
                Assert.Equal("seven", result);
                Assert.Equal(1, inner.Calls);
                Assert.Same(context, inner.Contexts.Single());
                Assert.Equal(7, inner.Inputs.Single());
                Assert.Equal(caller.Token, inner.Tokens.Single());
            }
            else if (stage == 0)
                Assert.Equal(0, inner.Calls);
        }
        finally
        {
            variableSource.TrySetResult(7);
            conversionSource.TrySetResult("seven");
            await ObserveAsync(variableSource.Task);
            await ObserveAsync(conversionSource.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-LIFETIME", "nullable-wrapper-owns-accepted-conversion")]
    public async Task ToNullableConverter_OwnsAcceptedConversionOutcomeAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = CanceledSource();
        var source = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new ControlledConverter<long, int>(_ => source.Task);
        var converter = new ToNullablePropertyConverter<long, int>(inner);
        InitializeContext<OwnerMessage> context = CreateContext();
        var root = converter.ConvertAsync(context, 7, caller.Token);
        var expected = new ExpectedConverterException("nullable conversion failed");
        try
        {
            Assert.Equal(1, inner.Calls);
            Assert.Same(context, inner.Contexts.Single());
            Assert.Equal(7, inner.Inputs.Single());
            Assert.Equal(caller.Token, inner.Tokens.Single());
            caller.Cancel();
            await AssertPendingAsync(root);
            Complete(source, outcome, 70L, expected, producerCancellation.Token);
            long? result = await AssertOutcomeAsync(root, outcome, expected, producerCancellation.Token);
            if (outcome == 0)
                Assert.Equal(70L, result);
        }
        finally
        {
            source.TrySetResult(70L);
            await ObserveAsync(source.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TASK-LIFETIME", "accepted-downstream-conversion-is-owned")]
    public async Task ConvertingTaskAdapter_OwnsAcceptedDownstreamOutcomeAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = CanceledSource();
        var input = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var conversion = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new ControlledConverter<long, int>(_ => conversion.Task);
        var converter = (IPropertyConverter<long, Task<int>>)new TaskPropertyConverter<long, int>(inner);
        InitializeContext<OwnerMessage> context = CreateContext();
        var root = converter.ConvertAsync(context, input.Task, caller.Token);
        var expected = new ExpectedConverterException("task conversion failed");
        try
        {
            Assert.Equal(0, inner.Calls);
            input.SetResult(8);
            await inner.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, inner.Calls);
            Assert.Same(context, inner.Contexts.Single());
            Assert.Equal(8, inner.Inputs.Single());
            Assert.Equal(caller.Token, inner.Tokens.Single());
            caller.Cancel();
            await AssertPendingAsync(root);
            Complete(conversion, outcome, 80L, expected, producerCancellation.Token);
            long? result = await AssertOutcomeAsync(root, outcome, expected, producerCancellation.Token);
            if (outcome == 0)
                Assert.Equal(80L, result);
        }
        finally
        {
            input.TrySetResult(8);
            conversion.TrySetResult(80L);
            await ObserveAsync(input.Task);
            await ObserveAsync(conversion.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TASK-LIFETIME", "task-input-remains-caller-owned")]
    public async Task TaskInputs_RemainCallerOwnedAndLocallyCancellableAsync(int form)
    {
        using var caller = new CancellationTokenSource();
        var input = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new ControlledConverter<long, int>(_ => Task.FromResult(80L));
        InitializeContext<OwnerMessage> context = CreateContext();
        Task root = form == 0
            ? ((IPropertyConverter<int, Task<int>>)new TaskPropertyConverter<int>()).ConvertAsync(context, input.Task, caller.Token)
            : ((IPropertyConverter<long, Task<int>>)new TaskPropertyConverter<long, int>(inner)).ConvertAsync(context, input.Task, caller.Token);
        try
        {
            Assert.False(root.IsCompleted);
            caller.Cancel();
            Task completed = await Task.WhenAny(root, Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
            Assert.Same(root, completed);
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => root);
            Assert.Equal(caller.Token, actual.CancellationToken);
            Assert.True(root.IsCanceled);
            Assert.False(input.Task.IsCompleted);
            Assert.Equal(0, inner.Calls);
        }
        finally
        {
            input.TrySetResult(8);
            await ObserveAsync(input.Task);
            await ObserveAsync(root);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TASK-LIFETIME", "message-data-value-remains-caller-owned")]
    public async Task MessageDataValueInput_RemainsCallerOwnedAndLocallyCancellableAsync()
    {
        using var caller = new CancellationTokenSource();
        var value = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var input = new ControlledMessageData(value.Task);
        var converter = (IPropertyConverter<MessageData<byte[]>, MessageData<string>>)MessageDataPropertyConverter.Instance;
        var root = converter.ConvertAsync(CreateContext(), input, caller.Token);
        try
        {
            Assert.Equal(1, input.ValueReads);
            Assert.False(root.IsCompleted);
            caller.Cancel();
            Task completed = await Task.WhenAny(root, Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
            Assert.Same(root, completed);
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => root);
            Assert.Equal(caller.Token, actual.CancellationToken);
            Assert.True(root.IsCanceled);
            Assert.False(value.Task.IsCompleted);
            Assert.Equal(1, input.ValueReads);
        }
        finally
        {
            value.TrySetResult("released");
            await ObserveAsync(value.Task);
            await ObserveAsync(root);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TASK-LIFETIME", "task-wrapping-preserves-conversion-task-identity")]
    public async Task TaskWrapping_PreservesTheAcceptedConversionTaskIdentityAsync()
    {
        var conversion = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new ControlledConverter<long, int>(_ => conversion.Task);
        var converter = (IPropertyConverter<Task<long>, int>)new TaskPropertyConverter<long, int>(inner);
        InitializeContext<OwnerMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;

        Task<Task<long>?> root = converter.ConvertAsync(context, 9, token);
        Task<long>? result = await root;
        Task<long> ownedResult = Assert.IsType<Task<long>>(result);

        Assert.True(root.IsCompletedSuccessfully);
        Assert.Same(conversion.Task, ownedResult);
        Assert.Equal(1, inner.Calls);
        Assert.Same(context, inner.Contexts.Single());
        Assert.Equal(9, inner.Inputs.Single());
        Assert.Equal(token, inner.Tokens.Single());

        conversion.SetResult(90L);
        Assert.Equal(90L, await ownedResult);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-LIFETIME", "dependency-and-null-task-guards")]
    public async Task ScalarOwnershipConverters_RejectMissingDependenciesAndAcceptedTasksAsync()
    {
        Assert.Equal("propertyConverter", Assert.Throws<ArgumentNullException>(() =>
            new VariablePropertyConverter<string, ControlledVariable<int>, int>(null!)).ParamName);
        Assert.Equal("converter", Assert.Throws<ArgumentNullException>(() =>
            new ToNullablePropertyConverter<long, int>(null!)).ParamName);
        Assert.Equal("converter", Assert.Throws<ArgumentNullException>(() =>
            new TaskPropertyConverter<long, int>(null!)).ParamName);

        InitializeContext<OwnerMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;
        var missingVariableTask = new NullTaskVariable<int>();
        var directVariable = new VariablePropertyConverter<int, NullTaskVariable<int>>();
        var convertedVariable = new VariablePropertyConverter<string, NullTaskVariable<int>, int>(
            new ControlledConverter<string, int>(_ => Task.FromResult<string?>("value")));
        var completedVariable = new ControlledVariable<int>(Task.FromResult(7));
        var missingConversion = new NullTaskConverter<string, int>();

        InvalidOperationException directVariableFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directVariable.ConvertAsync(context, missingVariableTask, token));
        InvalidOperationException convertedVariableFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            convertedVariable.ConvertAsync(context, missingVariableTask, token));
        InvalidOperationException variableConversionFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new VariablePropertyConverter<string, ControlledVariable<int>, int>(missingConversion)
                .ConvertAsync(context, completedVariable, token));
        InvalidOperationException nullableFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ToNullablePropertyConverter<long, int>(new NullTaskConverter<long, int>())
                .ConvertAsync(context, 7, token));
        InvalidOperationException taskFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((IPropertyConverter<long, Task<int>>)new TaskPropertyConverter<long, int>(new NullTaskConverter<long, int>()))
            .ConvertAsync(context, Task.FromResult(7), token));

        Assert.Equal("The initializer variable returned null.", directVariableFailure.Message);
        Assert.Equal("The initializer variable returned null.", convertedVariableFailure.Message);
        Assert.Equal("The property converter returned null.", variableConversionFailure.Message);
        Assert.Equal("The property converter returned null.", nullableFailure.Message);
        Assert.Equal("The property converter returned a null task.", taskFailure.Message);
    }

    static InitializeContext<OwnerMessage> CreateContext()
    {
        var context = new BaseInitializeContext(TestContext.Current.CancellationToken);
        return context.CreateMessageContext(new OwnerMessage());
    }

    static CancellationTokenSource CanceledSource()
    {
        var source = new CancellationTokenSource();
        source.Cancel();
        return source;
    }

    static void Complete<T>(TaskCompletionSource<T> source, int outcome, T value, Exception expected, CancellationToken cancellationToken)
    {
        if (outcome == 0)
            source.SetResult(value);
        else if (outcome == 1)
            source.SetException(expected);
        else
            source.SetCanceled(cancellationToken);
    }

    static async Task<T?> AssertOutcomeAsync<T>(Task<T> task, int outcome, Exception expected, CancellationToken cancellationToken)
    {
        if (outcome == 0)
            return await task;

        if (outcome == 1)
        {
            Assert.Same(expected, await Assert.ThrowsAsync(expected.GetType(), () => task));
            Assert.True(task.IsFaulted);
        }
        else
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(cancellationToken, actual.CancellationToken);
            Assert.True(task.IsCanceled);
        }

        return default;
    }

    static async Task AssertPendingAsync(Task task)
    {
        await Assert.ThrowsAsync<TimeoutException>(() => task.WaitAsync(TimeSpan.FromMilliseconds(50)));
        await Assert.ThrowsAsync<TimeoutException>(() => task.WaitAsync(TimeSpan.FromMilliseconds(50)));
        Assert.False(task.IsCompleted);
    }

    static async Task ObserveAsync(Task? task)
    {
        if (task == null)
            return;

        try
        {
            await task;
        }
        catch (Exception) when (task.IsCompleted)
        {
        }
    }

    private sealed class ControlledVariable<TValue>(Task<TValue> task) : IInitializerVariable<TValue>
    {
        public int Calls { get; private set; }

        public List<object> Contexts { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public Task<TValue> GetValueAsync<TMessage>(InitializeContext<TMessage> context, CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Calls++;
            Contexts.Add(context);
            Tokens.Add(cancellationToken);
            return task;
        }
    }

    private sealed class NullTaskVariable<TValue> : IInitializerVariable<TValue>
    {
        public Task<TValue> GetValueAsync<TMessage>(InitializeContext<TMessage> context, CancellationToken cancellationToken = default)
            where TMessage : class => null!;
    }

    private sealed class ControlledConverter<TResult, TInput>(Func<TInput?, Task<TResult?>> body) : IPropertyConverter<TResult, TInput>
    {
        public int Calls { get; private set; }

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<TInput?> Inputs { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public List<object> Contexts { get; } = [];

        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Calls++;
            Inputs.Add(input);
            Tokens.Add(cancellationToken);
            Contexts.Add(context);
            Started.TrySetResult();
            return body(input);
        }
    }

    private sealed class NullTaskConverter<TResult, TInput> : IPropertyConverter<TResult, TInput>
    {
        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => null!;
    }

    private sealed class ControlledMessageData(Task<string?> value) : MessageData<string>
    {
        public Uri? Address => new("urn:message-data:owned-input");

        public bool HasValue => true;

        public int ValueReads { get; private set; }

        public Task<string?> Value
        {
            get
            {
                ValueReads++;
                return value;
            }
        }
    }

    public sealed class OwnerMessage;

    private sealed class ExpectedConverterException(string message) : Exception(message);
}
