using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlRoutingSlipTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0061", "postgresql-native-owner")]
    public async Task CompletedSubscription_DeliversAllRoutingSlipContentAndItsCustomValueAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "routing-slip",
            cancellationToken);
        string activityQueue = fixture.Name("activity-execute");
        string completionQueue = fixture.Name("completed");
        var activity = new RegistrationActivity();
        var completed = new TaskCompletionSource<ConsumeContext<RegistrationCompleted>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(activityQueue, endpoint =>
                endpoint.ExecuteActivityHost<RegistrationActivity, RegistrationArguments>(() => activity));
            configurator.ReceiveEndpoint(completionQueue, endpoint =>
                endpoint.Handler<RegistrationCompleted>(context =>
                {
                    completed.TrySetResult(context);
                    return Task.CompletedTask;
                }));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid trackingNumber = Guid.NewGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            await builder.AddSubscriptionAsync(new Uri($"queue:{completionQueue}"), RoutingSlipEvents.Completed, RoutingSlipEventContents.All, subscription => subscription.SendAsync<RegistrationCompleted>(new { Value = "Secret Value" }), TestContext.Current.CancellationToken);
            builder.AddActivity(
                "Registration",
                new Uri($"queue:{activityQueue}"),
                new RegistrationArguments("Hello"));

            await bus.ExecuteAsync(builder.Build(), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<RegistrationCompleted> context = await completed.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(trackingNumber, context.Message.TrackingNumber);
            Assert.Equal("Secret Value", context.Message.Value);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, activity.Executions);
            Assert.Equal("Hello", activity.LastValue);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    public interface RegistrationCompleted : RoutingSlipCompleted
    {
        string Value { get; }
    }

    private sealed record RegistrationArguments(string Value);

    private sealed class RegistrationActivity : IExecuteActivity<RegistrationArguments>
    {
        private int _executions;

        public int Executions => Volatile.Read(ref _executions);

        public string? LastValue { get; private set; }

        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RegistrationArguments> context)
        {
            LastValue = context.Arguments.Value;
            Interlocked.Increment(ref _executions);
            return Task.FromResult(context.Completed());
        }
    }
}
