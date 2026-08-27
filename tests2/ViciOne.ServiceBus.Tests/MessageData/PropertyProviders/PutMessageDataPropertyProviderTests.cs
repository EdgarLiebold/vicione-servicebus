namespace ViciOne.ServiceBus.Tests.MessageData.PropertyProviders;

using System.Reflection;
using System.Text;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.MessageData.PropertyProviders;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PutMessageDataPropertyProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-REPOSITORY-RESOLUTION", "pipe-context-payload")]
    public async Task ContextRepository_IsUsedWhenTheProviderHasNoConstructorRepository()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new RecordingRepository();
        var root = new BaseInitializeContext(cancellationToken);
        root.GetOrAddPayload<IMessageDataRepository>(() => repository);
        InitializeContext<TestMessage> messageContext = root.CreateMessageContext(new TestMessage());
        var input = new TestInput(new PutMessageData<string>("context-owned repository"));
        InitializeContext<TestMessage, TestInput> context = messageContext.CreateInputContext(input);
        PropertyInfo property = typeof(TestInput).GetProperty(nameof(TestInput.Value))
            ?? throw new InvalidOperationException("The test input property is missing.");
        var inputProvider = new InputPropertyProvider<TestInput, MessageData<string>>(property);
        var provider = new PutMessageDataPropertyProvider<TestInput, string>(inputProvider);

        MessageData<string> result = await provider.GetProperty(context);

        Assert.Equal(repository.Address, result.Address);
        Assert.Equal("context-owned repository", await result.Value);
        Assert.Equal("context-owned repository", Encoding.UTF8.GetString(repository.Bytes));
        Assert.Equal(cancellationToken, repository.CancellationToken);
        Assert.Equal(1, repository.PutCalls);
    }

    private sealed record TestInput(MessageData<string> Value);

    private sealed class TestMessage;

    private sealed class RecordingRepository : IMessageDataRepository
    {
        public Uri Address { get; } = new("urn:file:context-owned");

        public byte[] Bytes { get; private set; } = [];

        public CancellationToken CancellationToken { get; private set; }

        public int PutCalls { get; private set; }

        public Task<Stream> Get(Uri address, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task<Uri> Put(
            Stream stream,
            TimeSpan? timeToLive = null,
            CancellationToken cancellationToken = default)
        {
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy, cancellationToken);
            Bytes = copy.ToArray();
            CancellationToken = cancellationToken;
            PutCalls++;
            return Address;
        }
    }
}
