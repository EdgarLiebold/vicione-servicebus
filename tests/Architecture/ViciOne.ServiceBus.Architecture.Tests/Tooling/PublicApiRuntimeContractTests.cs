using System.Reflection;
using System.Text.Json;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Tooling;

public sealed class PublicApiRuntimeContractTests
{
    private static string ToolProject => Path.Combine(
        RepositoryLayout.Root, "tools", "public-api-baseline", "ViciOne.ServiceBus.Build.PublicApiBaseline.csproj");

    private static string Configuration => typeof(PublicApiRuntimeContractTests).Assembly
        .GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "inventory-host-evaluates-the-signalr-shared-framework")]
    public void InventoryHost_EvaluatesTheSignalRSharedFramework()
        => Assert.Contains(
            "Microsoft.AspNetCore.App",
            MsBuildEvaluation.ItemIdentities(ToolProject, "FrameworkReference", Configuration));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "inventory-host-materializes-the-signalr-shared-framework-in-runtime-config")]
    public void InventoryHost_MaterializesTheSignalRSharedFrameworkInRuntimeConfig()
    {
        string assemblyPath = MsBuildEvaluation.PropertyOf(ToolProject, "TargetPath", Configuration);
        string runtimeConfiguration = Path.ChangeExtension(assemblyPath, "runtimeconfig.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(runtimeConfiguration));
        JsonElement options = document.RootElement.GetProperty("runtimeOptions");
        string[] frameworks = options.TryGetProperty("frameworks", out JsonElement multiple)
            ? multiple.EnumerateArray().Select(framework => framework.GetProperty("name").GetString()!).ToArray()
            : [options.GetProperty("framework").GetProperty("name").GetString()!];

        Assert.Equal(["Microsoft.AspNetCore.App", "Microsoft.NETCore.App"], frameworks.Order(StringComparer.Ordinal));
    }
}
