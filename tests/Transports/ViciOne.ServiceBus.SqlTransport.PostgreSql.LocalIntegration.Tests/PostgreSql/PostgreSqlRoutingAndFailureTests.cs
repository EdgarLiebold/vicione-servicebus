using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlRoutingAndFailureTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0055", "postgresql-native-owner")]
    public async Task RoutingKeySubscription_DeliversOnlyTheExactKeyAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "routing-key",
            cancellationToken);
        string queueName = fixture.Name("routing-input");
        await DeclareSubscriptionsAsync(
            fixture,
            queueName,
            SqlSubscriptionType.RoutingKey,
            "8675309",
            "655321",
            cancellationToken);
        var rejected = new UpdatedEvent(Guid.NewGuid());
        var accepted = new DeletedEvent(Guid.NewGuid());

        await PublishWithRoutingKeysAsync(
            fixture,
            rejected,
            "11223344",
            accepted,
            "655321",
            cancellationToken);

        await AssertOnlyAcceptedDeliveryAsync(
            fixture,
            queueName,
            rejected.Id,
            accepted.Id,
            cancellationToken);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0056", "postgresql-native-owner")]
    public async Task PatternSubscription_UsesPostgreSqlAnchoredRegularExpressionsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "routing-pattern",
            cancellationToken);
        string queueName = fixture.Name("pattern-input");
        await DeclareSubscriptionsAsync(
            fixture,
            queueName,
            SqlSubscriptionType.Pattern,
            "^[A-Z]+$",
            "^[0-9]+$",
            cancellationToken);
        var rejected = new UpdatedEvent(Guid.NewGuid());
        var accepted = new DeletedEvent(Guid.NewGuid());

        await PublishWithRoutingKeysAsync(
            fixture,
            rejected,
            "11223344",
            accepted,
            "655321",
            cancellationToken);

        await AssertOnlyAcceptedDeliveryAsync(
            fixture,
            queueName,
            rejected.Id,
            accepted.Id,
            cancellationToken);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0057", "postgresql-native-owner")]
    public async Task UnhandledMessage_IsAcknowledgedIntoTheEndpointDeadLetterQueueAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "skipped-message",
            cancellationToken);
        string queueName = fixture.Name("skipped-input");
        var observer = new ReceiveCompletionObserver();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ConnectReceiveObserver(observer);
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.ConfigureConsumeTopology = false);
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(new UnhandledMessage(Guid.NewGuid()), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await observer.Completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(0, await connection.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCountAsync(fixture.Schema, queueName, 3, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0058", "postgresql-native-owner")]
    public async Task ThrowingHandler_PublishesFaultAndMovesDeliveryToTheEndpointErrorQueueAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "consumer-fault",
            cancellationToken);
        string queueName = fixture.Name("fault-input");
        string faultQueueName = fixture.Name("fault-observer");
        var faulted = NewObservation<ConsumeContext<Fault<FailingMessage>>>();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
                endpoint.Handler<FailingMessage>(_ => throw new DeliberateConsumerException()));
            configurator.ReceiveEndpoint(faultQueueName, endpoint =>
                endpoint.Handler<Fault<FailingMessage>>(context =>
                {
                    faulted.TrySetResult(context);
                    return Task.CompletedTask;
                }));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            var message = new FailingMessage(Guid.NewGuid());
            await endpoint.SendAsync(message, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<Fault<FailingMessage>> fault = await faulted.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(message.Id, fault.Message.Message.Id);
            Assert.Contains(fault.Message.Exceptions, exception =>
                exception.ExceptionType == typeof(DeliberateConsumerException).FullName);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(0, await connection.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCountAsync(fixture.Schema, queueName, 2, cancellationToken));
    }

    private static async Task DeclareSubscriptionsAsync(
        PostgreSqlTestDatabase fixture,
        string queueName,
        SqlSubscriptionType subscriptionType,
        string updatedRoutingKey,
        string deletedRoutingKey,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Subscribe<UpdatedEvent>(subscription =>
                {
                    subscription.SubscriptionType = subscriptionType;
                    subscription.RoutingKey = updatedRoutingKey;
                });
                endpoint.Subscribe<DeletedEvent>(subscription =>
                {
                    subscription.SubscriptionType = subscriptionType;
                    subscription.RoutingKey = deletedRoutingKey;
                });
            });
        });
        await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
    }

    private static async Task PublishWithRoutingKeysAsync(
        PostgreSqlTestDatabase fixture,
        UpdatedEvent rejected,
        string rejectedRoutingKey,
        DeletedEvent accepted,
        string acceptedRoutingKey,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.PublishAsync(
                    rejected,
                    context =>
                    {
                        context.MessageId = rejected.Id;
                        context.SetRoutingKey(rejectedRoutingKey);
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(
                    accepted,
                    context =>
                    {
                        context.MessageId = accepted.Id;
                        context.SetRoutingKey(acceptedRoutingKey);
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task AssertOnlyAcceptedDeliveryAsync(
        PostgreSqlTestDatabase fixture,
        string queueName,
        Guid rejectedId,
        Guid acceptedId,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        Assert.Equal(0, await connection.MessageCountAsync(fixture.Schema, rejectedId, cancellationToken));
        Assert.Equal(0, await connection.DeliveryCountForMessageAsync(fixture.Schema, rejectedId, cancellationToken));
        Assert.Equal(1, await connection.MessageCountAsync(fixture.Schema, acceptedId, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCountForMessageAsync(fixture.Schema, acceptedId, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ReceiveCompletionObserver : IReceiveObserver
    {
        public TaskCompletionSource Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceiveAsync(ReceiveContext context)
        {
            Completed.TrySetResult();
            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(
            ConsumeContext<T> context,
            TimeSpan duration,
            string consumerType,
            Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            Completed.TrySetException(exception);
            return Task.CompletedTask;
        }
    }

    private sealed record UpdatedEvent(Guid Id);
    private sealed record DeletedEvent(Guid Id);
    private sealed record UnhandledMessage(Guid Id);
    private sealed record FailingMessage(Guid Id);

    private sealed class DeliberateConsumerException : Exception
    {
    }
}
