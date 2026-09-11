using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class EndpointProviderTests
{
    private static readonly Uri HostAddress = new("loopback://localhost/");
    private static readonly Uri PublishAddress = new("loopback://localhost/messages/sample");
    private static readonly Uri SendAddress = new("loopback://localhost/queues/orders");

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-ENDPOINT-PROVIDER", "creation-token-cache-and-owned-release")]
    public async Task PublishEndpoint_CachesByMessageTypeAndOwnsTheCreatedTransportAsync()
    {
        var transport = new TrackingSendTransport();
        var transportProvider = new RecordingPublishTransportProvider((_, _) => Task.FromResult<ISendTransport>(transport));
        var provider = CreatePublishProvider(transportProvider, new ScriptedMessageTopology((true, PublishAddress)));
        using var creationCancellation = new CancellationTokenSource();

        ISendEndpoint first = await provider.GetPublishSendEndpointAsync<SampleMessage>(creationCancellation.Token);
        ISendEndpoint second = await provider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        Assert.Equal(1, transportProvider.InvocationCount);
        Assert.Equal(PublishAddress, transportProvider.ObservedAddress);
        Assert.NotEqual(creationCancellation.Token, transportProvider.ObservedCancellationToken);
        Assert.True(transportProvider.ObservedCancellationToken.CanBeCanceled);
        Assert.Equal(1, transport.ConnectCount);

        await provider.DisposeAsync();
        Assert.Equal(1, transport.Connection.DisconnectCount);
        Assert.Equal(1, transport.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-PROVIDER", "creation-token-normalization-cache-and-owned-release")]
    public async Task SendEndpoint_NormalizesOnceAndForwardsTheCreationTokenAsync()
    {
        var transport = new TrackingSendTransport();
        var transportProvider = new RecordingSendTransportProvider(transport);
        var provider = new SendEndpointProvider(
            transportProvider,
            new SendObservable(),
            CreateReceiveEndpointContext(),
            new NoOpSendPipe());
        using var creationCancellation = new CancellationTokenSource();

        ISendEndpoint first = await provider.GetSendEndpointAsync(SendAddress, creationCancellation.Token);
        ISendEndpoint second = await provider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        Assert.Equal(2, transportProvider.NormalizationCount);
        Assert.Equal(1, transportProvider.AcquisitionCount);
        Assert.NotEqual(creationCancellation.Token, transportProvider.ObservedCancellationToken);
        Assert.True(transportProvider.ObservedCancellationToken.CanBeCanceled);

        await provider.DisposeAsync();
        Assert.Equal(1, transport.Connection.DisconnectCount);
        Assert.Equal(1, transport.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-ENDPOINT-PROVIDER", "missing-address-does-not-poison-cache")]
    public async Task MissingPublishAddress_DoesNotPoisonTheCacheForAHealthyRetryAsync()
    {
        var transport = new TrackingSendTransport();
        var transportProvider = new RecordingPublishTransportProvider((_, _) => Task.FromResult<ISendTransport>(transport));
        var topology = new ScriptedMessageTopology((false, null), (true, PublishAddress));
        var provider = CreatePublishProvider(transportProvider, topology);

        await Assert.ThrowsAsync<PublishException>(() =>
            provider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken));
        ISendEndpoint endpoint = await provider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken);

        Assert.NotNull(endpoint);
        Assert.Equal(2, topology.InvocationCount);
        Assert.Equal(1, transportProvider.InvocationCount);
        await provider.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-PROVIDER-CONTRACT", "null-provider-results-are-normalized-and-retryable")]
    public async Task ProviderContractViolations_AreNormalizedAndDoNotPoisonTheCacheAsync()
    {
        var recoveredAfterMissingTask = new TrackingSendTransport();
        var missingTaskAttempts = 0;
        var missingTaskProvider = new RecordingPublishTransportProvider((_, _) =>
            ++missingTaskAttempts == 1 ? null! : Task.FromResult<ISendTransport>(recoveredAfterMissingTask));
        var missingTaskEndpointProvider = CreatePublishProvider(
            missingTaskProvider,
            new ScriptedMessageTopology((true, PublishAddress)));
        InvalidOperationException missingTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            missingTaskEndpointProvider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken));
        Assert.Equal("The publish transport provider returned no acquisition task.", missingTask.Message);
        Assert.NotNull(await missingTaskEndpointProvider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken));
        Assert.Equal(2, missingTaskProvider.InvocationCount);
        await missingTaskEndpointProvider.DisposeAsync();
        Assert.Equal(1, recoveredAfterMissingTask.DisposeCount);

        var recoveredAfterMissingTransport = new TrackingSendTransport();
        var missingTransportAttempts = 0;
        var missingTransportProvider = new RecordingPublishTransportProvider((_, _) => Task.FromResult<ISendTransport>(
            ++missingTransportAttempts == 1 ? null! : recoveredAfterMissingTransport));
        var missingTransportEndpointProvider = CreatePublishProvider(
            missingTransportProvider,
            new ScriptedMessageTopology((true, PublishAddress)));
        InvalidOperationException missingTransport = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            missingTransportEndpointProvider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken));
        Assert.Contains("returned no send transport", missingTransport.Message, StringComparison.Ordinal);
        Assert.NotNull(await missingTransportEndpointProvider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken));
        Assert.Equal(2, missingTransportProvider.InvocationCount);
        await missingTransportEndpointProvider.DisposeAsync();
        Assert.Equal(1, recoveredAfterMissingTransport.DisposeCount);

        var invalidConnectionTransport = new TrackingSendTransport(returnNullConnection: true);
        var recoveredAfterMissingConnection = new TrackingSendTransport();
        var connectionAttempts = 0;
        var recoveringProvider = new RecordingPublishTransportProvider(
            (_, _) => Task.FromResult<ISendTransport>(
                ++connectionAttempts == 1 ? invalidConnectionTransport : recoveredAfterMissingConnection));
        var recoveringEndpointProvider = CreatePublishProvider(
            recoveringProvider,
            new ScriptedMessageTopology((true, PublishAddress)));
        InvalidOperationException missingConnection = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            recoveringEndpointProvider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken));

        Assert.Equal("The publish transport returned no observer connection handle.", missingConnection.Message);
        Assert.Equal(1, invalidConnectionTransport.DisposeCount);
        Assert.NotNull(await recoveringEndpointProvider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken));
        Assert.Equal(2, recoveringProvider.InvocationCount);
        await recoveringEndpointProvider.DisposeAsync();
        Assert.Equal(1, recoveredAfterMissingConnection.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-PROVIDER", "invalid-provider-contracts-are-retryable")]
    public async Task SendProviderContractViolations_AreNormalizedAndDoNotPoisonTheCacheAsync()
    {
        var invalidNormalizationProvider = new RecordingSendTransportProvider(
            (_, _) => throw new UnreachableException(),
            _ => null!);
        var invalidNormalizationEndpointProvider = CreateSendProvider(invalidNormalizationProvider);
        InvalidOperationException missingNormalizedAddress = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invalidNormalizationEndpointProvider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken));
        Assert.Equal("The send transport provider returned no normalized address.", missingNormalizedAddress.Message);
        Assert.Equal(0, invalidNormalizationProvider.AcquisitionCount);

        var recoveredAfterMissingTask = new TrackingSendTransport();
        var missingTaskAttempts = 0;
        var missingTaskProvider = new RecordingSendTransportProvider((_, _) =>
            ++missingTaskAttempts == 1 ? null! : Task.FromResult<ISendTransport>(recoveredAfterMissingTask));
        var missingTaskEndpointProvider = CreateSendProvider(missingTaskProvider);
        InvalidOperationException missingTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            missingTaskEndpointProvider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken));
        Assert.Equal("The transport provider returned no acquisition task.", missingTask.Message);
        Assert.NotNull(await missingTaskEndpointProvider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken));
        Assert.Equal(2, missingTaskProvider.AcquisitionCount);
        await missingTaskEndpointProvider.DisposeAsync();
        Assert.Equal(1, recoveredAfterMissingTask.DisposeCount);

        var recoveredAfterMissingTransport = new TrackingSendTransport();
        var missingTransportAttempts = 0;
        var missingTransportProvider = new RecordingSendTransportProvider((_, _) => Task.FromResult<ISendTransport>(
            ++missingTransportAttempts == 1 ? null! : recoveredAfterMissingTransport));
        var missingTransportEndpointProvider = CreateSendProvider(missingTransportProvider);
        InvalidOperationException missingTransport = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            missingTransportEndpointProvider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken));
        Assert.Contains("returned no send transport", missingTransport.Message, StringComparison.Ordinal);
        Assert.NotNull(await missingTransportEndpointProvider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken));
        Assert.Equal(2, missingTransportProvider.AcquisitionCount);
        await missingTransportEndpointProvider.DisposeAsync();
        Assert.Equal(1, recoveredAfterMissingTransport.DisposeCount);

        var invalidConnectionTransport = new TrackingSendTransport(returnNullConnection: true);
        var recoveredAfterMissingConnection = new TrackingSendTransport();
        var connectionAttempts = 0;
        var missingConnectionProvider = new RecordingSendTransportProvider((_, _) => Task.FromResult<ISendTransport>(
            ++connectionAttempts == 1 ? invalidConnectionTransport : recoveredAfterMissingConnection));
        var missingConnectionEndpointProvider = CreateSendProvider(missingConnectionProvider);
        InvalidOperationException missingConnection = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            missingConnectionEndpointProvider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken));
        Assert.Equal("The send transport returned no observer connection handle.", missingConnection.Message);
        Assert.Equal(1, invalidConnectionTransport.DisposeCount);
        Assert.NotNull(await missingConnectionEndpointProvider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken));
        Assert.Equal(2, missingConnectionProvider.AcquisitionCount);
        await missingConnectionEndpointProvider.DisposeAsync();
        Assert.Equal(1, recoveredAfterMissingConnection.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-ENDPOINT-PROVIDER", "failed-creation-releases-all-owned-resources")]
    public async Task PublishEndpointCreationFailure_ReleasesEveryResourceAndPreservesAllFailuresAsync()
    {
        var disconnectFailure = new ExpectedDisconnectException();
        var disposalFailure = new ExpectedDisposalException();
        var transport = new TrackingSendTransport(
            disposalFailure: disposalFailure,
            disconnectFailure: disconnectFailure);
        var transportProvider = new RecordingPublishTransportProvider((_, _) => Task.FromResult<ISendTransport>(transport));
        var provider = CreatePublishProvider(
            transportProvider,
            new ScriptedMessageTopology((true, PublishAddress)),
            CreateReceiveEndpointContext(returnNullSerializer: true));

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() =>
            provider.GetPublishSendEndpointAsync<SampleMessage>(TestContext.Current.CancellationToken));

        Assert.Collection(
            actual.InnerExceptions,
            failure => Assert.IsType<InvalidOperationException>(failure),
            failure => Assert.Same(disconnectFailure, failure),
            failure => Assert.Same(disposalFailure, failure));
        Assert.Equal(1, transport.Connection.DisconnectCount);
        Assert.Equal(1, transport.DisposeCount);
        await provider.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-PROVIDER", "failed-creation-releases-all-owned-resources")]
    public async Task SendEndpointCreationFailure_ReleasesEveryResourceAndPreservesAllFailuresAsync()
    {
        var disconnectFailure = new ExpectedDisconnectException();
        var disposalFailure = new ExpectedDisposalException();
        var transport = new TrackingSendTransport(
            disposalFailure: disposalFailure,
            disconnectFailure: disconnectFailure);
        var provider = new SendEndpointProvider(
            new RecordingSendTransportProvider(transport),
            new SendObservable(),
            CreateReceiveEndpointContext(returnNullSerializer: true),
            new NoOpSendPipe());

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() =>
            provider.GetSendEndpointAsync(SendAddress, TestContext.Current.CancellationToken));

        Assert.Collection(
            actual.InnerExceptions,
            failure => Assert.IsType<InvalidOperationException>(failure),
            failure => Assert.Same(disconnectFailure, failure),
            failure => Assert.Same(disposalFailure, failure));
        Assert.Equal(1, transport.Connection.DisconnectCount);
        Assert.Equal(1, transport.DisposeCount);
        await provider.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-LIFETIME", "observer-and-transport-release-failures-are-preserved")]
    public async Task DisposeAsync_ReleasesTheTransportWhenObserverDisconnectFailsAndPreservesBothFailuresAsync()
    {
        var disconnectFailure = new ExpectedDisconnectException();
        var disposalFailure = new ExpectedDisposalException();
        var transport = new TrackingSendTransport(disposalFailure: disposalFailure);
        var connection = new TrackingConnectHandle(disconnectFailure);
        var endpoint = new SendEndpoint(
            transport,
            CreateReceiveEndpointContext(),
            SendAddress,
            new NoOpSendPipe(),
            connection);

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => endpoint.DisposeAsync().AsTask());

        Assert.Collection(
            actual.InnerExceptions,
            failure => Assert.Same(disconnectFailure, failure),
            failure => Assert.Same(disposalFailure, failure));
        Assert.Equal(1, connection.DisconnectCount);
        Assert.Equal(1, transport.DisposeCount);

        await Assert.ThrowsAsync<AggregateException>(() => endpoint.DisposeAsync().AsTask());
        Assert.Equal(1, connection.DisconnectCount);
        Assert.Equal(1, transport.DisposeCount);
    }

    private static PublishEndpointProvider CreatePublishProvider(
        IPublishTransportProvider transportProvider,
        IMessagePublishTopology<SampleMessage> messageTopology,
        ReceiveEndpointContext? context = null)
    {
        IPublishTopology topology = CreateProxy<IPublishTopology>((method, _) => method.Name switch
        {
            nameof(IPublishTopology.GetMessageTopology) when method.IsGenericMethod => messageTopology,
            _ => throw new NotSupportedException(method.Name),
        });

        return new PublishEndpointProvider(
            transportProvider,
            HostAddress,
            new PublishObservable(),
            context ?? CreateReceiveEndpointContext(),
            new NoOpPublishPipe(),
            topology);
    }

    private static SendEndpointProvider CreateSendProvider(ISendTransportProvider transportProvider)
    {
        return new SendEndpointProvider(
            transportProvider,
            new SendObservable(),
            CreateReceiveEndpointContext(),
            new NoOpSendPipe());
    }

    private static ReceiveEndpointContext CreateReceiveEndpointContext(bool returnNullSerializer = false)
    {
        IMessageSerializer serializer = CreateProxy<IMessageSerializer>((method, _) => throw new NotSupportedException(method.Name));
        ISerialization serialization = CreateProxy<ISerialization>((method, _) => method.Name switch
        {
            nameof(ISerialization.GetMessageSerializer) => returnNullSerializer ? null : serializer,
            $"get_{nameof(ISerialization.DefaultContentType)}" => new ContentType("application/json"),
            _ => throw new NotSupportedException(method.Name),
        });

        return CreateProxy<ReceiveEndpointContext>((method, _) => method.Name switch
        {
            $"get_{nameof(ReceiveEndpointContext.InputAddress)}" => new Uri(HostAddress, "input"),
            $"get_{nameof(ReceiveEndpointContext.Serialization)}" => serialization,
            _ => throw new NotSupportedException(method.Name),
        });
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> invoke)
        where T : class
    {
        T contract = DispatchProxy.Create<T, ContractProxy>();
        ((ContractProxy)(object)contract).InvokeMember = invoke ?? throw new ArgumentNullException(nameof(invoke));
        return contract;
    }

    private sealed class ScriptedMessageTopology(params (bool Found, Uri? Address)[] outcomes) :
        IMessagePublishTopology<SampleMessage>
    {
        private readonly Queue<(bool Found, Uri? Address)> _outcomes = new(outcomes);

        public int InvocationCount { get; private set; }
        public bool Exclude => false;

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            ArgumentNullException.ThrowIfNull(baseAddress);
            InvocationCount++;
            (bool found, Uri? address) = _outcomes.Count > 1 ? _outcomes.Dequeue() : _outcomes.Peek();
            publishAddress = address;
            return found;
        }

        public void Apply(ITopologyPipeBuilder<PublishContext<SampleMessage>> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
        }
    }

    private sealed class RecordingPublishTransportProvider(
        Func<Uri?, CancellationToken, Task<ISendTransport>> acquire) : IPublishTransportProvider
    {
        private readonly Func<Uri?, CancellationToken, Task<ISendTransport>> _acquire =
            acquire ?? throw new ArgumentNullException(nameof(acquire));

        public int InvocationCount { get; private set; }
        public Uri? ObservedAddress { get; private set; }
        public CancellationToken ObservedCancellationToken { get; private set; }

        public Task<ISendTransport> GetPublishTransportAsync<T>(
            Uri? publishAddress,
            CancellationToken cancellationToken = default)
            where T : class
        {
            InvocationCount++;
            ObservedAddress = publishAddress;
            ObservedCancellationToken = cancellationToken;
            return _acquire(publishAddress, cancellationToken);
        }
    }

    private sealed class RecordingSendTransportProvider : ISendTransportProvider
    {
        private readonly Func<Uri, CancellationToken, Task<ISendTransport>> _acquire;
        private readonly Func<Uri, Uri> _normalize;

        public RecordingSendTransportProvider(TrackingSendTransport transport)
            : this((_, _) => Task.FromResult<ISendTransport>(transport))
        {
            ArgumentNullException.ThrowIfNull(transport);
        }

        public RecordingSendTransportProvider(
            Func<Uri, CancellationToken, Task<ISendTransport>> acquire,
            Func<Uri, Uri>? normalize = null)
        {
            _acquire = acquire ?? throw new ArgumentNullException(nameof(acquire));
            _normalize = normalize ?? (address => address);
        }

        public int AcquisitionCount { get; private set; }
        public int NormalizationCount { get; private set; }
        public CancellationToken ObservedCancellationToken { get; private set; }

        public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            AcquisitionCount++;
            ObservedCancellationToken = cancellationToken;
            return _acquire(address, cancellationToken);
        }

        public Uri NormalizeAddress(Uri address)
        {
            ArgumentNullException.ThrowIfNull(address);
            NormalizationCount++;
            return _normalize(address);
        }
    }

    private sealed class TrackingSendTransport(
        bool returnNullConnection = false,
        Exception? disposalFailure = null,
        Exception? disconnectFailure = null) : ISendTransport, IAsyncDisposable
    {
        public TrackingConnectHandle Connection { get; } = new(disconnectFailure);
        public int ConnectCount { get; private set; }
        public int DisposeCount { get; private set; }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            ConnectCount++;
            return returnNullConnection ? null! : Connection;
        }

        public Task<SendContext<T>> CreateSendContextAsync<T>(
            T message,
            IPipe<SendContext<T>> pipe,
            CancellationToken cancellationToken)
            where T : class => throw new NotSupportedException();

        public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
            where T : class => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return disposalFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(disposalFailure);
        }
    }

    private sealed class TrackingConnectHandle(Exception? disconnectFailure = null) : ConnectHandle
    {
        public int DisconnectCount { get; private set; }

        public void Disconnect()
        {
            DisconnectCount++;
            if (disconnectFailure is not null)
                throw disconnectFailure;
        }

        public void Dispose() => Disconnect();
    }

    private sealed class NoOpSendPipe : ISendPipe
    {
        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
            where T : class => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }

    private sealed class NoOpPublishPipe : IPublishPipe
    {
        public Task SendAsync<T>(PublishContext<T> context, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }

    private class ContractProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> InvokeMember { get; set; } =
            (method, _) => throw new NotSupportedException(method.Name);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return InvokeMember(targetMethod, args);
        }
    }

    private sealed class SampleMessage;
    private sealed class ExpectedDisconnectException : Exception;
    private sealed class ExpectedDisposalException : Exception;
}
