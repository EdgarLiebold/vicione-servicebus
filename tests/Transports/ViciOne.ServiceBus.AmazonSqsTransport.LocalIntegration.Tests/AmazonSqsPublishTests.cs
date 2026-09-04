using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.PublishContracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

public sealed class AmazonSqsPublishTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0229", "two-distinct-contracts-publish-from-one-bus")]
    public async Task EveryDeclaredMessageContract_CanBePublishedFromOneBusAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("publish-contracts");
        string queueName = fixture.Name("input");
        Guid expected = Guid.NewGuid();
        var first = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCount = 0;
        var secondCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
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
            await bus.PublishAsync<FirstPublishedContract>(new { CorrelationId = expected }, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync<SecondPublishedContract>(new { CorrelationId = expected }, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, await first.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(expected, await second.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;
            Assert.Equal(1, firstCount);
            Assert.Equal(1, secondCount);
            Assert.Equal(
                ExpectedTopicNames(
                    fixture,
                    typeof(FirstPublishedContract),
                    typeof(SecondPublishedContract)),
                await fixture.ListOwnedTopicNamesAsync(cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static string[] ExpectedTopicNames(AmazonSqsLocalStack fixture, params Type[] messageTypes)
    {
        var formatter = new AmazonSqsMessageNameFormatter();
        return messageTypes
            .Select(type => $"{fixture.Prefix}_{formatter.GetMessageName(type)}")
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
