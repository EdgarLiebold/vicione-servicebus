using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class AdvancedEndpointExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-ENDPOINT", "capability-validation")]
    public void Advanced_RejectsMissingAndUnsupportedEndpoints()
    {
        Assert.Equal(
            "endpoint",
            Assert.Throws<ArgumentNullException>(() => AdvancedSendEndpointExtensions.Advanced(null!)).ParamName);
        Assert.Equal(
            "endpoint",
            Assert.Throws<ArgumentNullException>(() => AdvancedPublishEndpointExtensions.Advanced(null!)).ParamName);

        ISendEndpoint basicSendEndpoint = CreateProxy<ISendEndpoint>(out _);
        IPublishEndpoint basicPublishEndpoint = CreateProxy<IPublishEndpoint>(out _);

        Assert.Throws<NotSupportedException>(() => basicSendEndpoint.Advanced());
        Assert.Throws<NotSupportedException>(() => basicPublishEndpoint.Advanced());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-ENDPOINT", "send-required-inputs")]
    public void SendOverloads_RejectEveryMissingRequiredInputBeforeProviderUse()
    {
        ISendEndpoint endpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingEndpointProxy proxy);
        var message = new EndpointContract("value");
        IPipe<SendContext<EndpointContract>> typedPipe = Pipe.Empty<SendContext<EndpointContract>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("message", () => endpoint.SendAsync<EndpointContract>(null!, typedPipe, cancellationToken), proxy);
        AssertNull("pipe", () => endpoint.SendAsync(message, (IPipe<SendContext<EndpointContract>>)null!, cancellationToken), proxy);
        AssertNull("message", () => endpoint.SendAsync<EndpointContract>(null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => endpoint.SendAsync(message, (IPipe<SendContext>)null!, cancellationToken), proxy);

        AssertNull("message", () => endpoint.SendAsync((object)null!, typeof(EndpointContract), cancellationToken), proxy);
        AssertNull("messageType", () => endpoint.SendAsync((object)message, (Type)null!, cancellationToken), proxy);
        AssertNull("message", () => endpoint.SendAsync((object)null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => endpoint.SendAsync((object)message, (IPipe<SendContext>)null!, cancellationToken), proxy);
        AssertNull("message", () => endpoint.SendAsync((object)null!, typeof(EndpointContract), pipe, cancellationToken), proxy);
        AssertNull("messageType", () => endpoint.SendAsync((object)message, (Type)null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => endpoint.SendAsync((object)message, typeof(EndpointContract), (IPipe<SendContext>)null!, cancellationToken), proxy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-ENDPOINT", "publish-required-inputs")]
    public void PublishOverloads_RejectEveryMissingRequiredInputBeforeProviderUse()
    {
        IPublishEndpoint endpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingEndpointProxy proxy);
        var message = new EndpointContract("value");
        IPipe<PublishContext<EndpointContract>> typedPipe = Pipe.Empty<PublishContext<EndpointContract>>();
        IPipe<PublishContext> pipe = Pipe.Empty<PublishContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("message", () => endpoint.PublishAsync<EndpointContract>(null!, typedPipe, cancellationToken), proxy);
        AssertNull("pipe", () => endpoint.PublishAsync(message, (IPipe<PublishContext<EndpointContract>>)null!, cancellationToken), proxy);
        AssertNull("message", () => endpoint.PublishAsync<EndpointContract>(null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => endpoint.PublishAsync(message, (IPipe<PublishContext>)null!, cancellationToken), proxy);

        AssertNull("message", () => endpoint.PublishAsync((object)null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => endpoint.PublishAsync((object)message, (IPipe<PublishContext>)null!, cancellationToken), proxy);
        AssertNull("message", () => endpoint.PublishAsync((object)null!, typeof(EndpointContract), cancellationToken), proxy);
        AssertNull("messageType", () => endpoint.PublishAsync((object)message, (Type)null!, cancellationToken), proxy);
        AssertNull("message", () => endpoint.PublishAsync((object)null!, typeof(EndpointContract), pipe, cancellationToken), proxy);
        AssertNull("messageType", () => endpoint.PublishAsync((object)message, (Type)null!, pipe, cancellationToken), proxy);
        AssertNull("pipe", () => endpoint.PublishAsync((object)message, typeof(EndpointContract), (IPipe<PublishContext>)null!, cancellationToken), proxy);
    }

    static TContract CreateProxy<TContract>(out RecordingEndpointProxy proxy)
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, RecordingEndpointProxy>();
        proxy = (RecordingEndpointProxy)(object)contract;
        return contract;
    }

    static void AssertNull(string parameterName, Action operation, RecordingEndpointProxy proxy)
    {
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(operation).ParamName);
        Assert.Equal(0, proxy.InvocationCount);
    }

    sealed record EndpointContract(string Value);

    class RecordingEndpointProxy : DispatchProxy
    {
        public int InvocationCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            InvocationCount++;
            throw new InvalidOperationException($"The provider must not be invoked for invalid input: {targetMethod?.Name}");
        }
    }
}
