#:package Microsoft.CodeAnalysis.CSharp
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false
#:property NuGetLockFilePath=InitializeNonNullableMembers.packages.lock.json

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run InitializeNonNullableMembers.cs -- <compiler-log>");
    return 2;
}

var diagnosticPattern = new Regex(@"^(?<path>.*\.cs)\(\d+,\d+\): error CS8618:.*?[„'](?<member>[^“']+)[“'] (?:must|muss)",
    RegexOptions.Compiled | RegexOptions.CultureInvariant);
var membersByFile = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

foreach (var line in await File.ReadAllLinesAsync(args[0]))
{
    var match = diagnosticPattern.Match(line);
    if (!match.Success)
        continue;

    var path = Path.GetFullPath(match.Groups["path"].Value);
    if (!membersByFile.TryGetValue(path, out var members))
        membersByFile.Add(path, members = new HashSet<string>(StringComparer.Ordinal));
    members.Add(match.Groups["member"].Value);
}

var updatedFiles = 0;
var initializedMembers = 0;
var nullableEvents = 0;

foreach (var pair in membersByFile)
{
    var original = await File.ReadAllTextAsync(pair.Key);
    var tree = CSharpSyntaxTree.ParseText(original, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
    var root = (CompilationUnitSyntax)await tree.GetRootAsync();
    var rewriter = new MemberInitializerRewriter(pair.Value);
    var updated = (CompilationUnitSyntax)rewriter.Visit(root)!;

    if (rewriter.ChangeCount == 0)
    {
        Console.Error.WriteLine($"No declaration found in '{pair.Key}' for: {string.Join(", ", pair.Value)}");
        continue;
    }

    await File.WriteAllTextAsync(pair.Key, updated.ToFullString());
    updatedFiles++;
    initializedMembers += rewriter.InitializedMembers;
    nullableEvents += rewriter.NullableEvents;
}

Console.WriteLine(
    $"Initialized {initializedMembers} framework-owned members and annotated {nullableEvents} events across {updatedFiles} files.");
return 0;

sealed class MemberInitializerRewriter : CSharpSyntaxRewriter
{
    readonly HashSet<string> _members;

    public MemberInitializerRewriter(HashSet<string> members)
    {
        _members = members;
    }

    public int ChangeCount => InitializedMembers + NullableEvents;
    public int InitializedMembers { get; private set; }
    public int NullableEvents { get; private set; }

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        var visited = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node)!;
        var variables = visited.Declaration.Variables;
        var changed = false;

        for (var index = 0; index < variables.Count; index++)
        {
            var variable = variables[index];
            if (!_members.Contains(variable.Identifier.ValueText) || variable.Initializer != null)
                continue;

            variables = variables.Replace(variable, variable.WithInitializer(NullForgivingInitializer()));
            InitializedMembers++;
            changed = true;
        }

        return changed ? visited.WithDeclaration(visited.Declaration.WithVariables(variables)) : visited;
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var visited = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
        if (!_members.Contains(visited.Identifier.ValueText) || visited.Initializer != null)
            return visited;

        InitializedMembers++;
        return visited.WithInitializer(NullForgivingInitializer())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    public override SyntaxNode? VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        var visited = (EventFieldDeclarationSyntax)base.VisitEventFieldDeclaration(node)!;
        if (visited.Declaration.Type is NullableTypeSyntax
            || !visited.Declaration.Variables.Any(variable => _members.Contains(variable.Identifier.ValueText)))
            return visited;

        var originalType = visited.Declaration.Type;
        var nullableType = SyntaxFactory.NullableType(originalType.WithoutTrailingTrivia())
            .WithTrailingTrivia(originalType.GetTrailingTrivia());
        NullableEvents += visited.Declaration.Variables.Count(variable => _members.Contains(variable.Identifier.ValueText));
        return visited.WithDeclaration(visited.Declaration.WithType(nullableType));
    }

    static EqualsValueClauseSyntax NullForgivingInitializer()
    {
        var value = SyntaxFactory.PostfixUnaryExpression(
            SyntaxKind.SuppressNullableWarningExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        return SyntaxFactory.EqualsValueClause(value);
    }
}
