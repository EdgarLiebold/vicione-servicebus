using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Fabric;

public sealed class MessageReceiverCollectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ENDPOINT-CONCURRENCY", "receiver-wait-cancellation")]
    public async Task NextAsync_WithNoConnectedReceiver_PreservesCancellationAsync()
    {
        var receivers = CreateCollection();
        using var cancellation = new CancellationTokenSource();

        Task<IMessageReceiver<ReceiverMessage>> pending = receivers.NextAsync(new ReceiverMessage("pending"), cancellation.Token);
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ENDPOINT-CONCURRENCY", "receiver-wait-completes-on-connection")]
    public async Task NextAsync_WithNoConnectedReceiver_CompletesWhenAReceiverConnectsAsync()
    {
        var receivers = CreateCollection();
        var receiver = new RecordingReceiver("connected");
        Task<IMessageReceiver<ReceiverMessage>> pending = receivers.NextAsync(
            new ReceiverMessage("pending"),
            TestContext.Current.CancellationToken);

        receivers.Connect(receiver);

        Assert.Same(receiver, await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ENDPOINT-CONCURRENCY", "deterministic-round-robin")]
    public async Task MultipleReceivers_AreSelectedInConnectionOrderAsync()
    {
        var receivers = CreateCollection();
        var first = new RecordingReceiver("first");
        var second = new RecordingReceiver("second");
        receivers.Connect(first);
        receivers.Connect(second);
        Assert.NotNull(receivers.GetProbeResult(TestContext.Current.CancellationToken));

        IMessageReceiver<ReceiverMessage>[] selected =
        [
            await receivers.NextAsync(new ReceiverMessage("one"), TestContext.Current.CancellationToken),
            await receivers.NextAsync(new ReceiverMessage("two"), TestContext.Current.CancellationToken),
            await receivers.NextAsync(new ReceiverMessage("three"), TestContext.Current.CancellationToken),
            await receivers.NextAsync(new ReceiverMessage("four"), TestContext.Current.CancellationToken)
        ];

        Assert.Equal([first, second, first, second], selected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ENDPOINT-CONCURRENCY", "disconnect-rebuilds-receiver-selection")]
    public async Task Disconnect_RebuildsSelectionAndIsIdempotentAsync()
    {
        var receivers = CreateCollection();
        var first = new RecordingReceiver("first");
        var second = new RecordingReceiver("second");
        ITopologyHandle firstConnection = receivers.Connect(first);
        receivers.Connect(second);

        firstConnection.Disconnect();
        firstConnection.Disconnect();

        IMessageReceiver<ReceiverMessage> selected = await receivers.NextAsync(
            new ReceiverMessage("message"),
            TestContext.Current.CancellationToken);
        Assert.Same(second, selected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ENDPOINT-CONCURRENCY", "receiver-selection-required-inputs")]
    public void ReceiverSelection_RejectsEveryInvalidRequiredInput()
    {
        Assert.Equal("balancerFactory", Assert.Throws<ArgumentNullException>(() =>
            new MessageReceiverCollection<ReceiverMessage>(null!)).ParamName);

        var receivers = CreateCollection();
        Assert.Equal("receiver", Assert.Throws<ArgumentNullException>(() => receivers.Connect(null!)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receivers.NextAsync(null!, TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => receivers.Probe(null!)).ParamName);

        Assert.Equal("receivers", Assert.Throws<ArgumentNullException>(() =>
            new RoundRobinReceiverLoadBalancer<ReceiverMessage>(null!)).ParamName);
        Assert.Equal("receivers", Assert.Throws<ArgumentException>(() =>
            new RoundRobinReceiverLoadBalancer<ReceiverMessage>([])).ParamName);
        Assert.Equal("receivers", Assert.Throws<ArgumentException>(() =>
            new RoundRobinReceiverLoadBalancer<ReceiverMessage>([null!])).ParamName);
        Assert.Equal("receiver", Assert.Throws<ArgumentNullException>(() =>
            new SingleReceiverLoadBalancer<ReceiverMessage>(null!)).ParamName);

        var receiver = new RecordingReceiver("receiver");
        var roundRobin = new RoundRobinReceiverLoadBalancer<ReceiverMessage>([receiver]);
        var single = new SingleReceiverLoadBalancer<ReceiverMessage>(receiver);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => roundRobin.SelectReceiver(null!)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => single.SelectReceiver(null!)).ParamName);
    }

    private static MessageReceiverCollection<ReceiverMessage> CreateCollection() =>
        new(receivers => new RoundRobinReceiverLoadBalancer<ReceiverMessage>(receivers));

    private sealed record ReceiverMessage(string Value);

    private sealed class RecordingReceiver(string name) : IMessageReceiver<ReceiverMessage>
    {
        public string Name { get; } = name;

        public Task DeliverAsync(ReceiverMessage message, CancellationToken cancellationToken) => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }

        public override string ToString() => Name;
    }
}
