namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.PublishContracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ActiveMqPublishTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0441", "one-bus-publishes-every-declared-contract")]
    public async Task OneBus_PublishesEveryDeclaredMessageContract(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "publish");
        string queueName = fixture.Name("input");
        Guid expected = Guid.NewGuid();
        var first = NewObservation<Guid>();
        var second = NewObservation<Guid>();
        var firstCount = 0;
        var secondCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.MessageTopology.GetMessageTopology<FirstPublishedContract>().SetEntityName(fixture.Name("first"));
            configurator.MessageTopology.GetMessageTopology<SecondPublishedContract>().SetEntityName(fixture.Name("second"));
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<FirstPublishedContract>(context =>
                {
                    if (Interlocked.Increment(ref firstCount) != 1)
                        first.TrySetException(new InvalidDataException("The first contract was delivered more than once."));
                    else
                        first.TrySetResult(context.Message.CorrelationId);
                    return Task.CompletedTask;
                });
                endpoint.Handler<SecondPublishedContract>(context =>
                {
                    if (Interlocked.Increment(ref secondCount) != 1)
                        second.TrySetException(new InvalidDataException("The second contract was delivered more than once."));
                    else
                        second.TrySetResult(context.Message.CorrelationId);
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.Publish<FirstPublishedContract>(new { CorrelationId = expected }, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.Publish<SecondPublishedContract>(new { CorrelationId = expected }, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, await first.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(expected, await second.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }

        Assert.Equal(1, firstCount);
        Assert.Equal(1, secondCount);
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
