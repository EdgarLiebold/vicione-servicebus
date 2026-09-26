using System.Reflection;
using System.Text;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.MessageData.PropertyProviders;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData.PropertyProviders;

public sealed class PutMessageDataPropertyProviderTests
{
    [Theory]
    [InlineData(7L, 19L, 3L, 10L)]
    [InlineData(7L, 19L, null, 7L)]
    [InlineData(null, 19L, 3L, 19L)]
    [InlineData(null, null, 3L, null)]
    [InlineData(long.MaxValue - 1, 19L, 1L, long.MaxValue)]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "retention-precedence-addition-and-unlimited-storage")]
    public async Task RetentionPolicy_SelectsTheExactLifetimeWithoutChangingTheStoredValueAsync(
        long? sendTicks, long? policyTicks, long? extraTicks, long? expectedTicks)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new RecordingRepository();
        var root = new BaseInitializeContext(cancellationToken);
        var send = new MessageSendContext<TestMessage>(new TestMessage(), cancellationToken)
        {
            TimeToLive = Duration(sendTicks)
        };
        root.GetOrAddPayload<SendContext>(() => send);
        InitializeContext<TestMessage, TestInput> context = root.CreateMessageContext(send.Message)
            .CreateInputContext(new TestInput(new PutMessageData<string>("retained payload")));
        var provider = new PutMessageDataPropertyProvider<TestInput, string>(
            new ScriptedInputProvider(Task.FromResult<MessageData<string>?>(new PutMessageData<string>("retained payload"))),
            repository,
            new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1,
                timeToLive: Duration(policyTicks), extraTimeToLive: Duration(extraTicks)));

        MessageData<string> result = Assert.IsAssignableFrom<MessageData<string>>(
            await provider.GetPropertyAsync(context, cancellationToken));

        Assert.Equal(Duration(expectedTicks), repository.TimeToLive);
        Assert.Equal(repository.Address, result.Address);
        Assert.Equal("retained payload", await result.Value);
        Assert.Equal(Encoding.UTF8.GetBytes("retained payload"), repository.Bytes);
        Assert.Equal(cancellationToken, repository.CancellationToken);
        Assert.Equal(1, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "retention-overflow-fails-before-storage")]
    public async Task RetentionOverflow_FailsBeforeWritingAnyRepositoryDataAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new RecordingRepository();
        var root = new BaseInitializeContext(cancellationToken);
        var send = new MessageSendContext<TestMessage>(new TestMessage(), cancellationToken) { TimeToLive = TimeSpan.MaxValue };
        root.GetOrAddPayload<SendContext>(() => send);
        InitializeContext<TestMessage, TestInput> context = root.CreateMessageContext(send.Message)
            .CreateInputContext(new TestInput(new PutMessageData<string>("must not be stored")));
        var provider = new PutMessageDataPropertyProvider<TestInput, string>(
            new ScriptedInputProvider(Task.FromResult<MessageData<string>?>(new PutMessageData<string>("must not be stored"))),
            repository,
            new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1, extraTimeToLive: TimeSpan.FromTicks(1)));

        MessageDataException error = await Assert.ThrowsAsync<MessageDataException>(() => provider.GetPropertyAsync(context, cancellationToken));

        Assert.IsType<OverflowException>(error.InnerException);
        Assert.Equal("The outgoing message lifetime and additional repository retention exceed the supported duration.", error.Message);
        Assert.Equal(0, repository.PutCalls);
        Assert.Empty(repository.Bytes);
    }

    private static TimeSpan? Duration(long? ticks) => ticks.HasValue ? TimeSpan.FromTicks(ticks.Value) : null;

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-REPOSITORY-RESOLUTION", "owner-bound-repository-and-policy")]
    public async Task ConstructorRepositoryAndPolicy_AreUsedByTheProviderAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new RecordingRepository();
        var root = new BaseInitializeContext(cancellationToken);
        InitializeContext<TestMessage> messageContext = root.CreateMessageContext(new TestMessage());
        var input = new TestInput(new PutMessageData<string>("context-owned repository"));
        InitializeContext<TestMessage, TestInput> context = messageContext.CreateInputContext(input);
        PropertyInfo property = typeof(TestInput).GetProperty(nameof(TestInput.Value))
            ?? throw new InvalidOperationException("The test input property is missing.");
        var inputProvider = new InputPropertyProvider<TestInput, MessageData<string>>(property);
        var policy = new MessageDataPolicy(
            alwaysWriteToRepository: true,
            threshold: 1,
            timeToLive: TimeSpan.FromMinutes(12));
        var provider = new PutMessageDataPropertyProvider<TestInput, string>(inputProvider, repository, policy);

        MessageData<string> result = await provider.GetPropertyAsync(context, TestContext.Current.CancellationToken)
            ?? throw new Xunit.Sdk.XunitException("Expected the message-data property provider to return a value.");

        Assert.Equal(repository.Address, result.Address);
        Assert.Equal("context-owned repository", await result.Value);
        Assert.Equal("context-owned repository", Encoding.UTF8.GetString(repository.Bytes));
        Assert.Equal(cancellationToken, repository.CancellationToken);
        Assert.Equal(TimeSpan.FromMinutes(12), repository.TimeToLive);
        Assert.Equal(1, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "put-input-fault-independent-of-task-timing")]
    public async Task InputFailure_PreservesItsIdentityForCompletedAndPendingTasksAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var root = new BaseInitializeContext(cancellationToken);
        InitializeContext<TestMessage> messageContext = root.CreateMessageContext(new TestMessage());
        InitializeContext<TestMessage, TestInput> context = messageContext.CreateInputContext(
            new TestInput(ViciOne.ServiceBus.Advanced.MessageData.FromValue("unused")));
        var repository = new RecordingRepository();
        var expectedCompleted = new ExpectedInputException("completed");
        var expectedPending = new ExpectedInputException("pending");
        var pending = new TaskCompletionSource<MessageData<string>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedProvider = new PutMessageDataPropertyProvider<TestInput, string>(
            new ScriptedInputProvider(Task.FromException<MessageData<string>?>(expectedCompleted)),
            repository,
            MessageDataPolicy.Default);
        var pendingProvider = new PutMessageDataPropertyProvider<TestInput, string>(
            new ScriptedInputProvider(pending.Task),
            repository,
            MessageDataPolicy.Default);

        Task<MessageData<string>?> pendingResultTask = pendingProvider.GetPropertyAsync(context, cancellationToken);
        pending.SetException(expectedPending);

        ExpectedInputException completed = await Assert.ThrowsAsync<ExpectedInputException>(() =>
            completedProvider.GetPropertyAsync(context, cancellationToken));
        ExpectedInputException asynchronouslyCompleted = await Assert.ThrowsAsync<ExpectedInputException>(() => pendingResultTask);
        Assert.Same(expectedCompleted, completed);
        Assert.Same(expectedPending, asynchronouslyCompleted);
        Assert.Equal(0, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "put-context-null-boundary")]
    public async Task PutProvider_RejectsANullContextBeforeReadingItsInputAsync()
    {
        var repository = new RecordingRepository();
        var provider = new PutMessageDataPropertyProvider<TestInput, string>(
            new ScriptedInputProvider(Task.FromResult<MessageData<string>?>(null)),
            repository,
            MessageDataPolicy.Default);

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.GetPropertyAsync<TestMessage>(null!, TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
        Assert.Equal(0, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "all-addressless-values-use-owned-repository")]
    public async Task AddresslessCustomValue_IsStoredAndANullReportedValueFailsExplicitlyAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var root = new BaseInitializeContext(cancellationToken);
        InitializeContext<TestMessage> messageContext = root.CreateMessageContext(new TestMessage());
        InitializeContext<TestMessage, TestInput> context = messageContext.CreateInputContext(
            new TestInput(ViciOne.ServiceBus.Advanced.MessageData.FromValue("unused")));
        var repository = new RecordingRepository();
        var provider = new PutMessageDataPropertyProvider<TestInput, string>(
            new ScriptedInputProvider(Task.FromResult<MessageData<string>?>(new CustomMessageData(Task.FromResult<string?>("custom")))),
            repository,
            new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1));

        MessageData<string> result = Assert.IsAssignableFrom<MessageData<string>>(
            await provider.GetPropertyAsync(context, cancellationToken));

        Assert.Equal(repository.Address, result.Address);
        Assert.Equal("custom", await result.Value);
        Assert.Equal(1, repository.PutCalls);

        var invalidProvider = new PutMessageDataPropertyProvider<TestInput, string>(
            new ScriptedInputProvider(Task.FromResult<MessageData<string>?>(new CustomMessageData(Task.FromResult<string?>(null)))),
            new RecordingRepository(),
            MessageDataPolicy.Default);
        MessageDataException exception = await Assert.ThrowsAsync<MessageDataException>(() =>
            invalidProvider.GetPropertyAsync(context, cancellationToken));
        Assert.Contains("reported a value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "method-cancellation-token-owns-storage")]
    public async Task MethodCancellationToken_IsForwardedToRepositoryStorageAsync()
    {
        using var contextCancellation = new CancellationTokenSource();
        using var methodCancellation = new CancellationTokenSource();
        var root = new BaseInitializeContext(contextCancellation.Token);
        InitializeContext<TestMessage> messageContext = root.CreateMessageContext(new TestMessage());
        InitializeContext<TestMessage, TestInput> context = messageContext.CreateInputContext(
            new TestInput(ViciOne.ServiceBus.Advanced.MessageData.FromValue("value")));
        var repository = new RecordingRepository();
        var provider = new PutMessageDataPropertyProvider<TestInput, string>(
            new ScriptedInputProvider(Task.FromResult<MessageData<string>?>(
                ViciOne.ServiceBus.Advanced.MessageData.FromValue("value"))),
            repository,
            new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1));

        _ = await provider.GetPropertyAsync(context, methodCancellation.Token);

        Assert.Equal(methodCancellation.Token, repository.CancellationToken);
        Assert.NotEqual(contextCancellation.Token, repository.CancellationToken);
    }

    private sealed record TestInput(MessageData<string> Value);

    private sealed class TestMessage;

    private sealed class ScriptedInputProvider(Task<MessageData<string>?> result) :
        IPropertyProvider<TestInput, MessageData<string>>
    {
        public Task<MessageData<string>?> GetPropertyAsync<T>(
            InitializeContext<T, TestInput> context,
            CancellationToken cancellationToken = default)
            where T : class => result;
    }

    private sealed class ExpectedInputException(string message) : Exception(message);

    private sealed class CustomMessageData(Task<string?> value) : MessageData<string>
    {
        public Uri? Address => null;

        public bool HasValue => true;

        public Task<string?> Value => value;
    }

    private sealed class RecordingRepository : IMessageDataRepository
    {
        public Uri Address { get; } = new("urn:file:context-owned");

        public byte[] Bytes { get; private set; } = [];

        public CancellationToken CancellationToken { get; private set; }

        public int PutCalls { get; private set; }

        public TimeSpan? TimeToLive { get; private set; }

        public Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.IO.Stream>(cancellationToken); throw new NotSupportedException(); }
        public async Task<Uri> PutAsync(
            Stream stream,
            TimeSpan? timeToLive = null,
            CancellationToken cancellationToken = default)
        {
            PutCalls++;
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy, cancellationToken);
            Bytes = copy.ToArray();
            CancellationToken = cancellationToken;
            TimeToLive = timeToLive;
            return Address;
        }
    }
}
