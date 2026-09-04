using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

/// <summary>Physical source layout rules over the evaluated native-test project graph.</summary>
public sealed class NativeTestSourceLayoutTests
{
    [Fact]
    [RequirementCoverage("REQ-TEST-205", "native-source-folders-and-namespaces-mirror")]
    public void EveryNativeTestSourceNamespace_MirrorsItsProjectFolder()
    {
        Assert.NotEmpty(RepositoryLayout.NativeTestProjects);

        var violations = RepositoryLayout.NativeTestProjects
            .SelectMany(FindViolations)
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Native test source layout violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static IEnumerable<string> FindViolations(string project)
    {
        string projectDirectory = Path.GetDirectoryName(project)
            ?? throw new InvalidOperationException($"No directory for {project}.");
        string rootNamespace = MsBuildEvaluation.PropertyOf(project, "RootNamespace");

        if (string.IsNullOrWhiteSpace(rootNamespace))
        {
            yield return $"{RepositoryLayout.RelativeToRoot(project)}: evaluated RootNamespace is empty";
            yield break;
        }

        foreach (string source in MsBuildEvaluation.ItemMetadata(project, "Compile", "FullPath"))
        {
            string fullPath = Path.GetFullPath(source);
            if (Path.GetFileName(fullPath).Equals("GlobalUsings.cs", StringComparison.Ordinal))
                continue;

            string relativePath = Path.GetRelativePath(projectDirectory, fullPath);

            if (relativePath == ".." || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                yield return $"{RepositoryLayout.RelativeToRoot(project)}: linked source is outside its project: {fullPath}";
                continue;
            }

            string? relativeDirectory = Path.GetDirectoryName(relativePath);
            string expectedNamespace = string.IsNullOrEmpty(relativeDirectory) || relativeDirectory == "."
                ? rootNamespace
                : $"{rootNamespace}.{relativeDirectory.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.')}";

            CompilationUnitSyntax syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(fullPath))
                .GetCompilationUnitRoot();
            BaseNamespaceDeclarationSyntax[] declarations = syntax.DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .ToArray();

            if (declarations.Length != 1)
            {
                yield return $"{RepositoryLayout.RelativeToRoot(fullPath)}: expected exactly one namespace declaration, found {declarations.Length}";
                continue;
            }

            string actualNamespace = declarations[0].Name.ToString();

            if (!StringComparer.Ordinal.Equals(expectedNamespace, actualNamespace))
            {
                yield return $"{RepositoryLayout.RelativeToRoot(fullPath)}: expected namespace {expectedNamespace}, found {actualNamespace}";
            }
        }
    }
}
