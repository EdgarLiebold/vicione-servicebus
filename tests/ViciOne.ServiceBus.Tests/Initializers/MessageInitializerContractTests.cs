using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Initializers.HeaderInitializers;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
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
    [RequirementCoverage("REQ-VSB-INITIALIZER-PIPE", "probe-and-generic-send-adapter-matrix")]
    public async Task InitializedSendPipe_ImplementsProbeAndGenericSendContractsAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var innerPipe = new RecordingSendPipe();
        MessageInitializer<TestMessage, TestInput> initializer = Create(headerInitializer: new RecordingHeaderInitializer());
        InitializedMessage<TestMessage> initialized = await initializer.InitializeMessageAsync(
            new TestPipeContext(), new TestInput(), innerPipe, token);

        Assert.NotNull(initialized.Pipe.GetProbeResult(token));
        Assert.Equal(1, innerPipe.ProbeCount);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => initialized.Pipe.Probe(null!)).ParamName);

        ISendPipe sendPipe = Assert.IsAssignableFrom<ISendPipe>(initialized.Pipe);
        var sendContext = new MessageSendContext<TestMessage>(initialized.Message, token);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sendPipe.SendAsync<TestMessage>(null!, token))).ParamName);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sendPipe.SendAsync(sendContext, cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);

        await sendPipe.SendAsync(sendContext, token);
        Assert.Equal(1, innerPipe.GenericSendCount);

        InitializedMessage<TestMessage> withoutInnerPipe = await initializer.InitializeMessageAsync(
            new TestPipeContext(), new TestInput(), pipe: null, cancellationToken: token);
        await Assert.IsAssignableFrom<ISendPipe>(withoutInnerPipe.Pipe).SendAsync(sendContext, token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CANCELLATION", "pending-property-initializer-observes-caller-token")]
    public async Task PendingPropertyInitializer_ObservesCallerCancellationAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var propertyInitializer = new PendingPropertyInitializer();
        MessageInitializer<TestMessage, TestInput> initializer = Create(propertyInitializer: propertyInitializer);

        Task<InitializeContext<TestMessage>> result = initializer.InitializeAsync(new TestInput(), cancellation.Token);
        try
        {
            cancellation.Cancel();
            Assert.Equal(cancellation.Token, propertyInitializer.CancellationToken);
            Assert.False(result.IsCompleted);
            Assert.False(propertyInitializer.Task.IsCompleted);
            propertyInitializer.Cancel(cancellation.Token);
            OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                result.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, exception.CancellationToken);
            Assert.True(propertyInitializer.Task.IsCanceled);
        }
        finally
        {
            propertyInitializer.Release();
            await ObserveAsync(propertyInitializer.Task);
            await ObserveAsync(result);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CANCELLATION", "pending-header-initializer-observes-send-token")]
    public async Task PendingHeaderInitializer_ObservesSendContextCancellationAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var headerInitializer = new PendingHeaderInitializer();
        MessageInitializer<TestMessage, TestInput> initializer = Create(headerInitializer: headerInitializer);
        InitializedMessage<TestMessage> initialized = await initializer.InitializeMessageAsync(
            new TestInput(),
            Pipe.Empty<SendContext<TestMessage>>(),
            TestContext.Current.CancellationToken);
        SendContext<TestMessage> sendContext = DispatchProxy.Create<SendContext<TestMessage>, SendContextProxy>();
        ((SendContextProxy)(object)sendContext).CancellationToken = cancellation.Token;

        Task result = initialized.Pipe.SendAsync(sendContext);
        try
        {
            cancellation.Cancel();
            Assert.Equal(cancellation.Token, headerInitializer.CancellationToken);
            Assert.False(result.IsCompleted);
            Assert.False(headerInitializer.Task.IsCompleted);
            headerInitializer.Cancel(cancellation.Token);
            OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                result.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, exception.CancellationToken);
            Assert.True(headerInitializer.Task.IsCanceled);
        }
        finally
        {
            headerInitializer.Release();
            await ObserveAsync(headerInitializer.Task);
            await ObserveAsync(result);
        }
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
    [RequirementCoverage("REQ-VSB-INITIALIZER-API", "cache-facade-required-inputs")]
    public void CacheFacade_RejectsEveryMissingRequiredInputAtItsPublicBoundary()
    {
        var pipeContext = new TestPipeContext();
        var input = new TestInput();
        IPipe<SendContext<TestMessage>> pipe = Pipe.Empty<SendContext<TestMessage>>();
        InitializeContext<TestMessage> messageContext = new TestMessageFactory().Create(
            new BaseInitializeContext(TestContext.Current.CancellationToken));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertCacheNull("inputType", () => _ = MessageInitializerCache<TestMessage>.GetInitializer(null!));
        AssertCacheNull("input", () => _ = MessageInitializerCache<TestMessage>.InitializeAsync(null!, cancellationToken));

        AssertCacheNull("context", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            null!, input, cancellationToken));
        AssertCacheNull("input", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            context: pipeContext, input: null!, cancellationToken: cancellationToken));

        AssertCacheNull("context", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            null!, input, [], cancellationToken: cancellationToken));
        AssertCacheNull("input", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            pipeContext, null!, [], cancellationToken: cancellationToken));
        AssertCacheNull("moreInputs", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            pipeContext, input, (object?[])null!, cancellationToken: cancellationToken));

        AssertCacheNull("context", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            null!, input, pipe, cancellationToken));
        AssertCacheNull("input", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            pipeContext, null!, pipe, cancellationToken));
        AssertCacheNull("pipe", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            pipeContext, input, null!, cancellationToken));

        AssertCacheNull("input", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            null!, cancellationToken));
        AssertCacheNull("input", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            (object)null!, pipe, cancellationToken));
        AssertCacheNull("pipe", () => _ = MessageInitializerCache<TestMessage>.InitializeMessageAsync(
            input, null!, cancellationToken));

        AssertCacheNull("context", () => _ = MessageInitializerCache<TestMessage>.InitializeAsync(
            null!, input, cancellationToken));
        AssertCacheNull("input", () => _ = MessageInitializerCache<TestMessage>.InitializeAsync(
            messageContext, null!, cancellationToken));
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

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FAULT", "completed-custom-header-fault-is-not-aggregate-wrapped")]
    public async Task CompletedCustomHeaderFault_IsPropagatedWithoutAnAggregateWrapperAsync()
    {
        var expected = new ExpectedInitializerException("custom header failed");
        var provider = new FaultedPropertyProvider(expected);
        var initializer = new SetHeaderInitializer<TestMessage, TestInput, string?>("custom-header", provider);
        InitializeContext<TestMessage> messageContext = new TestMessageFactory().Create(
            new BaseInitializeContext(TestContext.Current.CancellationToken));
        InitializeContext<TestMessage, TestInput> inputContext = messageContext.CreateInputContext(new TestInput());
        var sendContext = new MessageSendContext<TestMessage>(messageContext.Message, TestContext.Current.CancellationToken);

        ExpectedInitializerException actual = await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
            initializer.ApplyAsync(inputContext, sendContext, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CANCELLATION", "pre-canceled-provider-initializers-do-not-resolve-values")]
    public async Task ProviderInitializers_DoNotResolveValuesAfterCallerCancellationAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var propertyProvider = new CountingProvider<TestInput, string?>("value");
        var headerProvider = new CountingProvider<TestInput, Guid?>(Guid.NewGuid());
        var customHeaderProvider = new CountingProvider<TestInput, string?>("value");
        var propertyInitializer = new ProviderPropertyInitializer<TestMessage, TestInput, string?>(
            propertyProvider,
            typeof(TestMessage).GetProperty(nameof(TestMessage.Value))!);
        var headerInitializer = new ProviderHeaderInitializer<TestMessage, TestInput, Guid?>(
            headerProvider,
            typeof(SendContext).GetProperty(nameof(SendContext.CorrelationId))!);
        var customHeaderInitializer = new SetHeaderInitializer<TestMessage, TestInput, string?>("custom", customHeaderProvider);
        InitializeContext<TestMessage> messageContext = new TestMessageFactory().Create(
            new BaseInitializeContext(TestContext.Current.CancellationToken));
        InitializeContext<TestMessage, TestInput> inputContext = messageContext.CreateInputContext(new TestInput());
        SendContext<TestMessage> sendContext = DispatchProxy.Create<SendContext<TestMessage>, SendContextProxy>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => propertyInitializer.ApplyAsync(inputContext, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => headerInitializer.ApplyAsync(inputContext, sendContext, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => customHeaderInitializer.ApplyAsync(inputContext, sendContext, cancellation.Token));

        Assert.Equal(0, propertyProvider.CallCount);
        Assert.Equal(0, headerProvider.CallCount);
        Assert.Equal(0, customHeaderProvider.CallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CANCELLATION", "pending-task-property-observes-caller-token")]
    public async Task PendingTaskProperty_ObservesCallerCancellationAsync()
    {
        var source = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var initializer = new CopyAsyncObjectPropertyInitializer<TestMessage, AsyncTestInput, string>(
            typeof(TestMessage).GetProperty(nameof(TestMessage.ObjectValue))!,
            typeof(AsyncTestInput).GetProperty(nameof(AsyncTestInput.Value))!);
        InitializeContext<TestMessage> messageContext = new TestMessageFactory().Create(
            new BaseInitializeContext(TestContext.Current.CancellationToken));
        InitializeContext<TestMessage, AsyncTestInput> inputContext =
            messageContext.CreateInputContext(new AsyncTestInput(source.Task));

        Task result = initializer.ApplyAsync(inputContext, cancellation.Token);
        try
        {
            cancellation.Cancel();
            OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                result.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, exception.CancellationToken);
            Assert.False(source.Task.IsCompleted);
            Assert.Null(messageContext.Message.ObjectValue);
        }
        finally
        {
            source.TrySetResult("cleanup");
            await ObserveAsync(source.Task);
            await ObserveAsync(result);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FAULT", "completed-task-property-fault-is-not-aggregate-wrapped")]
    public async Task CompletedTaskPropertyFault_IsPropagatedWithoutAnAggregateWrapperAsync()
    {
        var expected = new ExpectedInitializerException("task property failed");
        var initializer = new CopyAsyncObjectPropertyInitializer<TestMessage, AsyncTestInput, string>(
            typeof(TestMessage).GetProperty(nameof(TestMessage.ObjectValue))!,
            typeof(AsyncTestInput).GetProperty(nameof(AsyncTestInput.Value))!);
        InitializeContext<TestMessage> messageContext = new TestMessageFactory().Create(
            new BaseInitializeContext(TestContext.Current.CancellationToken));
        InitializeContext<TestMessage, AsyncTestInput> inputContext =
            messageContext.CreateInputContext(new AsyncTestInput(Task.FromException<string>(expected)));

        ExpectedInitializerException actual = await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
            initializer.ApplyAsync(inputContext, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FAULT", "null-property-initializer-task")]
    public async Task NullPropertyInitializerTask_IsRejectedAtTheOwningBoundaryAsync()
    {
        MessageInitializer<TestMessage, TestInput> initializer = Create(
            propertyInitializer: new NullTaskPropertyInitializer());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.InitializeAsync(new TestInput(), TestContext.Current.CancellationToken));

        Assert.Equal("A property initializer returned a null task.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FAULT", "null-header-initializer-task")]
    public async Task NullHeaderInitializerTask_StopsBeforeTheDownstreamPipeAsync()
    {
        var downstream = new RecordingSendPipe();
        MessageInitializer<TestMessage, TestInput> initializer = Create(
            headerInitializer: new NullTaskHeaderInitializer());
        InitializedMessage<TestMessage> initialized = await initializer.InitializeMessageAsync(
            new TestInput(),
            downstream,
            TestContext.Current.CancellationToken);
        var sendContext = new MessageSendContext<TestMessage>(
            initialized.Message,
            TestContext.Current.CancellationToken);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initialized.Pipe.SendAsync(sendContext));

        Assert.Equal("A header initializer returned a null task.", exception.Message);
        Assert.Equal(0, downstream.TypedSendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-API", "cache-facades-hide-mutable-implementation")]
    public void CacheFacades_AreStaticAndDoNotExportImplementationContracts()
    {
        Assert.True(typeof(MessageInitializerCache<>).IsAbstract);
        Assert.True(typeof(MessageInitializerCache<>).IsSealed);
        Assert.True(typeof(TypeConverterCache).IsAbstract);
        Assert.True(typeof(TypeConverterCache).IsSealed);

        Type[] exportedTypes = typeof(MessageInitializer).Assembly.GetExportedTypes();
        Assert.DoesNotContain(exportedTypes, type => type.Name == "IMessageInitializerCache`1");
        Assert.DoesNotContain(exportedTypes, type => type.Name == "ITypeConverterCache");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-API", "implementation-types-are-not-exported")]
    public void PublicApi_ExportsContractsAndFacadesButNotInitializerImplementations()
    {
        Type[] assemblyTypes = typeof(MessageInitializer).Assembly.GetTypes();
        Type[] exportedTypes = typeof(MessageInitializer).Assembly.GetExportedTypes();
        string[] implementationNamespaces =
        [
            "ViciOne.ServiceBus.Initializers.Contexts",
            "ViciOne.ServiceBus.Initializers.Factories",
            "ViciOne.ServiceBus.Initializers.HeaderInitializers",
            "ViciOne.ServiceBus.Initializers.PropertyConverters",
            "ViciOne.ServiceBus.Initializers.PropertyInitializers",
            "ViciOne.ServiceBus.Initializers.PropertyProviders",
        ];

        Assert.DoesNotContain(exportedTypes, type => implementationNamespaces.Contains(type.Namespace));
        Assert.DoesNotContain(exportedTypes, type =>
            type.Namespace == "ViciOne.ServiceBus.Initializers.Conventions" && type.IsClass);
        Assert.DoesNotContain(exportedTypes, type =>
            type.Namespace == "ViciOne.ServiceBus.Initializers.TypeConverters" && type != typeof(TypeConverterCache));
        Assert.DoesNotContain(exportedTypes, type => type.Name is
            "IMessageInitializerFactory`1" or
            "IMessageFactory" or
            "IMessageFactory`1" or
            "IPropertyProviderFactory`1" or
            "MessageFactoryCache`1" or
            "MessageInitializer`2");
        Assert.DoesNotContain(assemblyTypes, type => type.Name is
            "IMessageInitializerConvention" or
            "IMessageInputInitializerConvention`1");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONSTRUCTION", "convention-registry-rejects-null-elements")]
    public void ConventionRegistry_RejectsNullConventionElements()
    {
        IInitializerConvention[] conventions = [null!];

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new InitializerConventionRegistry(conventions));

        Assert.Equal("conventions", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-API", "conventions-freeze-after-first-snapshot")]
    public void ConventionRegistration_RejectsMutationAfterThePublishedSnapshotIsRead()
    {
        IReadOnlyList<IInitializerConvention> first = MessageInitializer.Conventions;
        IReadOnlyList<IInitializerConvention> second = MessageInitializer.Conventions;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MessageInitializer.AddConvention<UnusedConvention>());

        Assert.Same(first, second);
        Assert.Contains("immutable", exception.Message, StringComparison.Ordinal);
    }

    private static MessageInitializer<TestMessage, TestInput> Create(
        IPropertyInitializer<TestMessage, TestInput>? propertyInitializer = null,
        IHeaderInitializer<TestMessage, TestInput>? headerInitializer = null) =>
        new(
            new TestMessageFactory(),
            propertyInitializer is null ? [] : [propertyInitializer],
            headerInitializer is null ? [] : [headerInitializer]);

    private static void AssertCacheNull(string parameterName, Action operation) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(operation).ParamName);

    private static async Task ObserveAsync(Task task)
    {
        await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.True(task.IsCompleted);
    }

    private sealed class TestMessage
    {
        public object? ObjectValue { get; set; }

        public string? Value { get; set; }
    }

    private sealed class TestInput;

    private sealed record AsyncTestInput(Task<string> Value);

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

    private sealed class PendingPropertyInitializer : IPropertyInitializer<TestMessage, TestInput>
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Task => _completion.Task;

        public CancellationToken CancellationToken { get; private set; }

        public void Cancel(CancellationToken token) => _completion.TrySetCanceled(token);

        public void Release() => _completion.TrySetResult();

        public Task ApplyAsync(InitializeContext<TestMessage, TestInput> context,
            CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return _completion.Task;
        }
    }

    private sealed class PendingHeaderInitializer : IHeaderInitializer<TestMessage, TestInput>
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Task => _completion.Task;

        public CancellationToken CancellationToken { get; private set; }

        public void Cancel(CancellationToken token) => _completion.TrySetCanceled(token);

        public void Release() => _completion.TrySetResult();

        public Task ApplyAsync(InitializeContext<TestMessage, TestInput> context, SendContext sendContext,
            CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return _completion.Task;
        }
    }

    private sealed class NullTaskPropertyInitializer : IPropertyInitializer<TestMessage, TestInput>
    {
        public Task ApplyAsync(
            InitializeContext<TestMessage, TestInput> context,
            CancellationToken cancellationToken = default) => null!;
    }

    private sealed class NullTaskHeaderInitializer : IHeaderInitializer<TestMessage, TestInput>
    {
        public Task ApplyAsync(
            InitializeContext<TestMessage, TestInput> context,
            SendContext sendContext,
            CancellationToken cancellationToken = default) => null!;
    }

    private sealed class FaultedPropertyProvider(Exception exception) : IPropertyProvider<TestInput, string?>
    {
        public Task<string?> GetPropertyAsync<TMessage>(
            InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.FromException<string?>(exception);
    }

    private sealed class CountingProvider<TInputValue, TValue>(TValue value) : IPropertyProvider<TInputValue, TValue>
        where TInputValue : class
    {
        public int CallCount { get; private set; }

        public Task<TValue?> GetPropertyAsync<TMessage>(
            InitializeContext<TMessage, TInputValue> context,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            CallCount++;
            return Task.FromResult<TValue?>(value);
        }
    }

    private sealed class FaultedCorrelationIdProvider(Exception exception) : IPropertyProvider<TestInput, Guid?>
    {
        public Task<Guid?> GetPropertyAsync<TMessage>(
            InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.FromException<Guid?>(exception);
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RecordingSendPipe : IPipe<SendContext<TestMessage>>, ISendContextPipe
    {
        public int GenericSendCount { get; private set; }

        public int ProbeCount { get; private set; }

        public int TypedSendCount { get; private set; }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ProbeCount++;
        }

        public Task SendAsync(SendContext<TestMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            TypedSendCount++;
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            GenericSendCount++;
            return Task.CompletedTask;
        }
    }

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

    private sealed class UnusedConvention : IInitializerConvention
    {
        public bool TryGetPropertyInitializer<TMessageValue, TInputValue, TProperty>(
            PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyInitializer<TMessageValue, TInputValue>? initializer)
            where TMessageValue : class
            where TInputValue : class
        {
            initializer = null;
            return false;
        }

        public bool TryGetHeaderInitializer<TMessageValue, TInputValue, TProperty>(
            PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessageValue, TInputValue>? initializer)
            where TMessageValue : class
            where TInputValue : class
        {
            initializer = null;
            return false;
        }

        public bool TryGetHeadersInitializer<TMessageValue, TInputValue, TProperty>(
            PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessageValue, TInputValue>? initializer)
            where TMessageValue : class
            where TInputValue : class
        {
            initializer = null;
            return false;
        }
    }
}
