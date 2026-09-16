using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyInitializers;

public sealed class PropertyInitializerContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROPERTY-INITIALIZERS", "copy-value-object-and-no-input-matrix")]
    public async Task CopyInitializers_AssignValuesAndNullsOnlyWhenInputIsAvailableAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        PropertyInfo valueProperty = typeof(TestMessage).GetProperty(nameof(TestMessage.Value))!;
        PropertyInfo objectProperty = typeof(TestMessage).GetProperty(nameof(TestMessage.ObjectValue))!;
        PropertyInfo inputValueProperty = typeof(TestInput).GetProperty(nameof(TestInput.Value))!;
        PropertyInfo inputCountProperty = typeof(TestInput).GetProperty(nameof(TestInput.Count))!;
        var copyValue = new CopyPropertyInitializer<TestMessage, TestInput, string?>(valueProperty, inputValueProperty);
        var copyObject = new CopyObjectPropertyInitializer<TestMessage, TestInput, int>(objectProperty, inputCountProperty);
        var message = new TestMessage { Value = "before", ObjectValue = "before" };
        InitializeContext<TestMessage, TestInput> context = CreateContext(message, new TestInput(null, 42));

        await copyValue.ApplyAsync(context, token);
        await copyObject.ApplyAsync(context, token);

        Assert.Null(message.Value);
        Assert.Equal(42, message.ObjectValue);

        InitializeContext<TestMessage, TestInput> noInput = CreateNoInputContext<TestMessage, TestInput>();
        await copyValue.ApplyAsync(noInput, token);
        await copyObject.ApplyAsync(noInput, token);
        Assert.Equal(2, ((NoInputContextProxy)(object)noInput).HasInputReads);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROPERTY-INITIALIZERS", "async-copy-completion-fault-and-cancellation-matrix")]
    public async Task AsyncCopyInitializer_PreservesEveryTaskCompletionStateAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var initializer = new CopyAsyncObjectPropertyInitializer<TestMessage, AsyncInput, string?>(
            typeof(TestMessage).GetProperty(nameof(TestMessage.ObjectValue))!,
            typeof(AsyncInput).GetProperty(nameof(AsyncInput.Value))!);

        var completedMessage = new TestMessage();
        await initializer.ApplyAsync(CreateContext(completedMessage, new AsyncInput(Task.FromResult<string?>("completed"))), token);
        Assert.Equal("completed", completedMessage.ObjectValue);

        var pendingMessage = new TestMessage();
        var pendingSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task pending = initializer.ApplyAsync(CreateContext(pendingMessage, new AsyncInput(pendingSource.Task)), token);
        Assert.False(pending.IsCompleted);
        pendingSource.SetResult("pending");
        await pending;
        Assert.Equal("pending", pendingMessage.ObjectValue);

        var nullMessage = new TestMessage { ObjectValue = "before" };
        await initializer.ApplyAsync(CreateContext(nullMessage, new AsyncInput(Task.FromResult<string?>(null))), token);
        Assert.Null(nullMessage.ObjectValue);

        var expected = new ExpectedInitializerException("input failed");
        var faultedMessage = new TestMessage();
        ExpectedInitializerException failure = await Assert.ThrowsAsync<ExpectedInitializerException>(() => initializer.ApplyAsync(
            CreateContext(faultedMessage, new AsyncInput(Task.FromException<string?>(expected))), token));
        Assert.Same(expected, failure);
        Assert.Null(faultedMessage.ObjectValue);

        var nullTaskMessage = new TestMessage();
        InvalidOperationException nullTask = await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.ApplyAsync(
            CreateContext(nullTaskMessage, new AsyncInput(null!)), token));
        Assert.Equal("The input task property returned null.", nullTask.Message);

        InitializeContext<TestMessage, AsyncInput> noInput = CreateNoInputContext<TestMessage, AsyncInput>();
        await initializer.ApplyAsync(noInput, token);
        Assert.Equal(1, ((NoInputContextProxy)(object)noInput).HasInputReads);

        using var cancellation = new CancellationTokenSource();
        var neverCompletes = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task canceled = initializer.ApplyAsync(
            CreateContext(new TestMessage(), new AsyncInput(neverCompletes.Task)), cancellation.Token);
        cancellation.Cancel();
        OperationCanceledException canceledFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.Equal(cancellation.Token, canceledFailure.CancellationToken);
        Assert.False(neverCompletes.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROPERTY-INITIALIZERS", "dictionary-presence-and-null-value-matrix")]
    public async Task DictionaryCopyInitializer_DistinguishesMissingEntriesFromPresentNullValuesAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var initializer = new DictionaryCopyPropertyInitializer<TestMessage, Dictionary<string, string?>, string?>(
            typeof(TestMessage).GetProperty(nameof(TestMessage.Value))!, "Value");
        var message = new TestMessage { Value = "before" };

        await initializer.ApplyAsync(CreateContext(message, new Dictionary<string, string?>()), token);
        Assert.Equal("before", message.Value);

        await initializer.ApplyAsync(CreateContext(message, new Dictionary<string, string?> { ["Value"] = null }), token);
        Assert.Null(message.Value);

        message.Value = "before";
        await initializer.ApplyAsync(CreateContext(message, new Dictionary<string, string?> { ["Value"] = "after" }), token);
        Assert.Equal("after", message.Value);

        InitializeContext<TestMessage, Dictionary<string, string?>> noInput =
            CreateNoInputContext<TestMessage, Dictionary<string, string?>>();
        await initializer.ApplyAsync(noInput, token);
        Assert.Equal(1, ((NoInputContextProxy)(object)noInput).HasInputReads);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROPERTY-INITIALIZERS", "provider-completion-null-task-and-runtime-ownership-matrix")]
    public async Task ProviderInitializer_AssignsOwnedMessagesAcrossEveryProviderTaskStateAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        PropertyInfo property = typeof(TestMessage).GetProperty(nameof(TestMessage.Value))!;
        var completedProvider = new RecordingProvider(Task.FromResult<string?>("completed"));
        var completedInitializer = new ProviderPropertyInitializer<TestMessage, TestInput, string?>(completedProvider, property);
        var completedMessage = new TestMessage();
        InitializeContext<TestMessage, TestInput> completedContext = CreateContext(completedMessage, new TestInput("input", 1));

        await completedInitializer.ApplyAsync(completedContext, token);

        Assert.Equal("completed", completedMessage.Value);
        Assert.Same(completedContext, completedProvider.Context);
        Assert.Equal(token, completedProvider.Token);

        var pendingSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingProvider = new RecordingProvider(pendingSource.Task);
        var pendingInitializer = new ProviderPropertyInitializer<TestMessage, TestInput, string?>(pendingProvider, property);
        var pendingMessage = new TestMessage();
        Task pending = pendingInitializer.ApplyAsync(CreateContext(pendingMessage, new TestInput("input", 1)), token);
        Assert.False(pending.IsCompleted);
        pendingSource.SetResult("pending");
        await pending;
        Assert.Equal("pending", pendingMessage.Value);

        var expected = new ExpectedInitializerException("provider failed");
        var faultedInitializer = new ProviderPropertyInitializer<TestMessage, TestInput, string?>(
            new RecordingProvider(Task.FromException<string?>(expected)), property);
        ExpectedInitializerException fault = await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
            faultedInitializer.ApplyAsync(CreateContext(new TestMessage(), new TestInput("input", 1)), token));
        Assert.Same(expected, fault);

        var nullTaskInitializer = new ProviderPropertyInitializer<TestMessage, TestInput, string?>(new NullTaskProvider(), property);
        InvalidOperationException nullTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullTaskInitializer.ApplyAsync(CreateContext(new TestMessage(), new TestInput("input", 1)), token));
        Assert.Equal("The property provider returned a null task.", nullTask.Message);

        var derivedMessage = new DerivedMessage { Value = "retained" };
        var mismatchProvider = new RecordingProvider(Task.FromResult<string?>("must not assign"));
        var mismatchInitializer = new ProviderPropertyInitializer<TestMessage, TestInput, string?>(mismatchProvider, property);
        InitializeContext<TestMessage> derivedMessageContext =
            new BaseInitializeContext(token).CreateMessageContext<TestMessage>(derivedMessage);
        await mismatchInitializer.ApplyAsync(derivedMessageContext.CreateInputContext(new TestInput("input", 1)), token);
        Assert.Equal("retained", derivedMessage.Value);
        Assert.Equal(1, mismatchProvider.CallCount);

        using var cancellation = new CancellationTokenSource();
        var neverCompletes = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelingProvider = new RecordingProvider(neverCompletes.Task);
        var cancelingInitializer = new ProviderPropertyInitializer<TestMessage, TestInput, string?>(cancelingProvider, property);
        Task canceled = cancelingInitializer.ApplyAsync(
            CreateContext(new TestMessage(), new TestInput("input", 1)), cancellation.Token);
        try
        {
            cancellation.Cancel();
            Assert.Equal(cancellation.Token, cancelingProvider.Token);
            await Assert.ThrowsAsync<TimeoutException>(() =>
                canceled.WaitAsync(TimeSpan.FromMilliseconds(25), TestContext.Current.CancellationToken));
            Assert.False(canceled.IsCompleted);
            Assert.False(neverCompletes.Task.IsCompleted);
            neverCompletes.SetCanceled(cancellation.Token);
            OperationCanceledException canceledFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
            Assert.Equal(cancellation.Token, canceledFailure.CancellationToken);
            Assert.True(canceled.IsCanceled);
        }
        finally
        {
            neverCompletes.TrySetResult(null);
            await ObserveAsync(neverCompletes.Task);
            await ObserveAsync(canceled);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROPERTY-INITIALIZERS", "constructor-context-and-pre-cancellation-boundaries")]
    public async Task PropertyInitializers_RejectInvalidConstructionAndCallsAtTheirOwningBoundariesAsync()
    {
        PropertyInfo valueProperty = typeof(TestMessage).GetProperty(nameof(TestMessage.Value))!;
        PropertyInfo objectProperty = typeof(TestMessage).GetProperty(nameof(TestMessage.ObjectValue))!;
        PropertyInfo inputValueProperty = typeof(TestInput).GetProperty(nameof(TestInput.Value))!;
        PropertyInfo inputCountProperty = typeof(TestInput).GetProperty(nameof(TestInput.Count))!;

        Assert.Equal("messagePropertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new CopyPropertyInitializer<TestMessage, TestInput, string?>(null!, inputValueProperty)).ParamName);
        Assert.Equal("inputPropertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new CopyPropertyInitializer<TestMessage, TestInput, string?>(valueProperty, null!)).ParamName);
        Assert.Equal("messagePropertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new CopyObjectPropertyInitializer<TestMessage, TestInput, int>(null!, inputCountProperty)).ParamName);
        Assert.Equal("inputPropertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new CopyObjectPropertyInitializer<TestMessage, TestInput, int>(objectProperty, null!)).ParamName);
        Assert.Equal("messagePropertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new CopyAsyncObjectPropertyInitializer<TestMessage, AsyncInput, string?>(null!, typeof(AsyncInput).GetProperty(nameof(AsyncInput.Value))!)).ParamName);
        Assert.Equal("inputPropertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new CopyAsyncObjectPropertyInitializer<TestMessage, AsyncInput, string?>(objectProperty, null!)).ParamName);
        Assert.Equal("propertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new DictionaryCopyPropertyInitializer<TestMessage, Dictionary<string, string?>, string?>(null!, "Value")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() =>
            new DictionaryCopyPropertyInitializer<TestMessage, Dictionary<string, string?>, string?>(valueProperty, null!)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
            new DictionaryCopyPropertyInitializer<TestMessage, Dictionary<string, string?>, string?>(valueProperty, " ")).ParamName);
        Assert.Equal("propertyProvider", Assert.Throws<ArgumentNullException>(() =>
            new ProviderPropertyInitializer<TestMessage, TestInput, string?>(null, valueProperty)).ParamName);
        Assert.Equal("propertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new ProviderPropertyInitializer<TestMessage, TestInput, string?>(new RecordingProvider(Task.FromResult<string?>(null)), null)).ParamName);

        var copy = new CopyPropertyInitializer<TestMessage, TestInput, string?>(valueProperty, inputValueProperty);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            copy.ApplyAsync(null!, TestContext.Current.CancellationToken))).ParamName);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var throwingContext = CreateThrowingContext<TestMessage, TestInput>();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            copy.ApplyAsync(throwingContext, cancellation.Token));
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
    }

    static async Task ObserveAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
        catch (Exception) when (task.IsCompleted)
        {
        }
        Assert.True(task.IsCompleted);
    }

    static InitializeContext<TMessage, TInput> CreateContext<TMessage, TInput>(TMessage message, TInput input)
        where TMessage : class
        where TInput : class =>
        new BaseInitializeContext(TestContext.Current.CancellationToken)
            .CreateMessageContext(message)
            .CreateInputContext(input);

    static InitializeContext<TMessage, TInput> CreateNoInputContext<TMessage, TInput>()
        where TMessage : class
        where TInput : class =>
        DispatchProxy.Create<InitializeContext<TMessage, TInput>, NoInputContextProxy>();

    static InitializeContext<TMessage, TInput> CreateThrowingContext<TMessage, TInput>()
        where TMessage : class
        where TInput : class =>
        DispatchProxy.Create<InitializeContext<TMessage, TInput>, ThrowingContextProxy>();

    private sealed class RecordingProvider(Task<string?> task) : IPropertyProvider<TestInput, string?>
    {
        public int CallCount { get; private set; }

        public object? Context { get; private set; }

        public CancellationToken Token { get; private set; }

        public Task<string?> GetPropertyAsync<TMessage>(InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            CallCount++;
            Context = context;
            Token = cancellationToken;
            return task;
        }
    }

    private sealed class NullTaskProvider : IPropertyProvider<TestInput, string?>
    {
        public Task<string?> GetPropertyAsync<TMessage>(InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class => null!;
    }

    private class NoInputContextProxy : DispatchProxy
    {
        public int HasInputReads { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_HasInput")
            {
                HasInputReads++;
                return false;
            }

            throw new InvalidOperationException($"{targetMethod.Name} must not be called without input.");
        }
    }

    private class ThrowingContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"{targetMethod?.Name} must not be called after cancellation.");
    }

    private record TestInput(string? Value, int Count);

    private sealed record AsyncInput(Task<string?> Value);

    private class TestMessage
    {
        public object? ObjectValue { get; set; }

        public string? Value { get; set; }
    }

    private sealed class DerivedMessage : TestMessage;

    private sealed class ExpectedInitializerException(string message) : Exception(message);
}
