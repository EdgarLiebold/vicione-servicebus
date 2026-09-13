using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobServicePublicConfigurationApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-REGISTRATION-API", "local-runtime-options-endpoint-and-null-boundaries")]
    public void AddJobService_AppliesOptionsAndExposesAFunctionalRegistrationFacade()
    {
        var services = new ServiceCollection();
        IJobServiceRegistrationConfigurator? facade = null;
        services.AddViciOneServiceBus(configuration =>
        {
            facade = configuration.AddJobService(options =>
            {
                options.HeartbeatInterval = TimeSpan.FromSeconds(17);
                options.RejectedJobDelay = TimeSpan.FromSeconds(23);
            });
            facade.ConfigureEndpoint(endpoint => endpoint.ConcurrentMessageLimit = 7);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        JobConsumerOptions options = provider.GetRequiredService<IOptions<JobConsumerOptions>>().Value;
        IConsumerKindHost host = Assert.Single(provider.GetServices<IConsumerKindHost>());

        Assert.NotNull(facade);
        Assert.Equal(TimeSpan.FromSeconds(17), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromSeconds(23), options.RejectedJobDelay);
        Assert.Equal(7, host.EndpointDefinition.ConcurrentMessageLimit);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            JobServiceRegistrationExtensions.AddJobService(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-REGISTRATION-API", "facade-forwards-runtime-and-endpoint-configuration")]
    public void JobServiceRegistrationFacade_ForwardsConfigurationAndRejectsNullCallbacks()
    {
        var endpointRegistration = new JobServiceRegistration();
        IJobServiceRegistrationConfigurator endpointFacade = new JobServiceRegistrationConfigurator(endpointRegistration);
        Assert.Same(endpointFacade, endpointFacade.ConfigureOptions(_ => { }));
        endpointFacade.ConfigureEndpoint(endpoint => endpoint.PrefetchCount = 13);

        Assert.Equal(13, endpointRegistration.EndpointDefinition.PrefetchCount);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            endpointFacade.ConfigureOptions(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            endpointFacade.ConfigureEndpoint(null!)).ParamName);

        var invalidRegistration = new JobServiceRegistration();
        IJobServiceRegistrationConfigurator invalidFacade = new JobServiceRegistrationConfigurator(invalidRegistration);
        invalidFacade.ConfigureOptions(options => options.HeartbeatInterval = TimeSpan.Zero);

        Assert.Throws<ConfigurationException>(() => _ = invalidRegistration.EndpointDefinition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-REGISTRATION-API", "saga-options-three-registrations-and-repository-provider")]
    public void AddJobSagaStateMachines_RegistersTheCompleteCoordinatorAndItsSharedProvider()
    {
        var services = new ServiceCollection();
        var repositoryProvider = new RecordingSagaRepositoryRegistrationProvider();
        IJobSagaRegistrationConfigurator? facade = null;
        services.AddViciOneServiceBus(configuration =>
        {
            facade = configuration.AddJobSagaStateMachines(options =>
            {
                options.ConcurrentMessageLimit = 9;
                options.FinalizeCompleted = false;
            });
            Assert.Same(facade, facade.UseRepositoryRegistrationProvider(repositoryProvider));
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        JobSagaOptions options = provider.GetRequiredService<IOptions<JobSagaOptions>>().Value;
        Type[] sagaTypes = provider.GetServices<ISagaRegistration>()
            .Select(static registration => registration.Type)
            .Where(type => type == typeof(JobSaga) || type == typeof(JobTypeSaga) || type == typeof(JobAttemptSaga))
            .OrderBy(static type => type.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(9, options.ConcurrentMessageLimit);
        Assert.False(options.FinalizeCompleted);
        Assert.Equal([typeof(JobAttemptSaga), typeof(JobSaga), typeof(JobTypeSaga)], sagaTypes);
        Assert.Equal([typeof(JobAttemptSaga), typeof(JobSaga), typeof(JobTypeSaga)], repositoryProvider.SagaTypes.OrderBy(static type => type.Name));
        Assert.Equal("registrationProvider", Assert.Throws<ArgumentNullException>(() =>
            facade!.UseRepositoryRegistrationProvider(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => facade!.ConfigureEndpoints(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => facade!.ConfigureJobAttemptEndpoint(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => facade!.ConfigureJobEndpoint(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => facade!.ConfigureJobTypeEndpoint(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            JobServiceRegistrationExtensions.AddJobSagaStateMachines(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-REGISTRATION-API", "distribution-strategy-first-registration-and-scoped-lifetime")]
    public void TryAddJobDistributionStrategy_PreservesTheFirstScopedRegistration()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.TryAddJobDistributionStrategy<FirstDistributionStrategy>());
        Assert.Same(services, services.TryAddJobDistributionStrategy<SecondDistributionStrategy>());
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        IJobDistributionStrategy first = firstScope.ServiceProvider.GetRequiredService<IJobDistributionStrategy>();
        Assert.IsType<FirstDistributionStrategy>(first);
        Assert.Same(first, firstScope.ServiceProvider.GetRequiredService<IJobDistributionStrategy>());
        Assert.NotSame(first, secondScope.ServiceProvider.GetRequiredService<IJobDistributionStrategy>());
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
            JobServiceRegistrationExtensions.TryAddJobDistributionStrategy<FirstDistributionStrategy>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-REGISTRATION-API", "registered-custom-and-conventional-endpoint-identities")]
    public void IsJobServiceEndpoint_RecognizesDefinitionsConventionsAndInputBoundaries()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.SetKebabCaseEndpointNameFormatter();
            configuration.SetInMemorySagaRepositoryProvider();
            configuration.AddJobSagaStateMachines()
                .ConfigureJobEndpoint(endpoint => endpoint.Name = "jobs-coordinator")
                .ConfigureJobTypeEndpoint(endpoint => endpoint.Name = "job-types-coordinator")
                .ConfigureJobAttemptEndpoint(endpoint => endpoint.Name = "job-attempts-coordinator");
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        IBusRegistrationContext context = provider.GetRequiredService<IBusRegistrationContext>();

        Assert.True(context.IsJobServiceEndpoint("JOBS-COORDINATOR"));
        Assert.True(context.IsJobServiceEndpoint("JOB-TYPES-COORDINATOR"));
        Assert.True(context.IsJobServiceEndpoint("JOB-ATTEMPTS-COORDINATOR"));
        Assert.True(context.IsJobServiceEndpoint(context.EndpointNameFormatter.Saga<JobSaga>()));
        Assert.True(context.IsJobServiceEndpoint(context.EndpointNameFormatter.Saga<JobTypeSaga>()));
        Assert.True(context.IsJobServiceEndpoint(context.EndpointNameFormatter.Saga<JobAttemptSaga>()));
        Assert.False(context.IsJobServiceEndpoint("ordinary-consumer"));
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            JobServiceRegistrationExtensions.IsJobServiceEndpoint(null!, "jobs")).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => context.IsJobServiceEndpoint(null!)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => context.IsJobServiceEndpoint(" ")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DIRECT-CONFIGURATION", "service-options-and-container-repositories")]
    public void DirectConfigurationHelpers_PreserveOwnershipAndRejectMissingInputs()
    {
        var options = new ServiceInstanceOptions();
        Assert.Same(options, options.EnableJobServiceEndpoints());
        Assert.True(options.TryGetOptions(out JobServiceOptions first));
        Assert.Same(first, options.EnableJobServiceEndpoints().Options<JobServiceOptions>());
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            JobServiceInstanceOptionsExtensions.EnableJobServiceEndpoints(null!)).ParamName);

        IJobServiceConfigurator configurator = DispatchProxy.Create<IJobServiceConfigurator, JobServiceConfiguratorProxy>();
        var proxy = (JobServiceConfiguratorProxy)(object)configurator;
        TestRegistrationContext context = DispatchProxy.Create<TestRegistrationContext, PassiveRegistrationContextProxy>();

        Assert.Same(configurator, configurator.ConfigureSagaRepositories(context));
        Assert.IsType<DependencyInjectionSagaRepository<JobTypeSaga>>(proxy.Values[nameof(IJobServiceConfigurator.JobTypeRepository)]);
        Assert.IsType<DependencyInjectionSagaRepository<JobSaga>>(proxy.Values[nameof(IJobServiceConfigurator.JobRepository)]);
        Assert.IsType<DependencyInjectionSagaRepository<JobAttemptSaga>>(proxy.Values[nameof(IJobServiceConfigurator.JobAttemptRepository)]);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            JobServiceContainerConfigurationExtensions.ConfigureSagaRepositories(null!, context)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => configurator.ConfigureSagaRepositories(null!)).ParamName);
    }

    private interface TestRegistrationContext : IRegistrationContext, ISetScopedConsumeContext;

    private sealed class FirstDistributionStrategy : IJobDistributionStrategy
    {
        public Task<Uri?> SelectInstanceAsync(
            ConsumeContext<IAllocateJobSlot> requestContext,
            JobDistributionContext distributionContext,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Uri?>(null);
    }

    private sealed class SecondDistributionStrategy : IJobDistributionStrategy
    {
        public Task<Uri?> SelectInstanceAsync(
            ConsumeContext<IAllocateJobSlot> requestContext,
            JobDistributionContext distributionContext,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Uri?>(null);
    }

    private sealed class RecordingSagaRepositoryRegistrationProvider : ISagaRepositoryRegistrationProvider
    {
        public List<Type> SagaTypes { get; } = [];

        public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
            where TSaga : class, ISaga
        {
            ArgumentNullException.ThrowIfNull(configurator);
            SagaTypes.Add(typeof(TSaga));
            configurator.InMemoryRepository();
        }
    }

    private class JobServiceConfiguratorProxy : DispatchProxy
    {
        public Dictionary<string, object> Values { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name.StartsWith("set_", StringComparison.Ordinal))
            {
                Values[targetMethod.Name[4..]] = args![0]!;
                return null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class PassiveRegistrationContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
