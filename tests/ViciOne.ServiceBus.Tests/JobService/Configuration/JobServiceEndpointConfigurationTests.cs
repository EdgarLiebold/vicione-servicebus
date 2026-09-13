using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobServiceEndpointConfigurationTests
{
    private static readonly string[] JobEndpoints = ["Job", "JobAttempt", "JobType"];

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ENDPOINT-CONFIGURATION", "containerless-outbox-without-scope")]
    public void ContainerlessConfiguration_AppliesTheOutboxWithoutInventingAScope()
    {
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
        {
            configuration.ServiceInstance(instance => instance.ConfigureJobServiceEndpoints());
        });

        IReadOnlyDictionary<string, HashSet<string>> filters = FiltersByEndpoint(bus);

        foreach (string endpoint in JobEndpoints)
        {
            Assert.Contains(endpoint, filters.Keys);
            HashSet<string> endpointFilters = filters[endpoint];
            Assert.Contains("outbox", endpointFilters);
            Assert.DoesNotContain("scope", endpointFilters);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DIRECT-CONFIGURATION", "explicit-options-overload-preserves-instance-and-values")]
    public void ExplicitOptionsConfiguration_UsesTheSuppliedInstanceAndCompleteOptionSet()
    {
        var options = new JobServiceOptions();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 13, 10, 0, 0, TimeSpan.Zero));
        Func<string, TimeZoneInfo?> timeZoneResolver = static id =>
            id == "factory-zone" ? TimeZoneInfo.Utc : null;
        var jobTypeRepository = new InMemorySagaRepository<JobTypeSaga>();
        var jobRepository = new InMemorySagaRepository<JobSaga>();
        var jobAttemptRepository = new InMemorySagaRepository<JobAttemptSaga>();
        var callbackCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
        {
            configuration.ServiceInstance(instance =>
            {
                IServiceInstanceConfigurator<IInMemoryReceiveEndpointConfigurator> returned =
                    instance.ConfigureJobServiceEndpoints(options, context: null, jobService =>
                    {
                        callbackCount++;
                        jobService.JobTypeEndpointName = "custom-job-type";
                        jobService.JobEndpointName = "custom-job";
                        jobService.JobAttemptEndpointName = "custom-job-attempt";
                        jobService.HeartbeatInterval = TimeSpan.FromSeconds(17);
                        jobService.HeartbeatTimeout = TimeSpan.FromMinutes(3);
                        jobService.RejectedJobDelay = TimeSpan.FromSeconds(5);
                        jobService.TimeProvider = timeProvider;
                        jobService.SlotWaitTime = TimeSpan.FromSeconds(7);
                        jobService.StatusCheckInterval = TimeSpan.FromSeconds(37);
                        jobService.SuspectJobRetryCount = 4;
                        jobService.SuspectJobRetryDelay = TimeSpan.FromSeconds(11);
                        jobService.ConcurrentMessageLimit = 9;
                        jobService.FinalizeCompleted = false;
                        jobService.TimeZoneResolver = timeZoneResolver;
                        jobService.JobTypeRepository = jobTypeRepository;
                        jobService.JobRepository = jobRepository;
                        jobService.JobAttemptRepository = jobAttemptRepository;

                        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
                            jobService.JobTypeRepository = null!).ParamName);
                        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
                            jobService.JobRepository = null!).ParamName);
                        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
                            jobService.JobAttemptRepository = null!).ParamName);
                    });

                Assert.Same(instance, returned);
            });
        });

        IReadOnlyDictionary<string, HashSet<string>> filters = FiltersByEndpoint(bus);

        Assert.Equal(1, callbackCount);
        Assert.Contains("custom-job-type", filters.Keys);
        Assert.Contains("custom-job", filters.Keys);
        Assert.Contains("custom-job-attempt", filters.Keys);
        Assert.Equal(TimeSpan.FromSeconds(17), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromMinutes(3), options.HeartbeatTimeout);
        Assert.Equal(TimeSpan.FromSeconds(5), options.RejectedJobDelay);
        Assert.Same(timeProvider, options.TimeProvider);
        Assert.Equal(TimeSpan.FromSeconds(7), options.SlotWaitTime);
        Assert.Equal(TimeSpan.FromSeconds(37), options.StatusCheckInterval);
        Assert.Equal(4, options.SuspectJobRetryCount);
        Assert.Equal(TimeSpan.FromSeconds(11), options.SuspectJobRetryDelay);
        Assert.Equal(9, options.ConcurrentMessageLimit);
        Assert.False(options.FinalizeCompleted);
        Assert.Same(timeZoneResolver, options.TimeZoneResolver);
        Assert.Equal(TimeZoneInfo.Utc, options.TimeZoneResolver!("factory-zone"));
        Assert.Equal(new Uri("loopback://localhost/custom-job"), ((JobSagaSettings)options).JobSagaEndpointAddress);
        Assert.Equal(new Uri("loopback://localhost/custom-job-type"), ((JobSagaSettings)options).JobTypeSagaEndpointAddress);
        Assert.Equal(new Uri("loopback://localhost/custom-job-attempt"), ((JobSagaSettings)options).JobAttemptSagaEndpointAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DIRECT-CONFIGURATION", "extension-rejects-missing-instance-and-options")]
    public void ConfigureJobServiceEndpoints_RejectsMissingRequiredInputs()
    {
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            JobServiceConfigurationExtensions.ConfigureJobServiceEndpoints<IReceiveEndpointConfigurator>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            JobServiceConfigurationExtensions.ConfigureJobServiceEndpoints<IReceiveEndpointConfigurator>(null!, new JobServiceOptions(), null)).ParamName);

        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
        {
            configuration.ServiceInstance(instance =>
            {
                ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                    instance.ConfigureJobServiceEndpoints(null!, context: null));
                Assert.Equal("options", exception.ParamName);
            });
        });

        Assert.NotNull(bus);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ENDPOINT-CONFIGURATION", "registration-context-outbox-on-all-job-sagas")]
    public async Task RegistrationContext_AppliesTheOutboxToEveryJobSagaEndpointAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.SetInMemorySagaRepositoryProvider();
                configuration.AddJobSagaStateMachines();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureDelayedMessageScheduler();
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
            });

        IReadOnlyDictionary<string, HashSet<string>> filters =
            FiltersByEndpoint(provider.GetRequiredService<IBusControl>());

        foreach (string endpoint in JobEndpoints)
        {
            Assert.Contains(endpoint, filters.Keys);
            HashSet<string> endpointFilters = filters[endpoint];
            Assert.Contains("outbox", endpointFilters);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ENDPOINT-CONFIGURATION", "direct-registration-context-adds-scope-and-is-idempotent")]
    public void DirectRegistrationContext_AddsScopeAndConfiguresEndpointsOnlyOnce()
    {
        var options = new JobServiceOptions();
        using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ServiceInstance(instance =>
                    {
                        var configurator =
                            new JobServiceConfigurator<IInMemoryReceiveEndpointConfigurator>(instance, options);
                        configurator.ConfigureJobServiceEndpoints(context);
                        configurator.ConfigureJobServiceEndpoints(context);
                    });
                });
            })
            .BuildServiceProvider(validateScopes: true);

        IReadOnlyDictionary<string, HashSet<string>> filters =
            FiltersByEndpoint(provider.GetRequiredService<IBusControl>());

        foreach (string endpoint in JobEndpoints)
        {
            Assert.Contains(endpoint, filters.Keys);
            Assert.Contains("scope", filters[endpoint]);
            Assert.Contains("outbox", filters[endpoint]);
        }
        Assert.Equal(JobEndpoints.Length, filters.Keys.Count(JobEndpoints.Contains));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ENDPOINT-CONFIGURATION", "coordination-addresses-fail-before-endpoint-configuration")]
    public void CoordinationAddresses_RejectUseBeforeEndpointConfiguration()
    {
        JobSagaSettings settings = new JobServiceOptions();

        InvalidOperationException job = Assert.Throws<InvalidOperationException>(() => settings.JobSagaEndpointAddress);
        InvalidOperationException jobType = Assert.Throws<InvalidOperationException>(() => settings.JobTypeSagaEndpointAddress);
        InvalidOperationException jobAttempt = Assert.Throws<InvalidOperationException>(() => settings.JobAttemptSagaEndpointAddress);

        Assert.Contains("job endpoint", job.Message, StringComparison.Ordinal);
        Assert.Contains("job-type endpoint", jobType.Message, StringComparison.Ordinal);
        Assert.Contains("job-attempt endpoint", jobAttempt.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ENDPOINT-CONFIGURATION", "partitioned-receive-rejects-unsupported-transport")]
    public void PartitionedReceiveMode_RejectsATransportWithoutTheCapability()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.SetInMemorySagaRepositoryProvider();
                configuration.AddJobSagaStateMachines()
                    .UsePartitionedReceiveMode();
                configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
            })
            .BuildServiceProvider();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => provider.GetRequiredService<IBusControl>());

        Assert.Contains("does not support partitioned receive processing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Use a transport that supports partitioned receive processing", exception.Message, StringComparison.Ordinal);
    }

    private static Dictionary<string, HashSet<string>> FiltersByEndpoint(IBusControl bus)
    {
        IProbeResult probe = bus.GetProbeResult();
        IReadOnlyDictionary<string, object> busScope = Scope(probe.Results, "bus");
        IReadOnlyDictionary<string, object> hostScope = Scope(busScope, "host");
        object endpointValue = Assert.Contains("receiveEndpoint", hostScope);
        IEnumerable<IReadOnlyDictionary<string, object>> endpoints = endpointValue switch
        {
            IReadOnlyDictionary<string, object> single => [single],
            IEnumerable<IReadOnlyDictionary<string, object>> multiple => multiple,
            _ => throw new Xunit.Sdk.XunitException(
                $"The receiveEndpoint probe node has unsupported type '{endpointValue.GetType()}'."),
        };

        return endpoints.ToDictionary(
            endpoint => Assert.IsType<string>(Assert.Contains("name", endpoint)),
            endpoint => FilterTypes(endpoint, new HashSet<string>(StringComparer.Ordinal)));
    }

    private static IReadOnlyDictionary<string, object> Scope(IReadOnlyDictionary<string, object> parent, string key) =>
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(Assert.Contains(key, parent));

    private static HashSet<string> FilterTypes(object node, HashSet<string> found)
    {
        switch (node)
        {
            case IReadOnlyDictionary<string, object> dictionary:
                foreach ((string name, object value) in dictionary)
                {
                    if (name == "filterType" && value is string filterType)
                        found.Add(filterType);
                    else
                        FilterTypes(value, found);
                }

                break;
            case IEnumerable<object> sequence:
                foreach (object item in sequence)
                    FilterTypes(item, found);

                break;
        }

        return found;
    }
}
