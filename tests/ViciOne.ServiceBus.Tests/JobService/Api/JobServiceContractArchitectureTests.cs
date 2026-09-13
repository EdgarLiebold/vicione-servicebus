using System.Reflection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

public sealed class JobServiceContractArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONTRACT-BOUNDARY", "contracts-do-not-expose-mutable-dictionaries")]
    public void PublicContracts_DoNotExposeMutableDictionaryTypes()
    {
        Type[] contracts = typeof(ISubmitJob<>).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == typeof(ISubmitJob<>).Namespace)
            .ToArray();

        PropertyInfo[] mutableProperties = contracts
            .SelectMany(static type => type.GetProperties())
            .Where(static property => IsMutableDictionary(property.PropertyType))
            .ToArray();

        Assert.Empty(mutableProperties);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONTRACT-BOUNDARY", "serializer-representations-are-internal")]
    public void SerializerRepresentations_AreNotPartOfThePublicApi()
    {
        Type[] exposedRepresentations = typeof(ISubmitJob<>).Assembly.GetExportedTypes()
            .Where(static type => type.Namespace == "ViciOne.ServiceBus.JobService.Messages")
            .ToArray();

        Assert.Empty(exposedRepresentations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONTRACT-BOUNDARY", "configurator-implementation-is-not-public")]
    public void JobServiceConfiguratorImplementation_IsNotPartOfThePublicApi()
    {
        Type[] exportedTypes = typeof(JobOptions<>).Assembly.GetExportedTypes();

        Assert.DoesNotContain(exportedTypes,
            type => type.IsGenericTypeDefinition && type.Name == "JobServiceConfigurator`1");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONTRACT-BOUNDARY", "retry-api-exposes-only-effective-policy-configuration")]
    public void RetryConfiguration_DoesNotAdvertiseDisconnectedObservers()
    {
        MethodInfo method = Assert.Single(typeof(JobOptions<>).GetMethods(),
            candidate => candidate.Name == nameof(JobOptions<object>.ConfigureRetry));
        ParameterInfo parameter = Assert.Single(method.GetParameters());

        Assert.Equal(typeof(Action<IRetryPolicyConfigurator>), parameter.ParameterType);
        Assert.False(typeof(IRetryObserverConnector).IsAssignableFrom(typeof(IRetryPolicyConfigurator)));
    }

    static bool IsMutableDictionary(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
}
