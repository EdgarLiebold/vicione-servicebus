namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests;

using System.Text;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class RabbitMqFaultRedriveTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-FAULT-REDRIVE", "structured-filters-and-unmatched-requeue")]
    public async Task StructuredFilters_RedriveOnlyMatchesAndRequeueEveryUnmatchedDelivery()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("redrivefilters");
        string target = fixture.Name("input");
        string source = target + "_error";
        Guid messageId = Guid.Parse("5f27256b-a39e-401a-a037-6cf8627b5679");
        Guid correlationId = Guid.Parse("17eb2da5-4d99-43e3-86cd-03656ca1ce32");
        const string faultType = "System.InvalidOperationException";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await fixture.DeclareEndpointTopology(target, cancellationToken);
        await fixture.DeclareQueue(source, cancellationToken);
        await PublishSource(fixture, source, "other-message", GuidFrom(1), correlationId, faultType, cancellationToken);
        await PublishSource(fixture, source, "match-one", messageId, correlationId, faultType, cancellationToken);
        await PublishSource(fixture, source, "other-correlation", messageId, GuidFrom(2), faultType, cancellationToken);
        await PublishSource(fixture, source, "match-two", messageId, correlationId, faultType, cancellationToken);
        await PublishSource(fixture, source, "unscanned-match", messageId, correlationId, faultType, cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IRabbitMqQueueOperations operations = provider.GetRequiredService<IRabbitMqQueueOperations>();

            RabbitMqFaultRedriveResult result = await operations.RedriveFaultedMessages(
                    new RabbitMqFaultRedriveRequest(target)
                    {
                        MessageId = messageId,
                        CorrelationId = correlationId,
                        FaultExceptionType = faultType,
                        MaxMessages = 2,
                        MaxScanCount = 4,
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal((target, source), (result.EndpointQueueName, result.SourceQueueName));
            Assert.Equal((4, 2, 2), (result.Scanned, result.Matched, result.Redriven));
            Assert.False(result.SourceExhausted);
            Assert.False(result.ScanLimitReached);
            Assert.Equal(3U, await fixture.QueueMessageCount(source, cancellationToken));
            Assert.Equal(2U, await fixture.QueueMessageCount(target, cancellationToken));

            IReadOnlyList<RabbitMqBroker.RawMessage> messages = await fixture.GetRaw(target, 3, cancellationToken);
            Assert.Equal(["match-one", "match-two"], messages.Select(message => Encoding.UTF8.GetString(message.Body)));
            Assert.All(messages, message => Assert.Equal(source, message.RoutingKey));
            Assert.All(messages, message => Assert.Equal(messageId.ToString("D"), message.Properties.MessageId));
            Assert.All(messages, message => Assert.Equal(correlationId.ToString("D"), message.Properties.CorrelationId));
            Assert.All(messages, message => Assert.Equal("application/vnd.vicione.fault+json", message.Properties.ContentType));
            Assert.All(messages, message => Assert.Equal("ViciOne.Fault", message.Properties.Type));
            Assert.All(messages, message => Assert.True(message.Properties.Persistent));
            Assert.All(messages, message => Assert.Equal(faultType, FaultType(message.Properties)));
            Assert.Equal(0U, await fixture.QueueMessageCount(target, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-FAULT-REDRIVE", "unfiltered-redrive-limit")]
    public async Task UnfilteredRedrive_StopsAtTheExactConfiguredMessageLimit()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("redrivelimit");
        string target = fixture.Name("input");
        string source = target + "_error";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await fixture.DeclareEndpointTopology(target, cancellationToken);
        await fixture.DeclareQueue(source, cancellationToken);
        await PublishSource(fixture, source, "first", GuidFrom(3), GuidFrom(4), "First", cancellationToken);
        await PublishSource(fixture, source, "second", GuidFrom(5), GuidFrom(6), "Second", cancellationToken);
        await PublishSource(fixture, source, "third", GuidFrom(7), GuidFrom(8), "Third", cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            RabbitMqFaultRedriveResult result = await provider.GetRequiredService<IRabbitMqQueueOperations>()
                .RedriveFaultedMessages(new RabbitMqFaultRedriveRequest(target)
                {
                    MaxMessages = 2,
                    MaxScanCount = 3,
                }, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal((2, 2, 2), (result.Scanned, result.Matched, result.Redriven));
            Assert.False(result.SourceExhausted);
            Assert.False(result.ScanLimitReached);
            Assert.Equal(1U, await fixture.QueueMessageCount(source, cancellationToken));
            Assert.Equal(2U, await fixture.QueueMessageCount(target, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-FAULT-REDRIVE", "mandatory-rejection-retains-source")]
    public async Task MandatoryTargetRejection_LeavesTheSourceMessageAvailable()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("redrivereject");
        string target = fixture.Name("input");
        string source = target + "_error";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await fixture.DeclareQueue(target, cancellationToken);
        await fixture.DeclareExchange(target, cancellationToken);
        await fixture.DeclareQueue(source, cancellationToken);
        await PublishSource(fixture, source, "must-remain", GuidFrom(9), GuidFrom(10), "Rejected", cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            await Assert.ThrowsAsync<PublishReturnException>(() => provider.GetRequiredService<IRabbitMqQueueOperations>()
                .RedriveFaultedMessages(new RabbitMqFaultRedriveRequest(target)
                {
                    MaxMessages = 1,
                    MaxScanCount = 1,
                }, cancellationToken));

            Assert.Equal(1U, await fixture.QueueMessageCount(source, cancellationToken));
            Assert.Equal(0U, await fixture.QueueMessageCount(target, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-FAULT-REDRIVE", "passive-topology-verification")]
    public async Task MissingSourceTopology_IsRejectedWithoutCreatingOperatorSuppliedEntities()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("redrivemissing");
        string target = fixture.Name("missing");
        string source = target + "_error";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            await Assert.ThrowsAnyAsync<OperationInterruptedException>(() => provider.GetRequiredService<IRabbitMqQueueOperations>()
                .RedriveFaultedMessages(new RabbitMqFaultRedriveRequest(target)
                {
                    MaxMessages = 1,
                    MaxScanCount = 1,
                }, cancellationToken));

            Assert.False((await fixture.Queue(source, cancellationToken)).Exists);
            Assert.False((await fixture.Queue(target, cancellationToken)).Exists);
            Assert.False((await fixture.Exchange(target, cancellationToken)).Exists);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private static ServiceProvider CreateProvider(RabbitMqBroker fixture)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddViciOneServiceBus(configurator => configurator.UsingRabbitMq((_, rabbit) => fixture.ConfigureHost(rabbit)));
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private static Task PublishSource(
        RabbitMqBroker fixture,
        string source,
        string body,
        Guid messageId,
        Guid correlationId,
        string faultType,
        CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            MessageId = messageId.ToString("D"),
            CorrelationId = correlationId.ToString("D"),
            ContentType = "application/vnd.vicione.fault+json",
            Type = "ViciOne.Fault",
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                [MessageHeaders.FaultExceptionType] = Encoding.UTF8.GetBytes(faultType),
            },
        };
        return fixture.PublishRaw(
            exchangeName: string.Empty,
            routingKey: source,
            properties,
            Encoding.UTF8.GetBytes(body),
            cancellationToken);
    }

    private static string? FaultType(BasicProperties properties)
    {
        if (properties.Headers == null
            || !properties.Headers.TryGetValue(MessageHeaders.FaultExceptionType, out object? value))
            return null;

        return value switch
        {
            string text => text,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> bytes => Encoding.UTF8.GetString(bytes.Span),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    private static Guid GuidFrom(int value) => new(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
