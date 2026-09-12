using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipExecutorContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "constructor-requires-transport-capabilities")]
    public void Constructor_RejectsMissingTransportCapabilities()
    {
        var endpoints = new RecordingTransport();

        Assert.Equal("sendEndpointProvider", Assert.Throws<ArgumentNullException>(() =>
            new RoutingSlipExecutor(null!, endpoints)).ParamName);
        Assert.Equal("publishEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new RoutingSlipExecutor(endpoints, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "active-routing-slip-is-validated-snapshotted-and-sent")]
    public async Task ExecuteAsync_SendsAValidatedSnapshotInsteadOfCallerOwnedStateAsync()
    {
        var transport = new RecordingTransport();
        var clock = new FakeTimeProvider(CreatedAt);
        var arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["input"] = "original" };
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["tenant"] = "north" };
        var itinerary = new List<Activity>
        {
            new MutableActivity("ChargeCard", new Uri("loopback://localhost/charge"), arguments),
        };
        var source = MutableRoutingSlip.Create(itinerary, variables);
        var executor = new RoutingSlipExecutor(transport, transport, clock);

        await executor.ExecuteAsync(source, TestContext.Current.CancellationToken);

        arguments["input"] = "mutated";
        variables["tenant"] = "south";
        itinerary.Clear();

        RoutingSlip submitted = Assert.IsAssignableFrom<RoutingSlip>(Assert.Single(transport.Sent));
        Assert.NotSame(source, submitted);
        Assert.Equal("original", Assert.Single(submitted.Itinerary).Arguments["input"]);
        Assert.Equal("north", submitted.Variables["tenant"]);
        Assert.Empty(transport.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "completed-routing-slip-uses-injected-clock-and-snapshot")]
    public async Task ExecuteAsync_PublishesCompletionAtTheInjectedTimeWithDetachedVariablesAsync()
    {
        var transport = new RecordingTransport();
        var clock = new FakeTimeProvider(CreatedAt);
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["tenant"] = "north" };
        var source = MutableRoutingSlip.Create([], variables);
        var executor = new RoutingSlipExecutor(transport, transport, clock);
        clock.Advance(TimeSpan.FromMinutes(7));

        await executor.ExecuteAsync(source, TestContext.Current.CancellationToken);
        variables["tenant"] = "south";

        RoutingSlipCompleted completed = Assert.IsAssignableFrom<RoutingSlipCompleted>(Assert.Single(transport.Published));
        Assert.Equal(CreatedAt.AddMinutes(7), completed.Timestamp);
        Assert.Equal(TimeSpan.FromMinutes(7), completed.Duration);
        Assert.Equal("north", completed.Variables["tenant"]);
        Assert.Empty(transport.Sent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "malformed-routing-slip-rejected-before-transport")]
    public async Task ExecuteAsync_RejectsMalformedContractStateBeforeUsingTransportAsync()
    {
        var transport = new RecordingTransport();
        var clock = new FakeTimeProvider(CreatedAt);
        var executor = new RoutingSlipExecutor(transport, transport, clock);
        MutableRoutingSlip valid = MutableRoutingSlip.Create([], new Dictionary<string, object>());
        RoutingSlip[] malformed =
        [
            valid with { TrackingNumber = Guid.Empty },
            valid with { CreateTimestamp = default },
            valid with { CreateTimestamp = CreatedAt.AddTicks(1) },
            valid with { Itinerary = null! },
            valid with { Variables = null! },
            valid with
            {
                Itinerary =
                [
                    new MutableActivity(" ", new Uri("loopback://localhost/charge"), new Dictionary<string, object>()),
                ],
            },
        ];

        foreach (RoutingSlip routingSlip in malformed)
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                executor.ExecuteAsync(routingSlip, TestContext.Current.CancellationToken));
            Assert.Equal("routingSlip", exception.ParamName);
        }

        Assert.Empty(transport.Sent);
        Assert.Empty(transport.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "pre-canceled-execution-does-not-use-transport")]
    public async Task ExecuteAsync_WithPreCanceledTokenDoesNotUseTransportAsync()
    {
        var transport = new RecordingTransport();
        var executor = new RoutingSlipExecutor(transport, transport, new FakeTimeProvider(CreatedAt));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.ExecuteAsync(MutableRoutingSlip.Create([], new Dictionary<string, object>()), cancellation.Token));

        Assert.Empty(transport.Sent);
        Assert.Empty(transport.Published);
    }

    private sealed record MutableRoutingSlip : RoutingSlip
    {
        public Guid TrackingNumber { get; init; }

        public DateTimeOffset CreateTimestamp { get; init; }

        public IReadOnlyList<Activity> Itinerary { get; init; } = [];

        public IReadOnlyList<ActivityLog> ActivityLogs { get; init; } = [];

        public IReadOnlyList<CompensateLog> CompensateLogs { get; init; } = [];

        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();

        public IReadOnlyList<ActivityException> ActivityExceptions { get; init; } = [];

        public IReadOnlyList<Subscription> Subscriptions { get; init; } = [];

        public static MutableRoutingSlip Create(IReadOnlyList<Activity> itinerary, IReadOnlyDictionary<string, object> variables) =>
            new()
            {
                TrackingNumber = NewId.NextGuid(),
                CreateTimestamp = CreatedAt,
                Itinerary = itinerary,
                Variables = variables,
            };
    }

    private sealed class MutableActivity(string name, Uri address, IReadOnlyDictionary<string, object> arguments) : Activity
    {
        public string Name { get; } = name;

        public Uri Address { get; } = address;

        public IReadOnlyDictionary<string, object> Arguments { get; } = arguments;
    }

    private sealed class RecordingTransport : ISendEndpointProvider, ISendEndpoint, IPublishEndpoint
    {
        public List<object> Sent { get; } = [];

        public List<object> Published { get; } = [];

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ISendEndpoint>(this);
        }

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            Sent.Add(message);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return SendAsync(message, cancellationToken);
        }

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            Published.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return PublishAsync(message, cancellationToken);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
    }
}
