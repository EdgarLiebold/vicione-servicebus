using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class ContainerActivityRegistrationTests
{
    [Theory]
    [InlineData(ActivityRegistrationShape.ManualExecuteEndpoint)]
    [InlineData(ActivityRegistrationShape.CustomExecuteEndpoint)]
    [InlineData(ActivityRegistrationShape.CustomExecuteAndCompensateEndpoints)]
    [RequirementCoverage("REQ-VSB-CONTAINER-COURIER-ACTIVITY", "manual-and-registration-owned-endpoint-matrix")]
    public async Task ContainerRegisteredActivity_ExecutesAtItsExactOwnedEndpointAsync(ActivityRegistrationShape shape)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string executeName = shape switch
        {
            ActivityRegistrationShape.ManualExecuteEndpoint => "container-manual-execute",
            ActivityRegistrationShape.CustomExecuteEndpoint => "container-custom-execute",
            _ => "container-complete-execute",
        };
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                switch (shape)
                {
                    case ActivityRegistrationShape.ManualExecuteEndpoint:
                        configuration.AddExecuteActivity<ContainerExecuteActivity, CourierArguments>();
                        configuration.UsingInMemory((context, bus) =>
                            bus.ReceiveEndpoint(executeName, endpoint =>
                                endpoint.ConfigureExecuteActivity(context, typeof(ContainerExecuteActivity))));
                        break;
                    case ActivityRegistrationShape.CustomExecuteEndpoint:
                        configuration.AddExecuteActivity<ContainerExecuteActivity, CourierArguments>()
                            .Endpoint(endpoint => endpoint.Name = executeName);
                        break;
                    default:
                        configuration.AddActivity<FirstCourierActivity, CourierArguments, CourierLog>()
                            .ExecuteEndpoint(endpoint => endpoint.Name = executeName)
                            .CompensateEndpoint(endpoint => endpoint.Name = "container-complete-compensate");
                        break;
                }
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity("ContainerActivity", new Uri($"queue:{executeName}"), new CourierArguments("expected"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            IPublishedMessage<IRoutingSlipActivityCompleted> activity = await harness.Published
                .SelectAsync<IRoutingSlipActivityCompleted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            IPublishedMessage<IRoutingSlipCompleted> completed = await harness.Published
                .SelectAsync<IRoutingSlipCompleted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(trackingNumber, activity.Context.Message.TrackingNumber);
            Assert.Equal(trackingNumber, completed.Context.Message.TrackingNumber);
            Assert.Equal("ContainerActivity", activity.Context.Message.ActivityName);
            Assert.Equal("expected", activity.Context.Message.Arguments[nameof(CourierArguments.Value)]);
            Assert.Equal(executeName, activity.Context.SourceAddress!.AbsolutePath.Trim('/'));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    public enum ActivityRegistrationShape
    {
        ManualExecuteEndpoint,
        CustomExecuteEndpoint,
        CustomExecuteAndCompensateEndpoints,
    }

    internal sealed class ContainerExecuteActivity : IExecuteActivity<CourierArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<CourierArguments> context) =>
            Task.FromResult(context.Completed());
    }
}
