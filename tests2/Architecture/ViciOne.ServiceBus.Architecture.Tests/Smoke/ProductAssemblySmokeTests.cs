using System.Reflection;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Smoke;

/// <summary>
/// Structural smoke checks over the real compiled product assemblies.
/// </summary>
/// <remarks>
/// Every subject here is an assembly that this build actually produced and loaded, reached through
/// a compile-verified type anchor. Nothing in this file consults a checked-in list of names, so a
/// product assembly cannot disappear or change framework while the tests keep passing.
/// </remarks>
public sealed class ProductAssemblySmokeTests
{
    [Fact]
    public void ProductAssemblies_TargetTheProductFramework()
    {
        // The framework is read off the compiled assembly, not off the project file, so a project
        // that builds for the wrong framework is caught by what it produced rather than by what it
        // declared.
        foreach (var assembly in ProductAssemblyFacts.ArchitectureAnchors)
        {
            Assert.Equal(
                ".NETCoreApp,Version=v10.0",
                ProductAssemblyFacts.TargetFrameworkOf(assembly));
        }
    }

    [Fact]
    public void ProductAssemblies_AreStrongNamed()
    {
        // The product signs its assemblies. An unsigned one would silently break every
        // InternalsVisibleTo grant that the inherited tests still depend on.
        foreach (var assembly in ProductAssemblyFacts.ArchitectureAnchors)
        {
            var publicKey = assembly.GetName().GetPublicKeyToken();

            Assert.NotNull(publicKey);
            Assert.NotEmpty(publicKey);
        }
    }

    [Fact]
    public void AbstractionsAssembly_PublishesTheBusContractPublicly()
    {
        // IBus is the contract every consumer binds to. Narrowing it to internal would compile
        // inside the product and break every consumer outside it.
        var contract = ProductAssemblyFacts.Abstractions.GetType("ViciOne.ServiceBus.IBus", throwOnError: true)!;

        Assert.True(contract.IsInterface);
        Assert.True(contract.IsPublic);
    }

    [Fact]
    public void CoreAssembly_DependsOnAbstractions_NotTheOtherWayAround()
    {
        // The dependency direction is the whole point of splitting the two assemblies. Asserting it
        // on the compiled references catches an accidental reference the project file would hide
        // behind a transitive path.
        var coreReferences = ProductAssemblyFacts.ReferencedAssemblyNames(ProductAssemblyFacts.Core);
        var abstractionsReferences =
            ProductAssemblyFacts.ReferencedAssemblyNames(ProductAssemblyFacts.Abstractions);

        Assert.Contains("ViciOne.ServiceBus.Abstractions", coreReferences);
        Assert.DoesNotContain("ViciOne.ServiceBus", abstractionsReferences);
    }

    [Fact]
    public void ProductAssemblies_ExposePublicApi()
    {
        // An assembly that produced no exported type would still load and still pass a mere
        // "does it exist" check. Counting exported types is what makes an empty build fail.
        foreach (var assembly in ProductAssemblyFacts.ArchitectureAnchors)
        {
            Assert.NotEmpty(assembly.GetExportedTypes());
        }
    }

    [Fact]
    public void ProductAssemblies_AreBuiltInTheSameConfigurationAsTheTests()
    {
        // A profile solution that only reaches a product project through a ProjectReference does not
        // include it in the solution configuration mapping. Building the profile in Release then
        // produced a Release test artifact carrying Debug product assemblies - the run asserted one
        // configuration while executing another. The configuration is read off the compiled
        // attribute, so the artifact itself answers rather than the command line that produced it.
        var testConfiguration = ProductAssemblyFacts.ConfigurationOf(ProductAssemblyFacts.ArchitectureTests);

        Assert.False(string.IsNullOrEmpty(testConfiguration));

        foreach (var assembly in ProductAssemblyFacts.ArchitectureAnchors)
        {
            Assert.Equal(testConfiguration, ProductAssemblyFacts.ConfigurationOf(assembly));
        }
    }

    [Fact]
    public void TestAssembly_RunsOnTheProductFramework()
    {
        Assert.Equal(
            ".NETCoreApp,Version=v10.0",
            ProductAssemblyFacts.TargetFrameworkOf(ProductAssemblyFacts.ArchitectureTests));
    }
}
