using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Integration;

public sealed class ContainerJobConsumerDiscoveryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-JOB-CONSUMER", "namespace-discovery-and-service-instance-submission")]
    public async Task DiscoveredJobConsumer_AcceptsAndExecutesThroughItsKebabServiceEndpointAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ContainerJobDiscovery.JobObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.SetKebabCaseEndpointNameFormatter();
                configuration.AddConsumersFromNamespaceContaining<ContainerJobDiscovery.DiscoveryMarker>();
                configuration.AddRequestClient<ContainerJobDiscovery.CrunchNumbers>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseDelayedMessageScheduler();
                    var options = new ServiceInstanceOptions().EnableJobServiceEndpoints();
                    bus.ConfigureServiceInstanceEndpoints(context, options);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IRequestClient<ContainerJobDiscovery.CrunchNumbers> client = scope.ServiceProvider
                .GetRequiredService<IRequestClient<ContainerJobDiscovery.CrunchNumbers>>();
            var request = new ContainerJobDiscovery.CrunchNumbers(NewId.NextGuid(), 41);

            Response<JobSubmissionAccepted> accepted = await client.GetResponseAsync<JobSubmissionAccepted>(
                request,
                cancellationToken);
            ContainerJobDiscovery.JobSnapshot executed = await observation.Executed.Task
                .WaitAsync(timeout, cancellationToken);
            IPublishedMessage<JobCompleted<ContainerJobDiscovery.CrunchNumbers>> completed = await harness.Published
                .SelectAsync<JobCompleted<ContainerJobDiscovery.CrunchNumbers>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(request.CorrelationId, executed.CorrelationId);
            Assert.Equal(41, executed.Value);
            Assert.NotEqual(Guid.Empty, executed.JobId);
            Assert.Equal(accepted.Message.JobId, executed.JobId);
            Assert.Equal(executed.JobId, completed.Context.Message.JobId);
            Assert.Equal(request, completed.Context.Message.Job);
            Assert.Equal(
                KebabCaseEndpointNameFormatter.Instance.Consumer<ContainerJobDiscovery.CrunchNumbersConsumer>(),
                accepted.SourceAddress!.AbsolutePath.Trim('/'));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }
}
