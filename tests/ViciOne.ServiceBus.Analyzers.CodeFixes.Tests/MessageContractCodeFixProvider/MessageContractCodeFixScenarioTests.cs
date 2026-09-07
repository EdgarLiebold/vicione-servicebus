using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.MessageContractCodeFixProvider;

public sealed class MessageContractCodeFixScenarioTests
{
    public static IEnumerable<object[]> Cases() =>
        MessageContractScenarioCatalog.All
            .Where(scenario => scenario.HasCodeFix)
            .SelectMany(scenario => scenario.EnumerateForms(), (scenario, form) =>
                new object[] { scenario.Key, form });

    [Theory]
    [MemberData(nameof(Cases))]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-CODEFIX", "canonical-scenario-catalog")]
    public async Task MissingPropertiesFix_AddsOnlyTheRequiredLeafInitializersAsync(
        string scenarioKey,
        MessageSourceForm form)
    {
        var scenario = MessageContractScenarioCatalog.Get(scenarioKey);
        Assert.True(scenario.HasCodeFix);

        var source = scenario.CreateSource(form);
        var before = ReadLeafInitializers(source);
        var fixedSource = await RoslynTestHost.ApplyAllFixesAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            new global::ViciOne.ServiceBus.Analyzers.MessageContractCodeFixProvider(),
            ServiceBusCodeFixFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);
        var after = ReadLeafInitializers(fixedSource);

        foreach (var existing in before)
        {
            Assert.True(after.TryGetValue(existing.Key, out var actual), $"The fix removed '{existing.Key}'.");
            Assert.Equal(existing.Value, actual);
        }

        var expectedAdditions = scenario.ExpectedAddedInitializers.ToDictionary(
            initializer => initializer.Path,
            initializer => NormalizeExpression(initializer.Expression),
            StringComparer.Ordinal);
        var actualAdditions = after
            .Where(initializer => !before.ContainsKey(initializer.Key))
            .ToDictionary(initializer => initializer.Key, initializer => initializer.Value, StringComparer.Ordinal);

        Assert.Equal(
            expectedAdditions.OrderBy(initializer => initializer.Key, StringComparer.Ordinal),
            actualAdditions.OrderBy(initializer => initializer.Key, StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-CODEFIX", "inferred-identifier-is-preserved")]
    public async Task MissingPropertiesFix_PreservesAnInferredIdentifierInitializerAsync()
    {
        var source = ServiceBusCodeFixFixture.Usings + @"
namespace ConsoleApplication1
{
    public interface Order
    {
        Guid Id { get; }
        string CustomerId { get; }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            var id = Guid.Empty;
            await bus.PublishAsync<Order>(/* VSB_TARGET */ new { id });
        }
    }
}
";

        var fixedSource = await RoslynTestHost.ApplyAllFixesAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            new global::ViciOne.ServiceBus.Analyzers.MessageContractCodeFixProvider(),
            ServiceBusCodeFixFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);
        var initializers = ReadLeafInitializers(fixedSource);

        Assert.Equal("id", initializers["id"]);
        Assert.Equal("default(string)", initializers["CustomerId"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-CODEFIX", "qualified-custom-value-type")]
    public async Task MissingPropertiesFix_QualifiesACustomValueTypeOutsideTheCurrentNamespaceAsync()
    {
        var source = ServiceBusCodeFixFixture.Usings + @"
namespace Contracts
{
    public readonly record struct ExternalId(Guid Value);

    public interface Message
    {
        ExternalId Id { get; }
    }
}

namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            await bus.PublishAsync<Contracts.Message>(/* VSB_TARGET */ new { });
        }
    }
}
";

        var fixedSource = await RoslynTestHost.ApplyAllFixesAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            new global::ViciOne.ServiceBus.Analyzers.MessageContractCodeFixProvider(),
            ServiceBusCodeFixFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);
        var initializers = ReadLeafInitializers(fixedSource);

        Assert.Equal("default(Contracts.ExternalId)", initializers["Id"]);
    }

    private static IReadOnlyDictionary<string, string> ReadLeafInitializers(string source)
    {
        var markerIndex = source.IndexOf(MessageContractSourceFactory.TargetMarker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "The source must preserve the canonical target marker.");

        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var target = root.DescendantNodes()
            .OfType<AnonymousObjectCreationExpressionSyntax>()
            .Where(candidate => candidate.SpanStart > markerIndex)
            .MinBy(candidate => candidate.SpanStart)
            ?? throw new InvalidDataException("The marked anonymous-object creation is missing.");
        var leaves = new Dictionary<string, string>(StringComparer.Ordinal);

        AddLeaves(leaves, string.Empty, target);
        return leaves;
    }

    private static void AddLeaves(
        IDictionary<string, string> leaves,
        string path,
        AnonymousObjectCreationExpressionSyntax anonymousObject)
    {
        if (path.Length > 0 && anonymousObject.Initializers.Count == 0)
        {
            Assert.True(
                leaves.TryAdd(path, NormalizeExpression(anonymousObject)),
                $"Duplicate initializer path '{path}'.");
            return;
        }

        foreach (var initializer in anonymousObject.Initializers)
        {
            var name = InitializerName(initializer);
            var childPath = string.IsNullOrEmpty(path) ? name : $"{path}.{name}";

            switch (initializer.Expression)
            {
                case AnonymousObjectCreationExpressionSyntax nested:
                    AddLeaves(leaves, childPath, nested);
                    break;

                case ImplicitArrayCreationExpressionSyntax
                {
                    Initializer.Expressions: [AnonymousObjectCreationExpressionSyntax element, ..]
                }:
                    AddLeaves(leaves, childPath, element);
                    break;

                case InvocationExpressionSyntax invocation
                    when invocation.DescendantNodes()
                        .OfType<AnonymousObjectCreationExpressionSyntax>()
                        .FirstOrDefault() is { } projectedElement:
                    AddLeaves(leaves, childPath, projectedElement);
                    break;

                default:
                    Assert.True(
                        leaves.TryAdd(childPath, NormalizeExpression(initializer.Expression)),
                        $"Duplicate initializer path '{childPath}'.");
                    break;
            }
        }
    }

    private static string InitializerName(AnonymousObjectMemberDeclaratorSyntax initializer) =>
        initializer.NameEquals?.Name.Identifier.ValueText
        ?? initializer.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => throw new InvalidDataException($"Unsupported inferred member '{initializer.Expression}'."),
        };

    private static string NormalizeExpression(string expression) =>
        SyntaxFactory.ParseExpression(expression)
            .NormalizeWhitespace(eol: "\n")
            .ToFullString();

    private static string NormalizeExpression(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax expression) =>
        NormalizeExpression(expression.ToString());
}
