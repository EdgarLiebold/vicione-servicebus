using System.Text;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.MessageData.PropertyProviders;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData.PropertyProviders;

public sealed class GetMessageDataPropertyProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "addressed-value-lazy-single-load")]
    public async Task AddressedValue_IsLoadedLazilyOnceFromItsOwningRepositoryAsync()
    {
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;
        InitializeContext<TestMessage, TestInput> context = CreateContext(cancellationToken);
        var address = new Uri("urn:message-data:customer-note");
        var repository = new RecordingRepository("repository value");
        var provider = new GetMessageDataPropertyProvider<TestInput, string>(
            new ScriptedPropertyProvider(Task.FromResult<MessageData<string>?>(new AddressReference(address))),
            repository);

        MessageData<string>? result = await provider.GetPropertyAsync(context, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(address, result.Address);
        Assert.True(result.HasValue);
        Assert.Equal(0, repository.GetCount);

        Task<string?> firstRead = result.Value;
        Task<string?> secondRead = result.Value;

        Assert.Same(firstRead, secondRead);
        Assert.Equal("repository value", await firstRead);
        Assert.Equal(1, repository.GetCount);
        Assert.Equal(address, repository.LastAddress);
        Assert.Equal(cancellationToken, repository.LastCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "empty-value-independent-of-task-timing")]
    public async Task EmptyReferencedValue_HasTheSameMeaningForCompletedAndPendingInputTasksAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InitializeContext<TestMessage, TestInput> context = CreateContext(cancellationToken);
        var emptyReference = new EmptyReference(new Uri("urn:message-data:empty"));
        var pending = new TaskCompletionSource<MessageData<string>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedProvider = new GetMessageDataPropertyProvider<TestInput, string>(
            new ScriptedPropertyProvider(Task.FromResult<MessageData<string>?>(emptyReference)),
            new RejectingRepository());
        var pendingProvider = new GetMessageDataPropertyProvider<TestInput, string>(
            new ScriptedPropertyProvider(pending.Task),
            new RejectingRepository());

        Task<MessageData<string>?> pendingResultTask = pendingProvider.GetPropertyAsync(context, cancellationToken);
        pending.SetResult(emptyReference);
        MessageData<string>? completedResult = await completedProvider.GetPropertyAsync(context, cancellationToken);
        MessageData<string>? pendingResult = await pendingResultTask;

        Assert.NotNull(completedResult);
        Assert.NotNull(pendingResult);
        Assert.False(completedResult.HasValue);
        Assert.False(pendingResult.HasValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "fault-identity-independent-of-task-timing")]
    public async Task InputFailure_PreservesItsIdentityForCompletedAndPendingTasksAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InitializeContext<TestMessage, TestInput> context = CreateContext(cancellationToken);
        var expectedCompleted = new ExpectedInputException("completed");
        var expectedPending = new ExpectedInputException("pending");
        var pending = new TaskCompletionSource<MessageData<string>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedProvider = new GetMessageDataPropertyProvider<TestInput, string>(
            new ScriptedPropertyProvider(Task.FromException<MessageData<string>?>(expectedCompleted)),
            new RejectingRepository());
        var pendingProvider = new GetMessageDataPropertyProvider<TestInput, string>(
            new ScriptedPropertyProvider(pending.Task),
            new RejectingRepository());

        Task<MessageData<string>?> pendingResultTask = pendingProvider.GetPropertyAsync(context, cancellationToken);
        pending.SetException(expectedPending);

        ExpectedInputException completed = await Assert.ThrowsAsync<ExpectedInputException>(() =>
            completedProvider.GetPropertyAsync(context, cancellationToken));
        ExpectedInputException asynchronouslyCompleted = await Assert.ThrowsAsync<ExpectedInputException>(() => pendingResultTask);
        Assert.Same(expectedCompleted, completed);
        Assert.Same(expectedPending, asynchronouslyCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PROPERTY-PROVIDERS", "constructor-and-context-boundaries")]
    public async Task Provider_RejectsMissingRequiredDependenciesAtTheirOwningBoundaryAsync()
    {
        var inputProvider = new ScriptedPropertyProvider(Task.FromResult<MessageData<string>?>(null));

        Assert.Equal("inputProvider", Assert.Throws<ArgumentNullException>(() =>
            new GetMessageDataPropertyProvider<TestInput, string>(null!, new RejectingRepository())).ParamName);
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            new GetMessageDataPropertyProvider<TestInput, string>(inputProvider, null!)).ParamName);

        var provider = new GetMessageDataPropertyProvider<TestInput, string>(inputProvider, new RejectingRepository());
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.GetPropertyAsync<TestMessage>(null!, TestContext.Current.CancellationToken))).ParamName);
    }

    private static InitializeContext<TestMessage, TestInput> CreateContext(CancellationToken cancellationToken)
    {
        var root = new BaseInitializeContext(cancellationToken);
        InitializeContext<TestMessage> message = root.CreateMessageContext(new TestMessage());
        return message.CreateInputContext(new TestInput());
    }

    private sealed class ScriptedPropertyProvider(Task<MessageData<string>?> result) :
        IPropertyProvider<TestInput, MessageData<string>>
    {
        public Task<MessageData<string>?> GetPropertyAsync<T>(
            InitializeContext<T, TestInput> context,
            CancellationToken cancellationToken = default)
            where T : class => result;
    }

    private sealed class EmptyReference(Uri address) : MessageData<string>
    {
        public Uri Address => address;

        public bool HasValue => false;

        public Task<string?> Value => throw new InvalidOperationException("An empty reference has no value.");
    }

    private sealed class AddressReference(Uri address) : MessageData<string>
    {
        public Uri Address => address;

        public bool HasValue => true;

        public Task<string?> Value => throw new InvalidOperationException("An unresolved reference has no local value.");
    }

    private sealed class RecordingRepository(string value) : IMessageDataRepository
    {
        public int GetCount { get; private set; }

        public Uri? LastAddress { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
        {
            GetCount++;
            LastAddress = address;
            LastCancellationToken = cancellationToken;

            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(value), writable: false);
            return Task.FromResult(stream);
        }

        public Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The get provider must not write to the repository.");
    }

    private sealed class RejectingRepository : IMessageDataRepository
    {
        public Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("An empty reference must not reach the repository.");

        public Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The get provider must not write to the repository.");
    }

    private sealed class TestInput;

    private sealed class TestMessage;

    private sealed class ExpectedInputException(string message) : Exception(message);
}
