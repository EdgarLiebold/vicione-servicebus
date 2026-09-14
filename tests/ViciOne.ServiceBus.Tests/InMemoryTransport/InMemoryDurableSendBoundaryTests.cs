using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.InMemoryTransport.DurableSend;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryDurableSendBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-COMPLETION", "default-dispatch-context-rejected-at-owner")]
    public async Task Dispatch_RejectsADefaultContextBeforeResolvingCollaboratorsAsync()
    {
        ITestBus bus = CreateBus((_, _) => throw new UnexpectedEndpointResolutionException());
        var dispatcher = new InMemoryDurableSendDispatcher<ITestBus>(bus, []);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            dispatcher.DispatchAsync(default, TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-COMPLETION", "catalog-success-requires-message-type")]
    public async Task Dispatch_RejectsASuccessfulNullCatalogResultBeforeEndpointResolutionAsync()
    {
        var endpointResolutionCount = 0;
        ITestBus bus = CreateBus((_, _) =>
        {
            Interlocked.Increment(ref endpointResolutionCount);
            throw new UnexpectedEndpointResolutionException();
        });
        var dispatcher = new InMemoryDurableSendDispatcher<ITestBus>(
            bus,
            [new ControlledMessageContractCatalog(found: true, messageType: null)]);

        MessageContractException exception = await Assert.ThrowsAsync<MessageContractException>(() =>
            dispatcher.DispatchAsync(CreateContext(), TestContext.Current.CancellationToken));

        Assert.Contains("returned no message type", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, Volatile.Read(ref endpointResolutionCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-COMPLETION", "unknown-contract-rejected-before-endpoint-resolution")]
    public async Task Dispatch_RejectsAnUnknownContractBeforeEndpointResolutionAsync()
    {
        var endpointResolutionCount = 0;
        ITestBus bus = CreateBus((_, _) =>
        {
            Interlocked.Increment(ref endpointResolutionCount);
            throw new UnexpectedEndpointResolutionException();
        });
        var dispatcher = new InMemoryDurableSendDispatcher<ITestBus>(
            bus,
            [new ControlledMessageContractCatalog(found: false, messageType: null)]);

        MessageContractException exception = await Assert.ThrowsAsync<MessageContractException>(() =>
            dispatcher.DispatchAsync(CreateContext(), TestContext.Current.CancellationToken));

        Assert.Contains("is not registered", exception.Message, StringComparison.Ordinal);
        Assert.Contains(ContractIdentity.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, Volatile.Read(ref endpointResolutionCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-COMPLETION", "endpoint-provider-null-task-rejected-at-owner")]
    public async Task Dispatch_RejectsANullEndpointResolutionTaskAsync()
    {
        ITestBus bus = CreateBus((_, _) => null!);
        var dispatcher = CreateDispatcher(bus);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(CreateContext(), TestContext.Current.CancellationToken));

        Assert.Contains("null task", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-COMPLETION", "endpoint-provider-null-result-rejected-at-owner")]
    public async Task Dispatch_RejectsANullEndpointResolutionResultAsync()
    {
        ITestBus bus = CreateBus((_, _) => Task.FromResult<ISendEndpoint>(null!));
        var dispatcher = CreateDispatcher(bus);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(CreateContext(), TestContext.Current.CancellationToken));

        Assert.Contains("null endpoint", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-COMPLETION", "pre-cancellation-precedes-context-validation")]
    public async Task Dispatch_PreCancellationPreservesTheCallerTokenAndSkipsCollaboratorsAsync()
    {
        ITestBus bus = CreateBus((_, _) => throw new UnexpectedEndpointResolutionException());
        var dispatcher = new InMemoryDurableSendDispatcher<ITestBus>(bus, []);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            dispatcher.DispatchAsync(default, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    private static InMemoryDurableSendDispatcher<ITestBus> CreateDispatcher(ITestBus bus) =>
        new(bus, [new ControlledMessageContractCatalog(found: true, typeof(BoundaryMessage))]);

    private static DurableSendDispatchContext CreateContext()
    {
        var id = new DurableSendId(Guid.Parse("04a873ff-3279-4e3f-bb10-836f79d8bc47"));
        var message = new SerializedDurableSend
        {
            Id = id,
            ContractIdentity = ContractIdentity,
            DestinationAddress = new Uri("loopback://localhost/durable-boundary"),
            ContentType = "application/json",
            Body = ReadOnlyMemory<byte>.Empty,
            Metadata = ReadOnlyMemory<byte>.Empty,
        };

        return new DurableSendDispatchContext(message, id, 1, new ControlledConsumerCompletion(id));
    }

    private static ITestBus CreateBus(Func<Uri, CancellationToken, Task<ISendEndpoint>> resolve)
    {
        ITestBus bus = DispatchProxy.Create<ITestBus, BusProxy>();
        ((BusProxy)(object)bus).Resolve = resolve;
        return bus;
    }

    private static readonly MessageContractIdentity ContractIdentity = new("vicione.tests.inmemory-boundary", 1);

    public interface ITestBus : IBus;

    public class BusProxy : DispatchProxy
    {
        public Func<Uri, CancellationToken, Task<ISendEndpoint>> Resolve { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(ISendEndpointProvider.GetSendEndpointAsync))
            {
                return Resolve(
                    Assert.IsType<Uri>(args![0]),
                    Assert.IsType<CancellationToken>(args[1]));
            }

            throw new InvalidOperationException($"Unexpected bus member: {targetMethod.Name}.");
        }
    }

    private sealed class ControlledMessageContractCatalog(bool found, Type? messageType) : IMessageContractCatalog
    {
        public MessageContractIdentity GetIdentity(Type messageType) => throw new NotSupportedException();

        public bool TryGetIdentity(Type messageType, out MessageContractIdentity identity)
        {
            identity = default;
            throw new NotSupportedException();
        }

        public Type GetMessageType(MessageContractIdentity identity) => throw new NotSupportedException();

        public bool TryGetMessageType(MessageContractIdentity identity, [NotNullWhen(true)] out Type? resolvedType)
        {
            resolvedType = messageType;
            return found;
        }
    }

    private sealed class ControlledConsumerCompletion(DurableSendId durableSendId) : IDurableSendConsumerCompletion
    {
        public DurableSendId DurableSendId { get; } = durableSendId;

        public ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(true);
    }

    private sealed record BoundaryMessage;

    private sealed class UnexpectedEndpointResolutionException : Exception;
}
