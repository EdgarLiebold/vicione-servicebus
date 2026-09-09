using System.Reflection;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Scheduling;

public sealed class SqlScheduleMessageProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-TOKEN", "generated-token-is-persisted-and-returned")]
    public async Task ScheduleSendAsync_PersistsAndReturnsTheSameGeneratedTokenAsync()
    {
        ISendEndpoint endpoint = DispatchProxy.Create<AdvancedScheduleEndpoint, ScheduleEndpointProxy>();
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, EndpointProviderProxy>();
        ((EndpointProviderProxy)(object)endpoints).Endpoint = endpoint;
        ISqlHostConfiguration hostConfiguration = DispatchProxy.Create<ISqlHostConfiguration, UnsupportedProxy>();
        var provider = new SqlScheduleMessageProvider(hostConfiguration, endpoints);

        ScheduledMessage<Probe> scheduled = await provider.ScheduleSendAsync(
            new Uri("db://localhost/transport/scheduled"),
            DateTimeOffset.UtcNow.AddMinutes(5),
            new Probe(),
            Pipe.Empty<SendContext<Probe>>(),
            TestContext.Current.CancellationToken);

        SendContext context = Assert.IsType<InMemorySendContext<Probe>>(
            ((ScheduleEndpointProxy)(object)endpoint).Context);
        Assert.NotEqual(Guid.Empty, scheduled.TokenId);
        Assert.Equal(scheduled.TokenId, context.ScheduledMessageId);
        Assert.True(context.Headers.TryGetHeader(MessageHeaders.SchedulingTokenId, out object? header));
        Assert.Equal(scheduled.TokenId.ToString("D"), header);
    }

    private sealed record Probe;

    private interface AdvancedScheduleEndpoint :
        ISendEndpoint,
        ViciOne.ServiceBus.Advanced.IAdvancedSendEndpoint;

    private class EndpointProviderProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name == "GetSendEndpointAsync"
                ? Task.FromResult(Endpoint)
                : throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class ScheduleEndpointProxy : DispatchProxy
    {
        public SendContext? Context { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "SendAsync"
                && args is [Probe message, IPipe<SendContext<Probe>> pipe, CancellationToken _])
            {
                var context = new InMemorySendContext<Probe>(message);
                Context = context;
                return pipe.SendAsync(context);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class UnsupportedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
