namespace ViciOne.ServiceBus.Quartz.Tests.Testing;

internal static class QuartzJobServiceTestBus
{
    public static Task<QuartzTestBus> StartAsync<TJob, TConsumer>(
        TimeSpan timeout,
        TConsumer consumer,
        Action<JobOptions<TJob>> configureJob,
        Action<IInMemoryBusFactoryConfigurator>? configureAdditionalEndpoints = null,
        Action<Uri>? captureServiceAddress = null,
        Action<IJobServiceConfigurator>? configureJobService = null)
        where TJob : class
        where TConsumer : class, IJobConsumer<TJob>
    {
        return QuartzTestBus.StartAsync(timeout, configure: configurator =>
        {
            configureAdditionalEndpoints?.Invoke(configurator);

            var options = new ServiceInstanceOptions()
                .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);

            configurator.ServiceInstance(options, instance =>
            {
                instance.ConfigureJobServiceEndpoints(configureJobService);
                instance.ReceiveEndpoint(instance.EndpointNameFormatter.Message<TJob>(), endpoint =>
                {
                    endpoint.Consumer(() => consumer, registration =>
                        registration.Options<JobOptions<TJob>>(configureJob));
                    captureServiceAddress?.Invoke(endpoint.InputAddress);
                });
            });
        });
    }
}
