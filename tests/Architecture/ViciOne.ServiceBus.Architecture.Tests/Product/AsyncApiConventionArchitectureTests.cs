using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class AsyncApiConventionArchitectureTests
{
    private static readonly IReadOnlySet<string> AsyncReturnTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "IAsyncEnumerable",
        "IAsyncEnumerator",
        "AsyncPageable",
        "Task",
        "ValueTask",
    };

    [Fact]
    [RequirementCoverage("REQ-VSB-API-ASYNC-NAMING", "all-product-and-test-methods-bidirectional")]
    public void EveryMethodName_MatchesItsAsynchronousContractBidirectionally()
    {
        string[] violations = SourceFiles(RepositoryLayout.ProductProjects.Concat(RepositoryLayout.NativeTestProjects))
            .SelectMany(InspectAsyncNames)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Bidirectional asynchronous method naming violations:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-API-CANCELLATION-SHAPE", "public-token-is-final-parameter")]
    public void EveryPublicApiMethod_PlacesCancellationTokenLast()
    {
        string[] violations = SourceFiles(RepositoryLayout.ProductProjects)
            .SelectMany(InspectCancellationTokenPosition)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Public API CancellationToken parameters that are not last:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> InspectAsyncNames(string path)
    {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
            .GetCompilationUnitRoot();

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (method.ExplicitInterfaceSpecifier is not null || IsExternallyNamedInterfaceMethod(method))
                continue;

            string? violation = AsyncNameViolation(
                path,
                method.Identifier.ValueText,
                method.ReturnType,
                method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                method.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
            if (violation is not null)
                yield return violation;
        }

        foreach (LocalFunctionStatementSyntax method in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
        {
            string? violation = AsyncNameViolation(
                path,
                method.Identifier.ValueText,
                method.ReturnType,
                method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                method.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
            if (violation is not null)
                yield return violation;
        }
    }

    private static string? AsyncNameViolation(
        string path,
        string name,
        TypeSyntax returnType,
        bool hasAsyncModifier,
        int line)
    {
        bool hasAsyncName = name.EndsWith("Async", StringComparison.Ordinal)
            || name.Contains("AsyncCore", StringComparison.Ordinal);
        bool hasAsyncContract = hasAsyncModifier || IsAsynchronousReturnType(returnType);
        if (hasAsyncName == hasAsyncContract)
            return null;

        string expectation = hasAsyncContract
            ? "has an asynchronous contract but its name has no 'Async' marker"
            : "has an 'Async' marker but no asynchronous contract";
        return $"{RepositoryLayout.RelativeToRoot(path)}:{line}: {name} {expectation}.";
    }

    private static IEnumerable<string> InspectCancellationTokenPosition(string path)
    {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
            .GetCompilationUnitRoot();

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!IsExternallyVisible(method))
                continue;

            ParameterSyntax[] parameters = method.ParameterList.Parameters.ToArray();
            int cancellationTokenIndex = Array.FindIndex(parameters, IsCancellationToken);
            if (cancellationTokenIndex < 0
                || IsCancellationToken(parameters[^1])
                || cancellationTokenIndex == 0 && parameters[0].Modifiers.Any(SyntaxKind.ThisKeyword))
                continue;

            int line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return $"{RepositoryLayout.RelativeToRoot(path)}:{line}: {method.Identifier.ValueText} places "
                + $"'{parameters[cancellationTokenIndex].Identifier.ValueText}' before "
                + $"'{parameters[cancellationTokenIndex + 1].Identifier.ValueText}'.";
        }
    }

    private static bool IsExternallyVisible(MethodDeclarationSyntax method)
    {
        bool memberVisible = method.Modifiers.Any(SyntaxKind.PublicKeyword)
            || method.Modifiers.Any(SyntaxKind.ProtectedKeyword)
            || method.Parent is InterfaceDeclarationSyntax;
        if (!memberVisible)
            return false;

        return method.Ancestors().OfType<BaseTypeDeclarationSyntax>().All(type =>
            type.Modifiers.Any(SyntaxKind.PublicKeyword)
            || type.Modifiers.Any(SyntaxKind.ProtectedKeyword));
    }

    private static bool IsExternallyNamedInterfaceMethod(MethodDeclarationSyntax method) =>
        method.Identifier.ValueText == "Execute"
        && method.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.BaseList?.Types
            .Any(type => type.Type.ToString() == "IJob") == true;

    private static bool IsAsynchronousReturnType(TypeSyntax returnType)
    {
        SimpleNameSyntax? name = returnType switch
        {
            SimpleNameSyntax simpleName => simpleName,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right,
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name,
            NullableTypeSyntax nullable => nullable.ElementType switch
            {
                SimpleNameSyntax simpleName => simpleName,
                QualifiedNameSyntax qualifiedName => qualifiedName.Right,
                AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name,
                _ => null,
            },
            _ => null,
        };

        return name is not null
            && AsyncReturnTypes.Contains(name.Identifier.ValueText);
    }

    private static bool IsCancellationToken(ParameterSyntax parameter) =>
        parameter.Type switch
        {
            SimpleNameSyntax name => name.Identifier.ValueText == nameof(CancellationToken),
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText == nameof(CancellationToken),
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText == nameof(CancellationToken),
            _ => false,
        };

    private static IEnumerable<string> SourceFiles(IEnumerable<string> projects) => projects
        .SelectMany(project => MsBuildEvaluation.ItemMetadata(project, "Compile", "FullPath"))
        .Select(Path.GetFullPath)
        .Where(IsRepositorySource)
        .Distinct(RepositoryLayout.PathComparer);

    private static bool IsRepositorySource(string path) =>
        path.StartsWith(RepositoryLayout.Root + Path.DirectorySeparatorChar, RepositoryLayout.PathComparison)
        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", RepositoryLayout.PathComparison)
        && !path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", RepositoryLayout.PathComparison);
}
