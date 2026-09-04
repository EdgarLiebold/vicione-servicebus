using System.Reflection;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.Requirements;

public sealed class RequirementCoverageProjectionTests
{
    [Fact]
    public void EveryDeclaredRequirement_HasOneExecutingTestCarrier()
    {
        Assembly assembly = typeof(RequirementCoverageProjectionTests).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(
                                  "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.Requirements.RabbitMqLocalIntegrationRequirements.json")
                              ?? throw new InvalidOperationException("The RabbitMQ requirement projection is not embedded.");
        RequirementEntry[] entries = JsonSerializer.Deserialize<RequirementEntry[]>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("The RabbitMQ requirement projection is invalid.");

        Assert.NotEmpty(entries);
        Assert.Equal(entries.Length, entries.Select(entry => (entry.RequirementId, entry.VariantKey)).Distinct().Count());
        foreach (RequirementEntry entry in entries)
        {
            Type type = assembly.GetType(entry.TestType, throwOnError: true)!;
            MethodInfo method = type.GetMethod(entry.TestMethod, BindingFlags.Instance | BindingFlags.Public)
                                ?? throw new InvalidOperationException($"Missing test carrier {entry.TestType}.{entry.TestMethod}.");
            Assert.NotNull(method.GetCustomAttribute<FactAttribute>() ?? (Attribute?)method.GetCustomAttribute<TheoryAttribute>());
            Assert.Contains(
                method.GetCustomAttributes<RequirementCoverageAttribute>(),
                coverage => coverage.RequirementId == entry.RequirementId && coverage.VariantKey == entry.VariantKey);
        }
    }

    private sealed record RequirementEntry(
        string RequirementId,
        string VariantKey,
        string TestAssembly,
        string TestType,
        string TestMethod);
}
