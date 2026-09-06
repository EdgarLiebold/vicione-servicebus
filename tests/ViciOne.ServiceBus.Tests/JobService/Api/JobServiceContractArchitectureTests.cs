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
        Type[] contracts = typeof(SubmitJob<>).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == typeof(SubmitJob<>).Namespace)
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
        Type[] exposedRepresentations = typeof(SubmitJob<>).Assembly.GetExportedTypes()
            .Where(static type => type.Namespace == "ViciOne.ServiceBus.JobService.Messages")
            .ToArray();

        Assert.Empty(exposedRepresentations);
    }

    static bool IsMutableDictionary(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
}
