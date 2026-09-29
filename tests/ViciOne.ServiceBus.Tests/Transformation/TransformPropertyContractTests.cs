using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transformation;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transformation;

public sealed class TransformPropertyContractTests
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-NESTED-TRANSFORM-METADATA", "absent-source-does-not-evaluate-property-transform")]
    public async Task AbsentSourceTransform_DoesNotReadAnAvailableInitializationInputAsync()
    {
        var source = new SourceMessage("input value");
        var transform = new SourceTransformContext(source, hasInput: false);
        InitializeContext<SourceMessage, SourceMessage> context = CreateContext(transform, source);
        var inputProvider = new RecordingPropertyProvider(Task.FromResult<string?>("input value"));
        var callbackCalls = 0;
        var provider = new DelegatePropertyProvider<SourceMessage, string?>(inputProvider, property =>
        {
            callbackCalls++;
            return Task.FromResult(property.Value);
        });

        string? result = await provider.GetPropertyAsync(context, TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(0, inputProvider.Calls);
        Assert.Equal(0, callbackCalls);
        Assert.False(transform.HasInput);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NESTED-TRANSFORM-METADATA", "present-null-property-preserves-source-envelope")]
    public async Task PresentNullProperty_PassesTheExactSourceAndEnvelopeToItsCallbackAsync()
    {
        var source = new SourceMessage(null);
        using var owner = new CancellationTokenSource();
        var transform = new SourceTransformContext(source, hasInput: true, owner.Token);
        InitializeContext<SourceMessage, SourceMessage> context = CreateContext(transform, source);
        var inputProvider = new RecordingPropertyProvider(Task.FromResult<string?>(null));
        TransformPropertyContext<string?, SourceMessage>? observed = null;
        var callbackCalls = 0;
        var provider = new DelegatePropertyProvider<SourceMessage, string?>(inputProvider, property =>
        {
            observed = property;
            callbackCalls++;
            return Task.FromResult<string?>("resolved-null");
        });
        CancellationToken token = TestContext.Current.CancellationToken;

        string? result = await provider.GetPropertyAsync(context, token);

        Assert.Equal("resolved-null", result);
        Assert.Equal(1, inputProvider.Calls);
        Assert.Same(context, inputProvider.Context);
        Assert.Equal(token, inputProvider.Token);
        Assert.Equal(1, callbackCalls);
        Assert.NotNull(observed);
        Assert.True(observed.HasInput);
        Assert.Same(source, observed.Input);
        Assert.True(observed.HasValue);
        Assert.Null(observed.Value);
        AssertEnvelope(transform, observed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-TRANSFORM-PROPERTY", "null-input-or-callback-task-is-diagnosed")]
    public async Task PropertyTransform_RejectsNullTasksAtTheirOwningStageAsync(bool nullInputTask)
    {
        var source = new SourceMessage("value");
        var transform = new SourceTransformContext(source, hasInput: true);
        InitializeContext<SourceMessage, SourceMessage> context = CreateContext(transform, source);
        var inputProvider = new RecordingPropertyProvider(nullInputTask ? null! : Task.FromResult<string?>("value"));
        var callbackCalls = 0;
        var provider = new DelegatePropertyProvider<SourceMessage, string?>(inputProvider, _ =>
        {
            callbackCalls++;
            return null!;
        });

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetPropertyAsync(context, TestContext.Current.CancellationToken));

        Assert.Equal(nullInputTask
            ? "The input property provider returned a null task."
            : "The property transform callback returned a null task.", failure.Message);
        Assert.Equal(1, inputProvider.Calls);
        Assert.Equal(nullInputTask ? 0 : 1, callbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSFORM-PROPERTY", "raw-provider-null-task-is-rejected-without-mutation")]
    public async Task RawTransformInitializer_RejectsNullProviderTaskBeforeMessageMutationAsync()
    {
        PropertyInfo property = typeof(TargetMessage).GetProperty(nameof(TargetMessage.Value))!;
        var message = new TargetMessage { Value = "before" };
        InitializeContext<TargetMessage, SourceMessage> context = new BaseInitializeContext(TestContext.Current.CancellationToken)
            .CreateMessageContext(message).CreateInputContext(new SourceMessage("source"));
        var nullTaskProvider = new RecordingPropertyProvider(null!);
        var invalid = new TransformPropertyInitializer<TargetMessage, SourceMessage, string?>(nullTaskProvider, property);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invalid.ApplyAsync(context, TestContext.Current.CancellationToken));
        Assert.Equal("The property provider returned a null task.", failure.Message);
        Assert.Equal("before", message.Value);
        Assert.Equal(1, nullTaskProvider.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSFORM-PROPERTY", "pre-canceled-raw-provider-is-not-invoked")]
    public async Task RawTransformInitializer_PreCanceledCallDoesNotInvokeProviderAsync()
    {
        PropertyInfo property = typeof(TargetMessage).GetProperty(nameof(TargetMessage.Value))!;
        var message = new TargetMessage { Value = "before" };
        InitializeContext<TargetMessage, SourceMessage> context = new BaseInitializeContext(TestContext.Current.CancellationToken)
            .CreateMessageContext(message).CreateInputContext(new SourceMessage("source"));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var availableProvider = new RecordingPropertyProvider(Task.FromResult<string?>("must not assign"));
        var canceledInitializer = new TransformPropertyInitializer<TargetMessage, SourceMessage, string?>(availableProvider, property);
        OperationCanceledException canceledFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            canceledInitializer.ApplyAsync(context, canceled.Token));
        Assert.Equal(canceled.Token, canceledFailure.CancellationToken);
        Assert.Equal(0, availableProvider.Calls);
        Assert.Equal("before", message.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSFORM-PROPERTY", "raw-provider-pending-and-faulted-work-is-observed")]
    public async Task RawTransformInitializer_AwaitsAcceptedProviderAndPreservesItsFailureAsync()
    {
        PropertyInfo property = typeof(TargetMessage).GetProperty(nameof(TargetMessage.Value))!;
        var message = new TargetMessage { Value = "before" };
        InitializeContext<TargetMessage, SourceMessage> context = new BaseInitializeContext(TestContext.Current.CancellationToken)
            .CreateMessageContext(message).CreateInputContext(new SourceMessage("source"));
        var pendingSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingProvider = new RecordingPropertyProvider(pendingSource.Task);
        var pendingInitializer = new TransformPropertyInitializer<TargetMessage, SourceMessage, string?>(pendingProvider, property);

        Task pending = pendingInitializer.ApplyAsync(context, TestContext.Current.CancellationToken);
        Assert.False(pending.IsCompleted);
        Assert.Equal("before", message.Value);
        pendingSource.SetResult("after");
        await pending.WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);
        Assert.Equal("after", message.Value);
        Assert.Equal(1, pendingProvider.Calls);

        var expected = new InvalidOperationException("property storage failed");
        var faultedProvider = new RecordingPropertyProvider(Task.FromException<string?>(expected));
        var faultedInitializer = new TransformPropertyInitializer<TargetMessage, SourceMessage, string?>(faultedProvider, property);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            faultedInitializer.ApplyAsync(context, TestContext.Current.CancellationToken));
        Assert.Same(expected, actual);
        Assert.Equal("after", message.Value);
        Assert.Equal(1, faultedProvider.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NESTED-TRANSFORM-METADATA", "nested-converter-awaits-and-preserves-parent-envelope")]
    public async Task NestedPropertyConverter_AwaitsInitializationWithParentEnvelopeAsync()
    {
        var source = new SourceMessage("root");
        using var owner = new CancellationTokenSource();
        var transform = new SourceTransformContext(source, hasInput: true, owner.Token);
        InitializeContext<SourceMessage> parent = new ScopeInitializeContext(transform).CreateMessageContext(source);
        var nested = new NestedMessage("original");
        var initializer = new RecordingNestedInitializer();
        var converter = new TransformPropertyConverter<NestedMessage>(initializer);
        CancellationToken token = TestContext.Current.CancellationToken;

        Task<NestedMessage?> pending = converter.ConvertAsync(parent, nested, token);
        Assert.False(pending.IsCompleted);
        Assert.Same(nested, initializer.Input);
        Assert.Equal(token, initializer.Token);
        var nestedContext = Assert.IsType<PropertyTransformContext<SourceMessage, NestedMessage>>(initializer.ParentContext);
        Assert.True(nestedContext.HasInput);
        Assert.Same(nested, nestedContext.Input);
        AssertEnvelope(transform, nestedContext);

        initializer.Complete(new NestedMessage("transformed"));
        NestedMessage? result = await pending.WaitAsync(CompletionTimeout, token);
        Assert.Equal("transformed", result?.Value);
        Assert.NotSame(nested, result);
        Assert.Equal(1, initializer.CreateCalls);
        Assert.Equal(1, initializer.InitializeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSFORM-PROPERTY", "nested-converter-null-task-and-fault-preserve-diagnostics")]
    public async Task NestedPropertyConverter_RejectsNullInitializationAndPreservesFaultIdentityAsync()
    {
        var source = new SourceMessage("root");
        var transform = new SourceTransformContext(source, hasInput: true);
        InitializeContext<SourceMessage> parent = new ScopeInitializeContext(transform).CreateMessageContext(source);
        var nested = new NestedMessage("original");
        var invalid = new RecordingNestedInitializer { ReturnNullTask = true };
        var converter = new TransformPropertyConverter<NestedMessage>(invalid);

        InvalidOperationException nullTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            converter.ConvertAsync(parent, nested, TestContext.Current.CancellationToken));
        Assert.Equal("The nested message initializer returned a null task.", nullTask.Message);
        Assert.Equal(1, invalid.InitializeCalls);

        var faulted = new RecordingNestedInitializer();
        var faultedConverter = new TransformPropertyConverter<NestedMessage>(faulted);
        Task<NestedMessage?> pending = faultedConverter.ConvertAsync(parent, nested, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("nested repository failed");
        faulted.Fail(expected);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
        Assert.Same(expected, actual);
        Assert.Equal(1, faulted.InitializeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSFORM-PROPERTY", "pre-canceled-nested-converter-does-not-create-or-initialize")]
    public async Task NestedPropertyConverter_PreCanceledCallDoesNotCreateOrInitializeAsync()
    {
        var source = new SourceMessage("root");
        var transform = new SourceTransformContext(source, hasInput: true);
        InitializeContext<SourceMessage> parent = new ScopeInitializeContext(transform).CreateMessageContext(source);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var initializer = new RecordingNestedInitializer();
        var converter = new TransformPropertyConverter<NestedMessage>(initializer);

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            converter.ConvertAsync(parent, new NestedMessage("original"), canceled.Token));

        Assert.Equal(canceled.Token, failure.CancellationToken);
        Assert.Equal(0, initializer.CreateCalls);
        Assert.Equal(0, initializer.InitializeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSFORM-PROPERTY", "nested-converter-rejects-null-context-before-initialization")]
    public async Task NestedPropertyConverter_NullContextDoesNotCreateOrInitializeAsync()
    {
        var initializer = new RecordingNestedInitializer();
        var converter = new TransformPropertyConverter<NestedMessage>(initializer);

        ArgumentNullException failure = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            converter.ConvertAsync<SourceMessage>(null!, new NestedMessage("original"), TestContext.Current.CancellationToken));

        Assert.Equal("context", failure.ParamName);
        Assert.Equal(0, initializer.CreateCalls);
        Assert.Equal(0, initializer.InitializeCalls);
    }

    private static InitializeContext<SourceMessage, SourceMessage> CreateContext(SourceTransformContext transform, SourceMessage input) =>
        new ScopeInitializeContext(transform).CreateMessageContext(new SourceMessage("target")).CreateInputContext(input);

    private static void AssertEnvelope(SourceTransformContext expected, TransformContext actual)
    {
        Assert.Equal(expected.CancellationToken, actual.CancellationToken);
        Assert.Equal(expected.MessageId, actual.MessageId);
        Assert.Equal(expected.RequestId, actual.RequestId);
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.ConversationId, actual.ConversationId);
        Assert.Equal(expected.InitiatorId, actual.InitiatorId);
        Assert.Equal(expected.ExpirationTime, actual.ExpirationTime);
        Assert.Equal(expected.SourceAddress, actual.SourceAddress);
        Assert.Equal(expected.DestinationAddress, actual.DestinationAddress);
        Assert.Equal(expected.ResponseAddress, actual.ResponseAddress);
        Assert.Equal(expected.FaultAddress, actual.FaultAddress);
        Assert.Equal(expected.SentTime, actual.SentTime);
        Assert.Same(expected.Headers, actual.Headers);
        Assert.Same(expected.Host, actual.Host);
    }

    private sealed record SourceMessage(string? Value);

    private sealed class TargetMessage
    {
        public string? Value { get; set; }
    }

    private sealed record NestedMessage(string Value);

    private sealed class SourceTransformContext(SourceMessage source, bool hasInput, CancellationToken ownerToken = default)
        : BasePipeContext(ownerToken), TransformContext<SourceMessage>
    {
        public Guid? MessageId { get; } = Guid.NewGuid();
        public Guid? RequestId { get; } = Guid.NewGuid();
        public Guid? CorrelationId { get; } = Guid.NewGuid();
        public Guid? ConversationId { get; } = Guid.NewGuid();
        public Guid? InitiatorId { get; } = Guid.NewGuid();
        public DateTimeOffset? ExpirationTime { get; } = new(2036, 1, 2, 3, 4, 5, TimeSpan.Zero);
        public Uri? SourceAddress { get; } = new("loopback://localhost/transform-source");
        public Uri? DestinationAddress { get; } = new("loopback://localhost/transform-destination");
        public Uri? ResponseAddress { get; } = new("loopback://localhost/transform-response");
        public Uri? FaultAddress { get; } = new("loopback://localhost/transform-fault");
        public DateTimeOffset? SentTime { get; } = new(2036, 1, 2, 3, 0, 0, TimeSpan.Zero);
        public Headers Headers { get; } = EmptyHeaders.Instance;
        public HostInfo Host { get; } = HostMetadataCache.Host;
        public bool HasInput => hasInput;
        public SourceMessage Input => hasInput ? source : throw new InvalidOperationException("Source input is absent.");
    }

    private sealed class RecordingPropertyProvider(Task<string?> task) : IPropertyProvider<SourceMessage, string?>
    {
        public int Calls { get; private set; }
        public object? Context { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<string?> GetPropertyAsync<T>(InitializeContext<T, SourceMessage> context, CancellationToken cancellationToken = default)
            where T : class
        {
            Calls++;
            Context = context;
            Token = cancellationToken;
            return task;
        }
    }

    private sealed class RecordingNestedInitializer : IMessageInitializer<NestedMessage>
    {
        private readonly TaskCompletionSource<InitializeContext<NestedMessage>> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ReturnNullTask { get; set; }
        public int CreateCalls { get; private set; }
        public int InitializeCalls { get; private set; }
        public PipeContext? ParentContext { get; private set; }
        public object? Input { get; private set; }
        public CancellationToken Token { get; private set; }

        public InitializeContext<NestedMessage> Create(PipeContext context)
        {
            CreateCalls++;
            ParentContext = context;
            return new ScopeInitializeContext(context).CreateMessageContext(new NestedMessage("uninitialized"));
        }

        public Task<InitializeContext<NestedMessage>> InitializeAsync(InitializeContext<NestedMessage> context, object input,
            CancellationToken cancellationToken = default)
        {
            InitializeCalls++;
            Input = input;
            Token = cancellationToken;
            return ReturnNullTask ? null! : _completion.Task;
        }

        public void Complete(NestedMessage message) =>
            _completion.SetResult(new ScopeInitializeContext(ParentContext!).CreateMessageContext(message));

        public void Fail(Exception exception) => _completion.SetException(exception);

        public InitializeContext<NestedMessage> Create(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InitializeContext<NestedMessage>> InitializeAsync(object input, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<NestedMessage>> InitializeMessageAsync(
            PipeContext context, object input, IPipe<SendContext<NestedMessage>>? pipe = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<NestedMessage>> InitializeMessageAsync(
            PipeContext context, object input, object?[] moreInputs, IPipe<SendContext<NestedMessage>>? pipe = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<NestedMessage>> InitializeMessageAsync(
            object input, IPipe<SendContext<NestedMessage>> pipe, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
