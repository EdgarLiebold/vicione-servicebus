using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class ForwardExtensionsBoundaryTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/forward-boundary");

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "every-overload-validates-required-inputs")]
    public async Task EveryForwardOverload_RejectsRequiredInputsBeforeUsingItsDependenciesAsync()
    {
        ConsumeContext<ForwardMessage> typedContext =
            DispatchProxy.Create<ConsumeContext<ForwardMessage>, UnexpectedInvocationProxy>();
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, UnexpectedInvocationProxy>();
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        IPipe<SendContext<ForwardMessage>> typedPipe =
            DispatchProxy.Create<IPipe<SendContext<ForwardMessage>>, UnexpectedInvocationProxy>();
        var message = new ForwardMessage();

        await AssertParameterAsync("context", () =>
            ForwardExtensions.ForwardAsync<ForwardMessage>(null!, DestinationAddress));
        await AssertParameterAsync("address", () => typedContext.ForwardAsync((Uri)null!));

        await AssertParameterAsync("context", () =>
            ForwardExtensions.ForwardAsync<ForwardMessage>(null!, DestinationAddress, typedPipe));
        await AssertParameterAsync("address", () => typedContext.ForwardAsync((Uri)null!, typedPipe));
        await AssertParameterAsync("pipe", () => typedContext.ForwardAsync(DestinationAddress, null!));

        await AssertParameterAsync("context", () =>
            ForwardExtensions.ForwardAsync<ForwardMessage>(null!, endpoint));
        await AssertParameterAsync("endpoint", () => typedContext.ForwardAsync((ISendEndpoint)null!));

        await AssertParameterAsync("context", () =>
            ForwardExtensions.ForwardAsync<ForwardMessage>(null!, endpoint, typedPipe));
        await AssertParameterAsync("endpoint", () => typedContext.ForwardAsync((ISendEndpoint)null!, typedPipe));
        await AssertParameterAsync("pipe", () => typedContext.ForwardAsync(endpoint, null!));

        await AssertParameterAsync("context", () =>
            ForwardExtensions.ForwardAsync(null!, DestinationAddress, message));
        await AssertParameterAsync("address", () => context.ForwardAsync((Uri)null!, message));
        await AssertParameterAsync("message", () => context.ForwardAsync<ForwardMessage>(DestinationAddress, null!));

        await AssertParameterAsync("context", () =>
            ForwardExtensions.ForwardAsync(null!, endpoint, message));
        await AssertParameterAsync("endpoint", () => context.ForwardAsync((ISendEndpoint)null!, message));
        await AssertParameterAsync("message", () => context.ForwardAsync<ForwardMessage>(endpoint, null!));
    }

    private static async Task AssertParameterAsync(string expectedParameterName, Func<Task> operation)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(operation);
        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    private sealed record ForwardMessage;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The forwarding boundary invoked {targetMethod?.Name}.");
    }
}
