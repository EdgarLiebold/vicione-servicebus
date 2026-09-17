using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierRoutingSlipExecutorDeepContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2041, 3, 4, 5, 6, 7, TimeSpan.Zero);
    private static readonly Uri ExecuteAddress = new("loopback://localhost/deep-executor-activity");
    private static readonly Uri SubscriptionAddress = new("loopback://localhost/deep-executor-subscription");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "deep-executor-builder-and-subscription-target-api-shapes")]
    public void OwnedApis_PreserveSyncBuildAsyncExecutionAndInternalNullableSubscriptionTargetShapes()
    {
        Type builderType = typeof(IRoutingSlipBuilder);
        Assert.True(builderType.IsPublic);
        Assert.Contains(typeof(IItineraryBuilder), builderType.GetInterfaces());
        MethodInfo build = Assert.Single(builderType.GetMethods(), method => method.DeclaringType == builderType);
        Assert.Equal(nameof(IRoutingSlipBuilder.Build), build.Name);
        Assert.DoesNotContain("Async", build.Name, StringComparison.Ordinal);
        Assert.Equal(typeof(IRoutingSlip), build.ReturnType);
        Assert.Empty(build.GetParameters());

        MethodInfo execute = Assert.Single(typeof(RoutingSlipExecutor).GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(RoutingSlipExecutor.ExecuteAsync), execute.Name);
        Assert.Equal(typeof(Task), execute.ReturnType);
        ParameterInfo[] executeParameters = execute.GetParameters();
        Assert.Equal([typeof(IRoutingSlip), typeof(CancellationToken)], executeParameters.Select(x => x.ParameterType));
        Assert.True(executeParameters[1].HasDefaultValue);

        Type targetType = typeof(IRoutingSlipSubscriptionTarget);
        Assert.True(targetType.IsNotPublic);
        MethodInfo addSubscription = Assert.Single(targetType.GetMethods());
        Assert.Equal(nameof(IRoutingSlipSubscriptionTarget.AddSubscription), addSubscription.Name);
        Assert.Equal(typeof(void), addSubscription.ReturnType);
        ParameterInfo[] targetParameters = addSubscription.GetParameters();
        Assert.Equal(["address", "events", "contents", "activityName", "message"], targetParameters.Select(x => x.Name));
        var nullability = new NullabilityInfoContext();
        Assert.Equal(NullabilityState.NotNull, nullability.Create(targetParameters[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, nullability.Create(targetParameters[3]).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(targetParameters[4]).ReadState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "deep-executor-null-slip-precedes-clock-and-transport")]
    public async Task ExecuteAsync_RejectsANullSlipBeforeReadingTheClockOrUsingTransportAsync()
    {
        var transport = new RecordingTransport();
        var clock = new CountingTimeProvider(CreatedAt);
        var executor = new RoutingSlipExecutor(transport, transport, clock);

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync(null!, TestContext.Current.CancellationToken));

        Assert.Equal("routingSlip", exception.ParamName);
        Assert.Equal(0, clock.ReadCount);
        AssertNoTransport(transport);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "deep-executor-pre-cancellation-preserves-token-before-clock-and-snapshot")]
    public async Task ExecuteAsync_PreCancellationPreservesTheExactTokenBeforeClockAndSnapshotAsync()
    {
        var transport = new RecordingTransport();
        var clock = new CountingTimeProvider(CreatedAt);
        var executor = new RoutingSlipExecutor(transport, transport, clock);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.ExecuteAsync(ValidSlip(), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, clock.ReadCount);
        AssertNoTransport(transport);
    }

    public static TheoryData<string> NullCollections => new()
    {
        nameof(IRoutingSlip.Itinerary),
        nameof(IRoutingSlip.ActivityLogs),
        nameof(IRoutingSlip.CompensateLogs),
        nameof(IRoutingSlip.Variables),
        nameof(IRoutingSlip.ActivityExceptions),
        nameof(IRoutingSlip.Subscriptions),
    };

    [Theory]
    [MemberData(nameof(NullCollections))]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "deep-executor-rejects-every-null-routing-slip-collection-atomically")]
    public async Task ExecuteAsync_RejectsEveryNullRoutingSlipCollectionBeforeTransportAsync(string collectionName)
    {
        MutableRoutingSlip routingSlip = ValidSlip() with
        {
            Itinerary = collectionName == nameof(IRoutingSlip.Itinerary) ? null! : [],
            ActivityLogs = collectionName == nameof(IRoutingSlip.ActivityLogs) ? null! : [],
            CompensateLogs = collectionName == nameof(IRoutingSlip.CompensateLogs) ? null! : [],
            Variables = collectionName == nameof(IRoutingSlip.Variables) ? null! : new Dictionary<string, object>(),
            ActivityExceptions = collectionName == nameof(IRoutingSlip.ActivityExceptions) ? null! : [],
            Subscriptions = collectionName == nameof(IRoutingSlip.Subscriptions) ? null! : [],
        };
        var transport = new RecordingTransport();
        var executor = new RoutingSlipExecutor(transport, transport, new FakeTimeProvider(CreatedAt));

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            executor.ExecuteAsync(routingSlip, TestContext.Current.CancellationToken));

        Assert.Equal("routingSlip", exception.ParamName);
        Assert.Contains("collections", exception.Message, StringComparison.OrdinalIgnoreCase);
        AssertNoTransport(transport);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "deep-executor-active-route-preserves-identity-address-token-and-detached-snapshot")]
    public async Task ExecuteAsync_ActiveSlipUsesOnlyTheFirstRouteAndSubmitsADetachedIdentityPreservingSnapshotAsync()
    {
        Guid trackingNumber = NewId.NextGuid();
        var arguments = new Dictionary<string, object> { ["input"] = "original" };
        var variables = new Dictionary<string, object> { ["tenant"] = "north" };
        var itinerary = new List<IActivity>
        {
            new MutableActivity("First", ExecuteAddress, arguments),
            new MutableActivity("Second", new Uri("loopback://localhost/deep-executor-second"), new Dictionary<string, object>()),
        };
        MutableRoutingSlip source = ValidSlip() with
        {
            TrackingNumber = trackingNumber,
            Itinerary = itinerary,
            Variables = variables,
        };
        var transport = new RecordingTransport();
        using var cancellation = new CancellationTokenSource();
        var executor = new RoutingSlipExecutor(transport, transport, new FakeTimeProvider(CreatedAt));

        await executor.ExecuteAsync(source, cancellation.Token);
        arguments["input"] = "changed";
        variables["tenant"] = "south";
        itinerary.Clear();

        Resolution resolution = Assert.Single(transport.Resolutions);
        Assert.Equal(ExecuteAddress, resolution.Address);
        Assert.Equal(cancellation.Token, resolution.CancellationToken);
        Sent sent = Assert.Single(transport.Sent);
        Assert.Equal(cancellation.Token, sent.CancellationToken);
        IRoutingSlip snapshot = Assert.IsAssignableFrom<IRoutingSlip>(sent.Message);
        Assert.NotSame(source, snapshot);
        Assert.Equal(trackingNumber, snapshot.TrackingNumber);
        Assert.Equal(CreatedAt, snapshot.CreateTimestamp);
        Assert.Equal(["First", "Second"], snapshot.Itinerary.Select(activity => activity.Name));
        Assert.Equal("original", snapshot.Itinerary[0].Arguments["input"]);
        Assert.Equal("north", snapshot.Variables["tenant"]);
        Assert.Empty(transport.Published);
    }

    [Theory]
    [InlineData(ResolutionOutcome.NullTask)]
    [InlineData(ResolutionOutcome.NullEndpoint)]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "deep-executor-null-resolution-task-and-endpoint-have-explicit-diagnostics")]
    public async Task ExecuteAsync_RejectsNullResolutionArtifactsWithAddressedDiagnosticsAsync(ResolutionOutcome outcome)
    {
        var transport = new RecordingTransport { Outcome = outcome };
        var executor = new RoutingSlipExecutor(transport, transport, new FakeTimeProvider(CreatedAt));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync(ValidSlip(active: true), TestContext.Current.CancellationToken));

        Assert.Contains(ExecuteAddress.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            outcome == ResolutionOutcome.NullTask ? "resolution task" : "resolved no send endpoint",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Single(transport.Resolutions);
        Assert.Empty(transport.Sent);
        Assert.Empty(transport.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "deep-executor-rechecks-cancellation-after-resolution-before-send")]
    public async Task ExecuteAsync_RechecksCancellationAfterResolutionBeforeCallingSendAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var transport = new RecordingTransport
        {
            ResolutionCompleted = () => cancellation.Cancel(),
        };
        var executor = new RoutingSlipExecutor(transport, transport, new FakeTimeProvider(CreatedAt));

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.ExecuteAsync(ValidSlip(active: true), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Single(transport.Resolutions);
        Assert.Empty(transport.Sent);
        Assert.Empty(transport.Published);
    }

    [Theory]
    [InlineData(ResolutionOutcome.ThrowSynchronously)]
    [InlineData(ResolutionOutcome.FaultedTask)]
    [InlineData(ResolutionOutcome.SendFailure)]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "deep-executor-preserves-resolution-and-send-failures-without-later-effects")]
    public async Task ExecuteAsync_PreservesTheExactTransportFailureWithoutLaterEffectsAsync(ResolutionOutcome outcome)
    {
        var transport = new RecordingTransport { Outcome = outcome };
        var executor = new RoutingSlipExecutor(transport, transport, new FakeTimeProvider(CreatedAt));

        ExpectedTransportException exception = await Assert.ThrowsAsync<ExpectedTransportException>(() =>
            executor.ExecuteAsync(ValidSlip(active: true), TestContext.Current.CancellationToken));

        Assert.Same(transport.Failure, exception);
        Assert.Single(transport.Resolutions);
        Assert.Equal(outcome == ResolutionOutcome.SendFailure ? 1 : 0, transport.SendCallCount);
        Assert.Empty(transport.Sent);
        Assert.Empty(transport.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "deep-executor-completion-routes-to-subscription-without-topology-duplication")]
    public async Task ExecuteAsync_CompletedSlipRoutesItsSnapshotToTheSelectedSubscriptionOnlyAsync()
    {
        Guid trackingNumber = NewId.NextGuid();
        var subscription = new MutableSubscription
        {
            Address = SubscriptionAddress,
            Events = RoutingSlipEvents.Completed,
            Include = RoutingSlipEventContents.Variables,
        };
        MutableRoutingSlip source = ValidSlip() with
        {
            TrackingNumber = trackingNumber,
            Variables = new Dictionary<string, object> { ["tenant"] = "north" },
            Subscriptions = [subscription],
        };
        var transport = new RecordingTransport();
        var clock = new FakeTimeProvider(CreatedAt.AddMinutes(4));
        var executor = new RoutingSlipExecutor(transport, transport, clock);

        await executor.ExecuteAsync(source, TestContext.Current.CancellationToken);
        subscription.Address = new Uri("loopback://localhost/mutated-subscription");

        Assert.Equal(SubscriptionAddress, Assert.Single(transport.Resolutions).Address);
        Sent sent = Assert.Single(transport.Sent);
        IRoutingSlipCompleted completed = Assert.IsAssignableFrom<IRoutingSlipCompleted>(sent.Message);
        Assert.Equal(trackingNumber, completed.TrackingNumber);
        Assert.Equal(CreatedAt.AddMinutes(4), completed.Timestamp);
        Assert.Equal(TimeSpan.FromMinutes(4), completed.Duration);
        Assert.Equal("north", completed.Variables["TENANT"]);
        Assert.Empty(transport.Published);
    }

    private static MutableRoutingSlip ValidSlip(bool active = false) => new()
    {
        TrackingNumber = NewId.NextGuid(),
        CreateTimestamp = CreatedAt,
        Itinerary = active
            ? [new MutableActivity("First", ExecuteAddress, new Dictionary<string, object>())]
            : [],
    };

    private static void AssertNoTransport(RecordingTransport transport)
    {
        Assert.Empty(transport.Resolutions);
        Assert.Empty(transport.Sent);
        Assert.Empty(transport.Published);
    }

    public enum ResolutionOutcome
    {
        Success,
        NullTask,
        NullEndpoint,
        ThrowSynchronously,
        FaultedTask,
        SendFailure,
    }

    private sealed record MutableRoutingSlip : IRoutingSlip
    {
        public Guid TrackingNumber { get; init; }
        public DateTimeOffset CreateTimestamp { get; init; }
        public IReadOnlyList<IActivity> Itinerary { get; init; } = [];
        public IReadOnlyList<IActivityLog> ActivityLogs { get; init; } = [];
        public IReadOnlyList<ICompensateLog> CompensateLogs { get; init; } = [];
        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
        public IReadOnlyList<IActivityException> ActivityExceptions { get; init; } = [];
        public IReadOnlyList<ISubscription> Subscriptions { get; init; } = [];
    }

    private sealed class MutableActivity(string name, Uri address, IReadOnlyDictionary<string, object> arguments) : IActivity
    {
        public string Name { get; } = name;
        public Uri Address { get; } = address;
        public IReadOnlyDictionary<string, object> Arguments { get; } = arguments;
    }

    private sealed class MutableSubscription : ISubscription
    {
        public Uri Address { get; set; } = SubscriptionAddress;
        public RoutingSlipEvents Events { get; set; }
        public RoutingSlipEventContents Include { get; set; }
        public string? ActivityName { get; set; }
        public MessageEnvelope? Message { get; set; }
    }

    private sealed class CountingTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public int ReadCount { get; private set; }

        public override DateTimeOffset GetUtcNow()
        {
            ReadCount++;
            return now;
        }
    }

    private sealed class ExpectedTransportException : Exception;

    private sealed record Resolution(Uri Address, CancellationToken CancellationToken);

    private sealed record Sent(object Message, CancellationToken CancellationToken);

    private sealed class RecordingTransport : ISendEndpointProvider, ISendEndpoint, IPublishEndpoint
    {
        public ExpectedTransportException Failure { get; } = new();
        public List<object> Published { get; } = [];
        public List<Resolution> Resolutions { get; } = [];
        public Action? ResolutionCompleted { get; init; }
        public ResolutionOutcome Outcome { get; init; }
        public int SendCallCount { get; private set; }
        public List<Sent> Sent { get; } = [];

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            Resolutions.Add(new Resolution(address, cancellationToken));
            if (Outcome == ResolutionOutcome.ThrowSynchronously)
                throw Failure;
            if (Outcome == ResolutionOutcome.NullTask)
                return null!;
            if (Outcome == ResolutionOutcome.FaultedTask)
                return Task.FromException<ISendEndpoint>(Failure);

            ResolutionCompleted?.Invoke();
            return Outcome == ResolutionOutcome.NullEndpoint
                ? Task.FromResult<ISendEndpoint>(null!)
                : Task.FromResult<ISendEndpoint>(this);
        }

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            SendCallCount++;
            if (Outcome == ResolutionOutcome.SendFailure)
                return Task.FromException(Failure);

            Sent.Add(new Sent(message, cancellationToken));
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
