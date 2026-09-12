using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class MessageInitializerCacheContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-API", "cache-facade-success-overload-matrix")]
    public async Task CacheFacade_ExecutesEveryPublicSuccessOverloadAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var input = new CacheInput("primary");
        var pipeContext = new TestPipeContext(token);

        IMessageInitializer<CacheMessage> first = MessageInitializerCache<CacheMessage>.GetInitializer(typeof(CacheInput));
        IMessageInitializer<CacheMessage> second = MessageInitializerCache<CacheMessage>.GetInitializer(typeof(CacheInput));
        Assert.Same(first, second);

        InitializeContext<CacheMessage> initialized = await MessageInitializerCache<CacheMessage>.InitializeAsync(input, token);
        Assert.Equal("primary", initialized.Message.Value);

        InitializedMessage<CacheMessage> inherited = await MessageInitializerCache<CacheMessage>.InitializeMessageAsync(
            pipeContext, input, token);
        Assert.Equal("primary", inherited.Message.Value);

        var inheritedPipe = new RecordingPipe();
        InitializedMessage<CacheMessage> inheritedWithPipe = await MessageInitializerCache<CacheMessage>.InitializeMessageAsync(
            pipeContext, input, inheritedPipe, token);
        await inheritedWithPipe.Pipe.SendAsync(new MessageSendContext<CacheMessage>(inheritedWithPipe.Message, token));
        Assert.Equal(1, inheritedPipe.SendCount);

        var combinedPipe = new RecordingPipe();
        InitializedMessage<CacheMessage> combined = await MessageInitializerCache<CacheMessage>.InitializeMessageAsync(
            pipeContext,
            input,
            [new CountInput(37), null],
            combinedPipe,
            token);
        await combined.Pipe.SendAsync(new MessageSendContext<CacheMessage>(combined.Message, token));
        Assert.Equal("primary", combined.Message.Value);
        Assert.Equal(37, combined.Message.Count);
        Assert.Equal(1, combinedPipe.SendCount);

        InitializedMessage<CacheMessage> standalone = await MessageInitializerCache<CacheMessage>.InitializeMessageAsync(input, token);
        Assert.Equal("primary", standalone.Message.Value);

        var standalonePipe = new RecordingPipe();
        InitializedMessage<CacheMessage> standaloneWithPipe = await MessageInitializerCache<CacheMessage>.InitializeMessageAsync(
            input, standalonePipe, token);
        await standaloneWithPipe.Pipe.SendAsync(new MessageSendContext<CacheMessage>(standaloneWithPipe.Message, token));
        Assert.Equal(1, standalonePipe.SendCount);

        InitializeContext<CacheMessage> existing = first.Create(token);
        InitializeContext<CacheMessage> updated = await MessageInitializerCache<CacheMessage>.InitializeAsync(
            existing, new CacheInput("updated"), token);
        Assert.Same(existing, updated);
        Assert.Equal("updated", updated.Message.Value);
    }

    public sealed class CacheMessage
    {
        public int Count { get; set; }

        public string? Value { get; set; }
    }

    public sealed record CacheInput(string Value);

    public sealed record CountInput(int Count);

    private sealed class RecordingPipe : IPipe<SendContext<CacheMessage>>
    {
        public int SendCount { get; private set; }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }

        public Task SendAsync(SendContext<CacheMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            SendCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestPipeContext(CancellationToken cancellationToken) : BasePipeContext(cancellationToken);
}
