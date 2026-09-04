using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Requirements;

public sealed class RequirementCoverageProjectionTests
{
    private const string ResourceName =
        "ViciOne.ServiceBus.RabbitMq.Tests.Requirements.RabbitMqAddressRequirements.json";

    [Fact]
    public void RabbitMqAddressRequirements_MatchCompiledRequirementMetadata()
    {
        var assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        using var projection = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"Embedded requirement projection '{ResourceName}' is missing.");

        var result = RequirementCoverageProjectionVerifier.Verify(assembly, projection, IsExactlyOneXunitTest);

        Assert.True(result.IsSuccess, result.Diagnostics);
    }

    private static bool IsExactlyOneXunitTest(MethodInfo method) =>
        method.GetCustomAttributesData()
            .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1;
}
