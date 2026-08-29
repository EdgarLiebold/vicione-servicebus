namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PostgreSqlRoutingAndFailureTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0055", "postgresql-native-owner")]
    public async Task RoutingKeySubscription_DeliversOnlyTheExactKey()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "routing-key",
            cancellationToken);
        string queueName = fixture.Name("routing-input");
        await DeclareSubscriptions(
            fixture,
            queueName,
            SqlSubscriptionType.RoutingKey,
            "8675309",
            "655321",
            cancellationToken);
        var rejected = new UpdatedEvent(Guid.NewGuid());
        var accepted = new DeletedEvent(Guid.NewGuid());

        await PublishWithRoutingKeys(
            fixture,
            rejected,
            "11223344",
            accepted,
            "655321",
            cancellationToken);

        await AssertOnlyAcceptedDelivery(
            fixture,
            queueName,
            rejected.Id,
            accepted.Id,
            cancellationToken);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0056", "postgresql-native-owner")]
    public async Task PatternSubscription_UsesPostgreSqlAnchoredRegularExpressions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "routing-pattern",
            cancellationToken);
        string queueName = fixture.Name("pattern-input");
        await DeclareSubscriptions(
            fixture,
            queueName,
            SqlSubscriptionType.Pattern,
            "^[A-Z]+$",
            "^[0-9]+$",
            cancellationToken);
        var rejected = new UpdatedEvent(Guid.NewGuid());
        var accepted = new DeletedEvent(Guid.NewGuid());

        await PublishWithRoutingKeys(
            fixture,
            rejected,
            "11223344",
            accepted,
            "655321",
            cancellationToken);

        await AssertOnlyAcceptedDelivery(
            fixture,
            queueName,
            rejected.Id,
            accepted.Id,
            cancellationToken);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0057", "postgresql-native-owner")]
    public async Task UnhandledMessage_IsAcknowledgedIntoTheEndpointDeadLetterQueue()
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
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new UnhandledMessage(Guid.NewGuid()), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await observer.Completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(0, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCount(fixture.Schema, queueName, 3, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0058", "postgresql-native-owner")]
    public async Task ThrowingHandler_PublishesFaultAndMovesDeliveryToTheEndpointErrorQueue()
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
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            var message = new FailingMessage(Guid.NewGuid());
            await endpoint.Send(message, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
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
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(0, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCount(fixture.Schema, queueName, 2, cancellationToken));
    }

    private static async Task DeclareSubscriptions(
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

    private static async Task PublishWithRoutingKeys(
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
            await bus.Publish(
                    rejected,
                    context =>
                    {
                        context.MessageId = rejected.Id;
                        context.SetRoutingKey(rejectedRoutingKey);
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.Publish(
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

    private static async Task AssertOnlyAcceptedDelivery(
        PostgreSqlTestDatabase fixture,
        string queueName,
        Guid rejectedId,
        Guid acceptedId,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);

        Assert.Equal(0, await connection.MessageCount(fixture.Schema, rejectedId, cancellationToken));
        Assert.Equal(0, await connection.DeliveryCountForMessage(fixture.Schema, rejectedId, cancellationToken));
        Assert.Equal(1, await connection.MessageCount(fixture.Schema, acceptedId, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCountForMessage(fixture.Schema, acceptedId, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ReceiveCompletionObserver : IReceiveObserver
    {
        public TaskCompletionSource Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PreReceive(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceive(ReceiveContext context)
        {
            Completed.TrySetResult();
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFault<T>(
            ConsumeContext<T> context,
            TimeSpan duration,
            string consumerType,
            Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFault(ReceiveContext context, Exception exception)
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
