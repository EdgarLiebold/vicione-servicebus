using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class ConfigurationExceptionArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-DIAGNOSTICS", "every-exception-has-feature-bus-problem-and-fix")]
    public void EveryConfigurationException_UsesTheActionableMessageFactoryOrAnExplicitConformingMessage()
    {
        string[] paths = Directory
            .EnumerateFiles(Path.Combine(RepositoryLayout.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToArray();
        IReadOnlySet<string> configurationExceptions = FindConfigurationExceptionTypes(paths);
        string[] violations = paths
            .SelectMany(path => FindViolations(path, configurationExceptions))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "ConfigurationException message-shape violations:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));

        string normalized = ConfigurationMessages.Create(
            "Reliable messaging",
            "orders-v1",
            "InitialRetryDelay is invalid\nbecause it is negative",
            "Set a positive duration");
        Assert.Equal(
            "Reliable messaging for bus 'orders-v1': InitialRetryDelay is invalid because it is negative. Set a positive duration.",
            normalized);
        Assert.DoesNotContain(Environment.NewLine, normalized, StringComparison.Ordinal);
    }

    static IReadOnlySet<string> FindConfigurationExceptionTypes(IEnumerable<string> paths)
    {
        ClassDeclarationSyntax[] declarations = paths
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
                .GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>())
            .ToArray();
        var result = new HashSet<string>(StringComparer.Ordinal) { "ConfigurationException" };
        bool changed;
        do
        {
            changed = false;
            foreach (ClassDeclarationSyntax declaration in declarations)
            {
                bool derivesFromConfigurationException = declaration.BaseList?.Types.Any(baseType =>
                    baseType.Type.DescendantNodesAndSelf()
                        .OfType<SimpleNameSyntax>()
                        .Any(name => result.Contains(name.Identifier.ValueText))) == true;
                if (derivesFromConfigurationException)
                    changed |= result.Add(declaration.Identifier.ValueText);
            }
        }
        while (changed);

        return result;
    }

    static IEnumerable<string> FindViolations(string path, IReadOnlySet<string> configurationExceptions)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot();
        foreach (ObjectCreationExpressionSyntax creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            string? typeName = creation.Type.DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .LastOrDefault()
                ?.Identifier.ValueText;
            if (typeName is null || !configurationExceptions.Contains(typeName))
                continue;

            bool factory = creation.ArgumentList?.Arguments
                .SelectMany(static argument => argument.Expression.DescendantNodesAndSelf())
                .OfType<InvocationExpressionSyntax>()
                .Any(static invocation => invocation.Expression.ToString().Contains(
                    "ConfigurationMessages.",
                    StringComparison.Ordinal)) == true;
            bool explicitShape = creation.ArgumentList?.Arguments.Any(static argument =>
            {
                string source = argument.Expression.ToString();
                return source.Contains(" for bus '", StringComparison.Ordinal)
                    && source.Contains("':", StringComparison.Ordinal);
            }) == true;
            if (factory || explicitShape)
                continue;

            FileLinePositionSpan span = creation.GetLocation().GetLineSpan();
            yield return $"{RepositoryLayout.RelativeToRoot(path)}:{span.StartLinePosition.Line + 1}";
        }
    }
}
