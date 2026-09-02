using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

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
            ProductAssemblyFacts.RoslynTestingInfrastructure,
            ProductAssemblyFacts.AnalyzerTestingInfrastructure,
            ProductAssemblyFacts.ArchitectureTests)
        .Build();

    private static void AssertHolds(IArchRule rule)
    {
        var results = rule.Evaluate(Model).ToArray();
        Assert.NotEmpty(results);

        var failures = results
            .Where(result => !result.Passed)
            .Select(result => result.Description)
            .ToArray();

        Assert.Empty(failures);
    }

    [Fact]
    [RequirementCoverage(
        "REQ-TEST-205",
        "core-and-abstractions-do-not-depend-on-test-types")]
    public void ProductTypes_DoNotDependOnTestTypes()
    {
        // The direction that must never invert. A product type reaching into test infrastructure
        // would ship test code to consumers and make the product unbuildable without it.
        AssertHolds(Types().That()
            .ResideInAssembly(ProductAssemblyFacts.Abstractions, ProductAssemblyFacts.Core)
            .Should()
            .NotDependOnAny(Types().That().ResideInAssembly(
                ProductAssemblyFacts.TestingInfrastructure,
                ProductAssemblyFacts.RoslynTestingInfrastructure,
                ProductAssemblyFacts.AnalyzerTestingInfrastructure,
                ProductAssemblyFacts.ArchitectureTests)));
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-205", "abstractions-do-not-depend-on-core-types")]
    public void AbstractionsTypes_DoNotDependOnCoreTypes()
    {
        // Abstractions is the lower layer. A dependency on the core assembly would invert that
        // direction and form a cycle with the core-to-abstractions reference.
        AssertHolds(Types().That()
            .ResideInAssembly(ProductAssemblyFacts.Abstractions)
            .Should()
            .NotDependOnAny(Types().That().ResideInAssembly(ProductAssemblyFacts.Core)));
    }

    [Fact]
    public void SupportLibraryTypes_DoNotDependOnTheTestProject()
    {
        // Test infrastructure may be used by a test project; it may never reach back into one.
        // That inversion would make reusable infrastructure depend on a specific consumer.
        AssertHolds(Types().That()
            .ResideInAssembly(
                ProductAssemblyFacts.TestingInfrastructure,
                ProductAssemblyFacts.RoslynTestingInfrastructure,
                ProductAssemblyFacts.AnalyzerTestingInfrastructure)
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

            Assert.DoesNotContain(references, name =>
                name.Contains(".Tests", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(references, name => name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(references, name => name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void CompiledCoreTestingInfrastructure_ReferencesNoTestFramework()
    {
        // Framework neutrality is read from the compiled assembly rather than inferred from the
        // absence of a direct package reference.
        foreach (var support in ProductAssemblyFacts.TestingInfrastructureAssemblies)
        {
            var references = ProductAssemblyFacts.ReferencedAssemblyNames(support);

            Assert.DoesNotContain(references, name => name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(references, name => name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(references, name => name.StartsWith("ArchUnitNET", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.Testing", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(references, name => name.Contains(".Tests", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void CoreTestingInfrastructurePublicTypes_StayInsideItsOwnNamespace()
    {
        // A structural rule the reference table cannot express: test infrastructure that leaks a
        // public type into a product namespace looks like product API to every consumer and to every
        // later architecture rule. ArchUnitNET evaluates this over the compiled types of the real
        // assembly.
        AssertHolds(Types().That()
            .ResideInAssembly(
                ProductAssemblyFacts.TestingInfrastructure,
                ProductAssemblyFacts.RoslynTestingInfrastructure,
                ProductAssemblyFacts.AnalyzerTestingInfrastructure)
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
