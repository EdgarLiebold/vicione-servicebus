using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestExtensionsBoundaryTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/request-boundary");

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-BOUNDARY", "advanced-request-overloads-reject-null-inputs")]
    public async Task EveryRequestOverload_RejectsItsRequiredInputsBeforeUsingTheBusAsync()
    {
        IBus bus = DispatchProxy.Create<IBus, UnexpectedInvocationProxy>();
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, UnexpectedInvocationProxy>();
        var message = new BoundaryRequest();

        await AssertParameterAsync("bus", () =>
            RequestExtensions.RequestAsync<BoundaryRequest, BoundaryResponse>(null!, DestinationAddress, message));
        await AssertParameterAsync("destinationAddress", () =>
            bus.RequestAsync<BoundaryRequest, BoundaryResponse>(null!, message));
        await AssertParameterAsync("message", () =>
            bus.RequestAsync<BoundaryRequest, BoundaryResponse>(DestinationAddress, (BoundaryRequest)null!));
        await AssertParameterAsync("values", () =>
            bus.RequestAsync<BoundaryRequest, BoundaryResponse>(DestinationAddress, (object)null!));
        await AssertParameterAsync("message", () =>
            bus.RequestAsync<BoundaryRequest, BoundaryResponse>((BoundaryRequest)null!));
        await AssertParameterAsync("values", () =>
            bus.RequestAsync<BoundaryRequest, BoundaryResponse>((object)null!));

        await AssertParameterAsync("consumeContext", () =>
            RequestExtensions.RequestAsync<BoundaryRequest, BoundaryResponse>(null!, bus, DestinationAddress, message));
        await AssertParameterAsync("bus", () =>
            consumeContext.RequestAsync<BoundaryRequest, BoundaryResponse>(null!, DestinationAddress, message));
        await AssertParameterAsync("destinationAddress", () =>
            consumeContext.RequestAsync<BoundaryRequest, BoundaryResponse>(bus, null!, message));
        await AssertParameterAsync("message", () =>
            consumeContext.RequestAsync<BoundaryRequest, BoundaryResponse>(bus, DestinationAddress, (BoundaryRequest)null!));
        await AssertParameterAsync("values", () =>
            consumeContext.RequestAsync<BoundaryRequest, BoundaryResponse>(bus, DestinationAddress, (object)null!));
        await AssertParameterAsync("message", () =>
            consumeContext.RequestAsync<BoundaryRequest, BoundaryResponse>(bus, (BoundaryRequest)null!));
        await AssertParameterAsync("values", () =>
            consumeContext.RequestAsync<BoundaryRequest, BoundaryResponse>(bus, (object)null!));
    }

    private static async Task AssertParameterAsync(string expectedParameterName, Func<Task> operation)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(operation);

        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    private sealed record BoundaryRequest;

    private sealed record BoundaryResponse;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The null boundary invoked {targetMethod?.Name}.");
    }
}
