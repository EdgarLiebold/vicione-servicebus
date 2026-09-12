using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.HeaderInitializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.HeaderInitializers;

public sealed class HeaderInitializerContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-HEADERS", "property-copy-value-null-and-no-input-matrix")]
    public async Task PropertyHeaderCopies_AssignValuesAndRespectAbsentInputsAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var correlationId = Guid.NewGuid();
        var copy = new CopyHeaderInitializer<TestMessage, HeaderInput, Guid?>(
            typeof(SendContext).GetProperty(nameof(SendContext.CorrelationId))!,
            typeof(HeaderInput).GetProperty(nameof(HeaderInput.CorrelationId))!);
        var setString = new SetStringHeaderInitializer<TestMessage, HeaderInput>(
            "application", typeof(HeaderInput).GetProperty(nameof(HeaderInput.Application))!);
        var sendContext = new MessageSendContext<TestMessage>(new TestMessage(), token);
        InitializeContext<TestMessage, HeaderInput> context = CreateContext(new HeaderInput(correlationId, "north"));

        await copy.ApplyAsync(context, sendContext, token);
        await setString.ApplyAsync(context, sendContext, token);

        Assert.Equal(correlationId, sendContext.CorrelationId);
        Assert.Equal("north", sendContext.Headers.Get<string>("application"));

        var nullContext = CreateContext(new HeaderInput(null, null));
        await copy.ApplyAsync(nullContext, sendContext, token);
        await setString.ApplyAsync(nullContext, sendContext, token);
        Assert.Null(sendContext.CorrelationId);
        Assert.False(sendContext.Headers.TryGetHeader("application", out _));

        sendContext.CorrelationId = correlationId;
        sendContext.Headers.Set("application", "retained");
        InitializeContext<TestMessage, HeaderInput> noInput = CreateNoInputContext<TestMessage, HeaderInput>();
        await copy.ApplyAsync(noInput, sendContext, token);
        await setString.ApplyAsync(noInput, sendContext, token);
        Assert.Equal(correlationId, sendContext.CorrelationId);
        Assert.Equal("retained", sendContext.Headers.Get<string>("application"));
        Assert.Equal(2, ((NoInputContextProxy)(object)noInput).HasInputReads);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-HEADERS", "dictionary-presence-null-and-no-input-matrix")]
    public async Task DictionaryHeaderCopy_DistinguishesMissingPresentNullAndAbsentInputAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var initial = Guid.NewGuid();
        var replacement = Guid.NewGuid();
        var initializer = new DictionaryCopyHeaderInitializer<TestMessage, Dictionary<string, Guid?>, Guid?>(
            typeof(SendContext).GetProperty(nameof(SendContext.CorrelationId))!, "CorrelationId");
        var sendContext = new MessageSendContext<TestMessage>(new TestMessage(), token) { CorrelationId = initial };

        await initializer.ApplyAsync(CreateContext(new Dictionary<string, Guid?>()), sendContext, token);
        Assert.Equal(initial, sendContext.CorrelationId);
        await initializer.ApplyAsync(CreateContext(new Dictionary<string, Guid?> { ["CorrelationId"] = null }), sendContext, token);
        Assert.Null(sendContext.CorrelationId);
        await initializer.ApplyAsync(CreateContext(new Dictionary<string, Guid?> { ["CorrelationId"] = replacement }), sendContext, token);
        Assert.Equal(replacement, sendContext.CorrelationId);

        InitializeContext<TestMessage, Dictionary<string, Guid?>> noInput =
            CreateNoInputContext<TestMessage, Dictionary<string, Guid?>>();
        await initializer.ApplyAsync(noInput, sendContext, token);
        Assert.Equal(replacement, sendContext.CorrelationId);
        Assert.Equal(1, ((NoInputContextProxy)(object)noInput).HasInputReads);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-HEADERS", "provider-standard-and-named-completion-matrix")]
    public async Task ProviderHeaders_PreserveCompletedPendingFaultAndNullTaskStatesAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        InitializeContext<TestMessage, HeaderInput> context = CreateContext(new HeaderInput(null, null));
        var standardProvider = new RecordingProvider<Guid?>(Task.FromResult<Guid?>(Guid.NewGuid()));
        var standard = new ProviderHeaderInitializer<TestMessage, HeaderInput, Guid?>(
            standardProvider, typeof(SendContext).GetProperty(nameof(SendContext.CorrelationId))!);
        var sendContext = new MessageSendContext<TestMessage>(new TestMessage(), token);

        await standard.ApplyAsync(context, sendContext, token);

        Assert.Equal(await standardProvider.Task, sendContext.CorrelationId);
        Assert.Same(context, standardProvider.Context);
        Assert.Equal(token, standardProvider.Token);

        var pendingSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var named = new SetHeaderInitializer<TestMessage, HeaderInput, string?>(
            "tenant", new RecordingProvider<string?>(pendingSource.Task));
        Task pending = named.ApplyAsync(context, sendContext, token);
        Assert.False(pending.IsCompleted);
        pendingSource.SetResult("west");
        await pending;
        Assert.Equal("west", sendContext.Headers.Get<string>("tenant"));

        var expected = new ExpectedHeaderException("header failed");
        var faulted = new ProviderHeaderInitializer<TestMessage, HeaderInput, Guid?>(
            new RecordingProvider<Guid?>(Task.FromException<Guid?>(expected)),
            typeof(SendContext).GetProperty(nameof(SendContext.CorrelationId))!);
        ExpectedHeaderException failure = await Assert.ThrowsAsync<ExpectedHeaderException>(() =>
            faulted.ApplyAsync(context, sendContext, token));
        Assert.Same(expected, failure);

        var nullTask = new SetHeaderInitializer<TestMessage, HeaderInput, string?>("invalid", new NullTaskProvider<string?>());
        InvalidOperationException missingTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullTask.ApplyAsync(context, sendContext, token));
        Assert.Equal("The property provider returned a null task.", missingTask.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-HEADERS", "constructor-and-call-boundary-matrix")]
    public async Task HeaderInitializers_ValidateConstructionContextDestinationAndCancellationInOrderAsync()
    {
        PropertyInfo correlationProperty = typeof(SendContext).GetProperty(nameof(SendContext.CorrelationId))!;
        PropertyInfo inputCorrelationProperty = typeof(HeaderInput).GetProperty(nameof(HeaderInput.CorrelationId))!;
        PropertyInfo inputApplicationProperty = typeof(HeaderInput).GetProperty(nameof(HeaderInput.Application))!;

        Assert.Equal("headerPropertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new CopyHeaderInitializer<TestMessage, HeaderInput, Guid?>(null!, inputCorrelationProperty)).ParamName);
        Assert.Equal("inputPropertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new CopyHeaderInitializer<TestMessage, HeaderInput, Guid?>(correlationProperty, null!)).ParamName);
        Assert.Equal("propertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new DictionaryCopyHeaderInitializer<TestMessage, Dictionary<string, Guid?>, Guid?>(null!, "CorrelationId")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
            new DictionaryCopyHeaderInitializer<TestMessage, Dictionary<string, Guid?>, Guid?>(correlationProperty, " ")).ParamName);
        Assert.Equal("propertyProvider", Assert.Throws<ArgumentNullException>(() =>
            new ProviderHeaderInitializer<TestMessage, HeaderInput, Guid?>(null!, correlationProperty)).ParamName);
        Assert.Equal("propertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new ProviderHeaderInitializer<TestMessage, HeaderInput, Guid?>(new NullTaskProvider<Guid?>(), null!)).ParamName);
        Assert.Equal("headerName", Assert.Throws<ArgumentException>(() =>
            new SetHeaderInitializer<TestMessage, HeaderInput, string?>(" ", new NullTaskProvider<string?>())).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new SetHeaderInitializer<TestMessage, HeaderInput, string?>("tenant", null!)).ParamName);
        Assert.Equal("headerName", Assert.Throws<ArgumentException>(() =>
            new SetStringHeaderInitializer<TestMessage, HeaderInput>(" ", inputApplicationProperty)).ParamName);
        Assert.Equal("propertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new SetStringHeaderInitializer<TestMessage, HeaderInput>("tenant", null!)).ParamName);

        var copy = new CopyHeaderInitializer<TestMessage, HeaderInput, Guid?>(correlationProperty, inputCorrelationProperty);
        var context = CreateContext(new HeaderInput(Guid.NewGuid(), "value"));
        var sendContext = new MessageSendContext<TestMessage>(new TestMessage(), TestContext.Current.CancellationToken);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            copy.ApplyAsync(null!, sendContext, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("sendContext", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            copy.ApplyAsync(context, null!, TestContext.Current.CancellationToken))).ParamName);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var throwingContext = CreateThrowingContext<TestMessage, HeaderInput>();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            copy.ApplyAsync(throwingContext, sendContext, cancellation.Token));
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
    }

    static InitializeContext<TestMessage, TInput> CreateContext<TInput>(TInput input)
        where TInput : class =>
        new BaseInitializeContext(TestContext.Current.CancellationToken)
            .CreateMessageContext(new TestMessage())
            .CreateInputContext(input);

    static InitializeContext<TMessage, TInput> CreateNoInputContext<TMessage, TInput>()
        where TMessage : class
        where TInput : class =>
        DispatchProxy.Create<InitializeContext<TMessage, TInput>, NoInputContextProxy>();

    static InitializeContext<TMessage, TInput> CreateThrowingContext<TMessage, TInput>()
        where TMessage : class
        where TInput : class =>
        DispatchProxy.Create<InitializeContext<TMessage, TInput>, ThrowingContextProxy>();

    private sealed class RecordingProvider<T>(Task<T?> task) : IPropertyProvider<HeaderInput, T>
    {
        public object? Context { get; private set; }

        public Task<T?> Task => task;

        public CancellationToken Token { get; private set; }

        public Task<T?> GetPropertyAsync<TMessage>(InitializeContext<TMessage, HeaderInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Context = context;
            Token = cancellationToken;
            return task;
        }
    }

    private sealed class NullTaskProvider<T> : IPropertyProvider<HeaderInput, T>
    {
        public Task<T?> GetPropertyAsync<TMessage>(InitializeContext<TMessage, HeaderInput> context,
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

    private sealed record HeaderInput(Guid? CorrelationId, string? Application);

    private sealed class TestMessage;

    private sealed class ExpectedHeaderException(string message) : Exception(message);
}
