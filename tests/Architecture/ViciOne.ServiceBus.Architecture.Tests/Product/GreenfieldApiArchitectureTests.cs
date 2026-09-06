using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

/// <summary>Rejects compatibility-only identities that have no place in the greenfield product.</summary>
public sealed class GreenfieldApiArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-CAPABILITIES", "standard-unsupported-capability-exceptions")]
    public void UnsupportedCapabilities_UseTheStandardBclContract()
    {
        string[] violations = ProductSources()
            .Where(path => File.ReadAllText(path).Contains("NotImplementedByDesignException", StringComparison.Ordinal))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Unsupported capabilities must use NotSupportedException:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-METADATA", "no-binary-serialization-compatibility")]
    public void ProductDeclarations_HaveNoLegacyBinarySerializationMetadata()
    {
        string[] violations = ProductSources()
            .Where(ContainsSerializableAttribute)
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Legacy binary-serialization attributes remain:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-IDENTITY", "no-former-state-machine-brand")]
    public void ProductSourceAndPackages_HaveNoFormerStateMachineBrand()
    {
        string formerBrand = "Auto" + "matonymous";
        string[] productFiles = ProductSources()
            .Concat(RepositoryLayout.ProductProjects)
            .Distinct(RepositoryLayout.PathComparer)
            .ToArray();
        string[] violations = productFiles
            .Where(path => File.ReadAllText(path).Contains(formerBrand, StringComparison.OrdinalIgnoreCase))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Former state-machine brand remains in product source or package metadata:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static bool ContainsSerializableAttribute(string path)
    {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path))
            .GetCompilationUnitRoot();
        return root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Any(attribute => attribute.Name.ToString() is "Serializable" or "SerializableAttribute"
                or "System.Serializable" or "System.SerializableAttribute");
    }

    private static IEnumerable<string> ProductSources() => RepositoryLayout.ProductProjects
        .SelectMany(project => MsBuildEvaluation.ItemMetadata(project, "Compile", "FullPath"))
        .Select(Path.GetFullPath)
        .Where(path => path.StartsWith(
            RepositoryLayout.Root + Path.DirectorySeparatorChar,
            RepositoryLayout.PathComparison))
        .Distinct(RepositoryLayout.PathComparer);
}
