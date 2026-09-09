using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.HeaderInitializers;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class MessageInitializerContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CANCELLATION", "property-initializers-receive-caller-token")]
    public async Task Initialization_ForwardsTheCallerTokenToEveryPropertyInitializerAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var propertyInitializer = new RecordingPropertyInitializer();
        MessageInitializer<TestMessage, TestInput> initializer = Create(propertyInitializer: propertyInitializer);

        await initializer.InitializeAsync(new TestInput(), cancellation.Token);

        Assert.Equal(cancellation.Token, propertyInitializer.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CANCELLATION", "header-initializers-receive-send-context-token")]
    public async Task InitializedSendPipe_ForwardsTheSendContextTokenToEveryHeaderInitializerAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var headerInitializer = new RecordingHeaderInitializer();
        MessageInitializer<TestMessage, TestInput> initializer = Create(headerInitializer: headerInitializer);
        InitializedMessage<TestMessage> initialized = await initializer.InitializeMessageAsync(
            new TestInput(),
            Pipe.Empty<SendContext<TestMessage>>(),
            TestContext.Current.CancellationToken);
        SendContext<TestMessage> sendContext = DispatchProxy.Create<SendContext<TestMessage>, SendContextProxy>();
        ((SendContextProxy)(object)sendContext).CancellationToken = cancellation.Token;

        await initialized.Pipe.SendAsync(sendContext);

        Assert.Equal(cancellation.Token, headerInitializer.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONSTRUCTION", "dependencies-and-elements-required")]
    public void Constructor_RejectsMissingDependenciesAndNullInitializerElements()
    {
        var factory = new TestMessageFactory();
        IPropertyInitializer<TestMessage, TestInput>[] properties = [null!];
        IHeaderInitializer<TestMessage, TestInput>[] headers = [null!];

        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            new MessageInitializer<TestMessage, TestInput>(null!, [], [])).ParamName);
        Assert.Equal("initializers", Assert.Throws<ArgumentNullException>(() =>
            new MessageInitializer<TestMessage, TestInput>(factory, null!, [])).ParamName);
        Assert.Equal("headerInitializers", Assert.Throws<ArgumentNullException>(() =>
            new MessageInitializer<TestMessage, TestInput>(factory, [], null!)).ParamName);
        Assert.Equal("initializers", Assert.Throws<ArgumentException>(() =>
            new MessageInitializer<TestMessage, TestInput>(factory, properties, [])).ParamName);
        Assert.Equal("headerInitializers", Assert.Throws<ArgumentException>(() =>
            new MessageInitializer<TestMessage, TestInput>(factory, [], headers)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-INPUT", "null-and-incompatible-inputs-rejected-at-entry")]
    public async Task Initialization_RejectsMissingAndIncompatibleInputsAsync()
    {
        MessageInitializer<TestMessage, TestInput> initializer = Create();

        Assert.Equal("input", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            initializer.InitializeAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("input", (await Assert.ThrowsAsync<ArgumentException>(() =>
            initializer.InitializeAsync(new object(), TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-INPUT", "contexts-pipes-and-additional-input-array-required")]
    public async Task Initialization_RejectsMissingContextsPipesAndAdditionalInputArraysAsync()
    {
        MessageInitializer<TestMessage, TestInput> initializer = Create();
        var input = new TestInput();

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => initializer.Create(null!)).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            initializer.InitializeMessageAsync(input, null!, TestContext.Current.CancellationToken))).ParamName);
        ArgumentNullException additionalInputs = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            initializer.InitializeMessageAsync(
                new TestPipeContext(),
                input,
                null!,
                pipe: null,
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("moreInputs", additionalInputs.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FAULT", "completed-property-fault-is-not-aggregate-wrapped")]
    public async Task CompletedPropertyFault_IsPropagatedWithoutAnAggregateWrapperAsync()
    {
        var expected = new ExpectedInitializerException("property failed");
        var provider = new FaultedPropertyProvider(expected);
        var initializer = new ProviderPropertyInitializer<TestMessage, TestInput, string?>(
            provider,
            typeof(TestMessage).GetProperty(nameof(TestMessage.Value))!);
        InitializeContext<TestMessage> messageContext = new TestMessageFactory().Create(
            new BaseInitializeContext(TestContext.Current.CancellationToken));
        InitializeContext<TestMessage, TestInput> inputContext = messageContext.CreateInputContext(new TestInput());

        ExpectedInitializerException actual = await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
            initializer.ApplyAsync(inputContext, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FAULT", "completed-header-fault-is-not-aggregate-wrapped")]
    public async Task CompletedHeaderFault_IsPropagatedWithoutAnAggregateWrapperAsync()
    {
        var expected = new ExpectedInitializerException("header failed");
        var provider = new FaultedCorrelationIdProvider(expected);
        var initializer = new ProviderHeaderInitializer<TestMessage, TestInput, Guid?>(
            provider,
            typeof(SendContext).GetProperty(nameof(SendContext.CorrelationId))!);
        InitializeContext<TestMessage> messageContext = new TestMessageFactory().Create(
            new BaseInitializeContext(TestContext.Current.CancellationToken));
        InitializeContext<TestMessage, TestInput> inputContext = messageContext.CreateInputContext(new TestInput());
        SendContext<TestMessage> sendContext = DispatchProxy.Create<SendContext<TestMessage>, SendContextProxy>();

        ExpectedInitializerException actual = await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
            initializer.ApplyAsync(inputContext, sendContext, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    private static MessageInitializer<TestMessage, TestInput> Create(
        IPropertyInitializer<TestMessage, TestInput>? propertyInitializer = null,
        IHeaderInitializer<TestMessage, TestInput>? headerInitializer = null) =>
        new(
            new TestMessageFactory(),
            propertyInitializer is null ? [] : [propertyInitializer],
            headerInitializer is null ? [] : [headerInitializer]);

    private sealed class TestMessage
    {
        public string? Value { get; set; }
    }

    private sealed class TestInput;

    private sealed class TestMessageFactory : IMessageFactory<TestMessage>
    {
        public InitializeContext<TestMessage> Create(InitializeContext context) =>
            context.CreateMessageContext(new TestMessage());
    }

    private sealed class RecordingPropertyInitializer : IPropertyInitializer<TestMessage, TestInput>
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task ApplyAsync(
            InitializeContext<TestMessage, TestInput> context,
            CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHeaderInitializer : IHeaderInitializer<TestMessage, TestInput>
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task ApplyAsync(
            InitializeContext<TestMessage, TestInput> context,
            SendContext sendContext,
            CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class FaultedPropertyProvider(Exception exception) : IPropertyProvider<TestInput, string?>
    {
        public Task<string?> GetPropertyAsync<TMessage>(
            InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.FromException<string?>(exception);
    }

    private sealed class FaultedCorrelationIdProvider(Exception exception) : IPropertyProvider<TestInput, Guid?>
    {
        public Task<Guid?> GetPropertyAsync<TMessage>(
            InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.FromException<Guid?>(exception);
    }

    private sealed class TestPipeContext : BasePipeContext;

    private class SendContextProxy : DispatchProxy
    {
        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_CancellationToken")
                return CancellationToken;

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private sealed class ExpectedInitializerException(string message) : Exception(message);
}
