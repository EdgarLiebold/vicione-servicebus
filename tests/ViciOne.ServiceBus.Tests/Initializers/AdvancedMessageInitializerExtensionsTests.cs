using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class AdvancedMessageInitializerExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-MESSAGE-INITIALIZER", "exact-overload-forwarding")]
    public async Task InitializerOverloads_ForwardExactValuesPipesAndCancellationAsync()
    {
        ISendEndpoint sendEndpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingEndpointProxy sendProxy);
        IPublishEndpoint publishEndpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingEndpointProxy publishProxy);
        sendProxy.ThrowOnInvocation = false;
        publishProxy.ThrowOnInvocation = false;
        var values = new { Value = "message" };
        IPipe<SendContext<MessageContract>> typedSendPipe = Pipe.Empty<SendContext<MessageContract>>();
        IPipe<SendContext> sendPipe = Pipe.Empty<SendContext>();
        IPipe<PublishContext<MessageContract>> typedPublishPipe = Pipe.Empty<PublishContext<MessageContract>>();
        IPipe<PublishContext> publishPipe = Pipe.Empty<PublishContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(sendEndpoint, values, cancellationToken);
        await AdvancedMessageInitializerExtensions.SendAsync(sendEndpoint, values, typedSendPipe, cancellationToken);
        await AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(sendEndpoint, values, sendPipe, cancellationToken);
        await AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(publishEndpoint, values, cancellationToken);
        await AdvancedMessageInitializerExtensions.PublishAsync(publishEndpoint, values, typedPublishPipe, cancellationToken);
        await AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(publishEndpoint, values, publishPipe, cancellationToken);

        AssertInvocation(sendProxy.Invocations[0], values, null, cancellationToken);
        AssertInvocation(sendProxy.Invocations[1], values, typedSendPipe, cancellationToken);
        AssertInvocation(sendProxy.Invocations[2], values, sendPipe, cancellationToken);
        AssertInvocation(publishProxy.Invocations[0], values, null, cancellationToken);
        AssertInvocation(publishProxy.Invocations[1], values, typedPublishPipe, cancellationToken);
        AssertInvocation(publishProxy.Invocations[2], values, publishPipe, cancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-MESSAGE-INITIALIZER", "exact-type-and-pipe-forwarding")]
    public async Task RuntimeSelectedOverloads_ForwardTheExactContractValuesPipeAndCancellationAsync()
    {
        ISendEndpoint sendEndpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingEndpointProxy sendProxy);
        IPublishEndpoint publishEndpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingEndpointProxy publishProxy);
        sendProxy.ThrowOnInvocation = false;
        publishProxy.ThrowOnInvocation = false;
        var values = new { Value = "message" };
        IPipe<SendContext> sendPipe = Pipe.Empty<SendContext>();
        IPipe<PublishContext> publishPipe = Pipe.Empty<PublishContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await SendEndpointExtensions.SendAsync(sendEndpoint, typeof(MessageContract), values, cancellationToken);
        await SendEndpointExtensions.SendAsync(sendEndpoint, typeof(MessageContract), values, sendPipe, cancellationToken);
        await PublishEndpointExtensions.PublishAsync(publishEndpoint, typeof(MessageContract), values, cancellationToken);
        await PublishEndpointExtensions.PublishAsync(publishEndpoint, typeof(MessageContract), values, publishPipe, cancellationToken);

        Assert.Equal(2, sendProxy.InvocationCount);
        Assert.Equal(2, publishProxy.InvocationCount);
        AssertInvocation(sendProxy.Invocations[0], values, null, cancellationToken);
        AssertInvocation(sendProxy.Invocations[1], values, sendPipe, cancellationToken);
        AssertInvocation(publishProxy.Invocations[0], values, null, cancellationToken);
        AssertInvocation(publishProxy.Invocations[1], values, publishPipe, cancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-MESSAGE-INITIALIZER", "capability-validation")]
    public void InitializerOverloads_RejectMissingAndUnsupportedEndpoints()
    {
        var values = new { Value = "message" };
        ISendEndpoint basicSendEndpoint = CreateProxy<ISendEndpoint>(out _);
        IPublishEndpoint basicPublishEndpoint = CreateProxy<IPublishEndpoint>(out _);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("endpoint", () => AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(null!, values, cancellationToken));
        AssertNull("endpoint", () => AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(null!, values, cancellationToken));
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(basicSendEndpoint, values, cancellationToken);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(basicPublishEndpoint, values, cancellationToken);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-MESSAGE-INITIALIZER", "required-inputs-before-provider-use")]
    public void InitializerOverloads_RejectEveryMissingValueAndPipeBeforeProviderUse()
    {
        ISendEndpoint sendEndpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingEndpointProxy sendProxy);
        IPublishEndpoint publishEndpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingEndpointProxy publishProxy);
        var values = new { Value = "message" };
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("values", () => AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(sendEndpoint, null!, cancellationToken));
        AssertNull("values", () => AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(sendEndpoint, null!,
            Pipe.Empty<SendContext<MessageContract>>(), cancellationToken));
        AssertNull("values", () => AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(sendEndpoint, null!,
            Pipe.Empty<SendContext>(), cancellationToken));
        AssertNull("pipe", () => AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(sendEndpoint, values,
            (IPipe<SendContext<MessageContract>>)null!, cancellationToken));
        AssertNull("pipe", () => AdvancedMessageInitializerExtensions.SendAsync<MessageContract>(sendEndpoint, values,
            (IPipe<SendContext>)null!, cancellationToken));

        AssertNull("values", () => AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(publishEndpoint, null!, cancellationToken));
        AssertNull("values", () => AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(publishEndpoint, null!,
            Pipe.Empty<PublishContext<MessageContract>>(), cancellationToken));
        AssertNull("values", () => AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(publishEndpoint, null!,
            Pipe.Empty<PublishContext>(), cancellationToken));
        AssertNull("pipe", () => AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(publishEndpoint, values,
            (IPipe<PublishContext<MessageContract>>)null!, cancellationToken));
        AssertNull("pipe", () => AdvancedMessageInitializerExtensions.PublishAsync<MessageContract>(publishEndpoint, values,
            (IPipe<PublishContext>)null!, cancellationToken));

        Assert.Equal(0, sendProxy.InvocationCount);
        Assert.Equal(0, publishProxy.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-MESSAGE-INITIALIZER", "required-inputs")]
    public void RuntimeSelectedOverloads_RejectEveryMissingRequiredInput()
    {
        ISendEndpoint sendEndpoint = CreateProxy<ISendEndpoint>(out _);
        IPublishEndpoint publishEndpoint = CreateProxy<IPublishEndpoint>(out _);
        var values = new { Value = "message" };
        IPipe<SendContext> sendPipe = Pipe.Empty<SendContext>();
        IPipe<PublishContext> publishPipe = Pipe.Empty<PublishContext>();

        AssertNull("endpoint", () => SendEndpointExtensions.SendAsync(null!, typeof(MessageContract), values));
        AssertNull("messageType", () => SendEndpointExtensions.SendAsync(sendEndpoint, null!, values));
        AssertNull("values", () => SendEndpointExtensions.SendAsync(sendEndpoint, typeof(MessageContract), null!));
        AssertNull("messageType", () => SendEndpointExtensions.SendAsync(sendEndpoint, null!, values, sendPipe));
        AssertNull("values", () => SendEndpointExtensions.SendAsync(sendEndpoint, typeof(MessageContract), null!, sendPipe));
        AssertNull("pipe", () => SendEndpointExtensions.SendAsync(sendEndpoint, typeof(MessageContract), values, null!));

        AssertNull("endpoint", () => PublishEndpointExtensions.PublishAsync(null!, typeof(MessageContract), values));
        AssertNull("messageType", () => PublishEndpointExtensions.PublishAsync(publishEndpoint, null!, values));
        AssertNull("values", () => PublishEndpointExtensions.PublishAsync(publishEndpoint, typeof(MessageContract), null!));
        AssertNull("messageType", () => PublishEndpointExtensions.PublishAsync(publishEndpoint, null!, values, publishPipe));
        AssertNull("values", () => PublishEndpointExtensions.PublishAsync(publishEndpoint, typeof(MessageContract), null!, publishPipe));
        AssertNull("pipe", () => PublishEndpointExtensions.PublishAsync(publishEndpoint, typeof(MessageContract), values, null!));
    }

    static TContract CreateProxy<TContract>(out RecordingEndpointProxy proxy)
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, RecordingEndpointProxy>();
        proxy = (RecordingEndpointProxy)(object)contract;
        return contract;
    }

    static void AssertNull(string parameterName, Action operation) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(operation).ParamName);

    static void AssertInvocation(Invocation invocation, object values, object? pipe, CancellationToken cancellationToken)
    {
        Assert.Equal(typeof(MessageContract), Assert.Single(invocation.Method.GetGenericArguments()));
        Assert.Same(values, invocation.Arguments[0]);
        if (pipe is null)
            Assert.Equal(cancellationToken, invocation.Arguments[1]);
        else
        {
            Assert.Same(pipe, invocation.Arguments[1]);
            Assert.Equal(cancellationToken, invocation.Arguments[2]);
        }
    }

    sealed record MessageContract(string Value);

    sealed record Invocation(MethodInfo Method, object?[] Arguments);

    class RecordingEndpointProxy : DispatchProxy
    {
        public List<Invocation> Invocations { get; } = [];

        public int InvocationCount => Invocations.Count;

        public bool ThrowOnInvocation { get; set; } = true;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            Invocations.Add(new Invocation(targetMethod, args ?? []));
            if (ThrowOnInvocation)
                throw new InvalidOperationException($"The provider must not be invoked for invalid input: {targetMethod.Name}");

            return Task.CompletedTask;
        }
    }
}
