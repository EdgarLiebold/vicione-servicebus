using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class CancellationTokenFlowArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-API-ASYNC-CANCELLATION", "public-method-token-is-consumed-or-forwarded")]
    public void PublicMethods_ConsumeOrForwardEveryDeclaredCancellationToken()
    {
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(RepositoryLayout.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .SelectMany(FindViolations)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Public CancellationToken flow violations:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> FindViolations(string path)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot();
        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!method.Modifiers.Any(SyntaxKind.PublicKeyword))
                continue;

            SyntaxNode? implementation = (SyntaxNode?)method.Body ?? method.ExpressionBody;
            if (implementation is null)
                continue;

            foreach (ParameterSyntax parameter in method.ParameterList.Parameters.Where(IsCancellationToken))
            {
                bool used = implementation.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Any(identifier => identifier.Identifier.ValueText == parameter.Identifier.ValueText
                        && !IsNameOfArgument(identifier));
                if (used)
                    continue;

                FileLinePositionSpan lineSpan = method.GetLocation().GetLineSpan();
                yield return $"{RepositoryLayout.RelativeToRoot(path)}:{lineSpan.StartLinePosition.Line + 1}: "
                    + $"{method.Identifier.ValueText} does not consume or forward '{parameter.Identifier.ValueText}'.";
            }
        }
    }

    private static bool IsCancellationToken(ParameterSyntax parameter) =>
        parameter.Type?.ToString().EndsWith("CancellationToken", StringComparison.Ordinal) == true;

    private static bool IsNameOfArgument(IdentifierNameSyntax identifier) =>
        identifier.Ancestors()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" });
}
