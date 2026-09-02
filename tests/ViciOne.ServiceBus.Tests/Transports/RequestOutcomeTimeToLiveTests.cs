using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class RequestOutcomeTimeToLiveTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-OUTCOME-TTL", "expired-response-and-fault-grace")]
    public async Task ExpiredRequest_GivesOnlyItsResponseAndFaultTheOneSecondGrace()
    {
        RequestOutcomeTimeToLive observation = await ObserveOutcomeTimeToLive(
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30));

        Assert.Equal(TimeSpan.FromSeconds(1), observation.Response);
        Assert.Equal(TimeSpan.FromSeconds(1), observation.Fault);
        Assert.Null(observation.UnrelatedSend);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-OUTCOME-TTL", "active-response-and-fault-remainder")]
    public async Task ActiveRequest_GivesOnlyItsResponseAndFaultTheRemainingLifetime()
    {
        TimeSpan expectedRemainder = TimeSpan.FromSeconds(90);

        RequestOutcomeTimeToLive observation = await ObserveOutcomeTimeToLive(
            TimeSpan.FromMinutes(2),
            -expectedRemainder);

        Assert.Equal(expectedRemainder, observation.Response);
        Assert.Equal(expectedRemainder, observation.Fault);
        Assert.Null(observation.UnrelatedSend);
    }

    private static async Task<RequestOutcomeTimeToLive> ObserveOutcomeTimeToLive(
        TimeSpan sourceTimeToLive,
        TimeSpan nowOffsetFromExpiration)
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"request-outcome-ttl-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var observation = new TaskCompletionSource<RequestOutcomeTimeToLive>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Uri responseAddress = new(harness.BaseAddress, "response-outcome");
        Uri faultAddress = new(harness.BaseAddress, "fault-outcome");
        Uri unrelatedAddress = new(harness.BaseAddress, "unrelated-send");

        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<RequestMessage>(async context =>
            {
                DateTime expirationTime = Assert.IsType<DateTime>(context.ExpirationTime);
                context.SetTimeProvider(new FakeTimeProvider(
                    new DateTimeOffset(expirationTime + nowOffsetFromExpiration, TimeSpan.Zero)));
                TimeSpan? responseTimeToLive = null;
                TimeSpan? faultTimeToLive = null;
                TimeSpan? unrelatedTimeToLive = null;

                ISendEndpoint responseEndpoint = await context.GetResponseEndpoint<OutcomeMessage>();
                await responseEndpoint.Send(
                    new OutcomeMessage(),
                    Pipe.Execute<SendContext<OutcomeMessage>>(sendContext =>
                        responseTimeToLive = sendContext.TimeToLive),
                    context.CancellationToken);

                ISendEndpoint faultEndpoint = await context.GetFaultEndpoint<OutcomeMessage>();
                await faultEndpoint.Send(
                    new OutcomeMessage(),
                    Pipe.Execute<SendContext<OutcomeMessage>>(sendContext =>
                        faultTimeToLive = sendContext.TimeToLive),
                    context.CancellationToken);

                ISendEndpoint unrelatedEndpoint = await context.GetSendEndpoint(unrelatedAddress);
                await unrelatedEndpoint.Send(
                    new OutcomeMessage(),
                    Pipe.Execute<SendContext<OutcomeMessage>>(sendContext =>
                        unrelatedTimeToLive = sendContext.TimeToLive),
                    context.CancellationToken);

                observation.TrySetResult(new RequestOutcomeTimeToLive(
                    responseTimeToLive,
                    faultTimeToLive,
                    unrelatedTimeToLive));
            });
        harness.OnConfigureInMemoryBus += configurator =>
        {
            configurator.ReceiveEndpoint("response-outcome", endpoint =>
                endpoint.Handler<OutcomeMessage>(_ => Task.CompletedTask));
            configurator.ReceiveEndpoint("fault-outcome", endpoint =>
                endpoint.Handler<OutcomeMessage>(_ => Task.CompletedTask));
            configurator.ReceiveEndpoint("unrelated-send", endpoint =>
                endpoint.Handler<OutcomeMessage>(_ => Task.CompletedTask));
        };

        try
        {
            await harness.Start(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(
                    new RequestMessage(),
                    context =>
                    {
                        context.RequestId = Guid.Parse("aaed0f76-129c-4281-9308-beb15678cc0c");
                        context.ResponseAddress = responseAddress;
                        context.FaultAddress = faultAddress;
                        context.TimeToLive = sourceTimeToLive;
                    },
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            return await observation.Task.WaitAsync(operationTimeout, cancellationToken);
        }
        finally
        {
            await harness.Stop().WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private sealed record RequestOutcomeTimeToLive(
        TimeSpan? Response,
        TimeSpan? Fault,
        TimeSpan? UnrelatedSend);

    private sealed class RequestMessage;

    private sealed class OutcomeMessage;
}
