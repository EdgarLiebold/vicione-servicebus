namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Requirements;

using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class RequirementCoverageProjectionTests
{
    [Fact]
    public void SqlServerRequirements_ProjectOntoExactlyOneExecutingTest()
    {
        Assembly assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        using Stream projection = assembly.GetManifestResourceStream(
            "ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Requirements.SqlServerTransportRequirements.json")
            ?? throw new InvalidDataException("The embedded SQL Server requirement projection is missing.");

        RequirementCoverageVerificationResult result = RequirementCoverageProjectionVerifier.Verify(
            assembly,
            projection,
            method => method.GetCustomAttributesData()
                .Count(attribute => typeof(FactAttribute).IsAssignableFrom(attribute.AttributeType)) == 1);

        Assert.True(result.IsSuccess, result.Diagnostics);
    }
}
