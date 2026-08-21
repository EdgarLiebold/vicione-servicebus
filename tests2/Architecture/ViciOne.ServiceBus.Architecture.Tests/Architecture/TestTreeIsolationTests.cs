using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace ViciOne.ServiceBus.Architecture.Tests.Architecture;

/// <summary>
/// Isolation rules over the real compiled assemblies, expressed with the framework-neutral
/// ArchUnitNET core and asserted with ordinary xUnit.
/// </summary>
/// <remarks>
/// The ArchUnitNET.xUnit adapter is deliberately not referenced. Its fluent <c>Check()</c> would be
/// a second assertion path beside xUnit's, with its own reporting and its own idea of what a failure
/// is. Evaluating the rule and asserting the result keeps one verdict owner.
/// <para>
/// The rules bind assemblies, not namespace strings. A namespace pattern keeps matching after the
/// thing it named was renamed or moved; an assembly reference cannot, because it is the same object
/// the compiler produced.
/// </para>
/// </remarks>
public sealed class TestTreeIsolationTests
{
    private static readonly ArchitectureModel Model = new ArchLoader()
        .LoadAssemblies(
            ProductAssemblyFacts.Abstractions,
            ProductAssemblyFacts.Core,
            ProductAssemblyFacts.TestingInfrastructure,
            ProductAssemblyFacts.ArchitectureTests)
        .Build();

    private static void AssertHolds(IArchRule rule)
    {
        var failures = rule.Evaluate(Model)
            .Where(result => !result.Passed)
            .Select(result => result.Description)
            .ToArray();

        Assert.Empty(failures);
    }

    [Fact]
    public void ProductTypes_DoNotDependOnTestTypes()
    {
        // The direction that must never invert. A product type reaching into test infrastructure
        // would ship test code to consumers and make the product unbuildable without it.
        AssertHolds(Types().That()
            .ResideInAssembly(ProductAssemblyFacts.Abstractions, ProductAssemblyFacts.Core)
            .Should()
            .NotDependOnAny(Types().That().ResideInAssembly(
                ProductAssemblyFacts.TestingInfrastructure,
                ProductAssemblyFacts.ArchitectureTests)));
    }

    [Fact]
    public void SupportLibraryTypes_DoNotDependOnTheTestProject()
    {
        // Shared infrastructure may be used by a test project; it may never reach back into one.
        // That inversion is how shared infrastructure quietly becomes a test-specific helper.
        AssertHolds(Types().That()
            .ResideInAssembly(ProductAssemblyFacts.TestingInfrastructure)
            .Should()
            .NotDependOnAny(Types().That().ResideInAssembly(ProductAssemblyFacts.ArchitectureTests)));
    }

    [Fact]
    public void CompiledProductAssemblies_ReferenceNoTestAssemblyAndNoTestFramework()
    {
        // The assembly reference table catches what a type-level rule cannot: a reference that no
        // type happens to use yet, and a framework whose assembly is not part of the loaded model.
        foreach (var product in ProductAssemblyFacts.ArchitectureAnchors)
        {
            var references = ProductAssemblyFacts.ReferencedAssemblyNames(product);

            Assert.DoesNotContain("ViciOne.ServiceBus.Tests.Infrastructure", references);
            Assert.DoesNotContain("ViciOne.ServiceBus.Architecture.Tests", references);
            Assert.DoesNotContain(references, name => name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(references, name => name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void CompiledSupportLibrary_ReferencesNoTestFramework()
    {
        // Framework neutrality of ViciOne.ServiceBus.Tests.Infrastructure, read off what it actually compiled
        // against rather than off the absence of a package reference alone.
        var references = ProductAssemblyFacts.ReferencedAssemblyNames(
            ProductAssemblyFacts.TestingInfrastructure);

        Assert.DoesNotContain(references, name => name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, name => name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, name => name.StartsWith("ArchUnitNET", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SupportLibraryPublicTypes_StayInsideItsOwnNamespace()
    {
        // A structural rule the reference table cannot express: shared infrastructure that leaks a
        // public type into a product namespace looks like product API to every consumer and to every
        // later architecture rule. ArchUnitNET evaluates this over the compiled types of the real
        // assembly.
        AssertHolds(Types().That()
            .ResideInAssembly(ProductAssemblyFacts.TestingInfrastructure)
            .And().ArePublic()
            .Should()
            .ResideInNamespaceMatching("^ViciOne\\.ServiceBus\\.Tests\\.Infrastructure($|\\.)"));
    }

    [Fact]
    public void TestAssembly_UsesTheArchUnitCoreWithoutItsXunitAdapter()
    {
        // The positive and the negative half together: the core must be there, the adapter must not.
        // Without the positive half the rule would also pass on a project that dropped ArchUnitNET
        // entirely and stopped checking anything.
        var references = ProductAssemblyFacts.ReferencedAssemblyNames(
            ProductAssemblyFacts.ArchitectureTests);

        Assert.Contains("ArchUnitNET", references);
        Assert.DoesNotContain(
            references,
            name => name.StartsWith("ArchUnitNET.", StringComparison.OrdinalIgnoreCase));
    }
}
