using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.MessageContractCodeFixProvider;

public sealed class MessageContractCollectionFixTests
{
    [Theory]
    [InlineData("Item[]")]
    [InlineData("List<Item>")]
    [InlineData("IList<Item>")]
    [InlineData("IReadOnlyList<Item>")]
    [InlineData("ICollection<Item>")]
    [InlineData("IEnumerable<Item>")]
    [InlineData("System.Collections.Immutable.ImmutableArray<Item>")]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-CODEFIX", "all-collection-elements-preserve-values-and-order")]
    public async Task MissingPropertiesFix_RepairsEveryArrayElementWithoutChangingExistingValuesAsync(string collectionType)
    {
        await AssertCollectionFixAsync(collectionType,
            "new[] { new { Id = 17 }, new { Id = 29 } }",
            "new[] { new { Id = 17, Price = default(decimal) }, new { Id = 29, Price = default(decimal) } }");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-CODEFIX", "nested-arrays-preserve-all-elements-and-dimensions")]
    public async Task MissingPropertiesFix_RepairsNestedArraysWithoutFlatteningOrDroppingElementsAsync()
    {
        await AssertCollectionFixAsync("IReadOnlyList<IReadOnlyList<Item>>",
            "new[] { new[] { new { Id = 17 }, new { Id = 29 } }, new[] { new { Id = 41 } } }",
            "new[] { new[] { new { Id = 17, Price = default(decimal) }, new { Id = 29, Price = default(decimal) } }, new[] { new { Id = 41, Price = default(decimal) } } }");
    }

    private static async Task AssertCollectionFixAsync(string collectionType, string items, string expectedItems)
    {
        string source = ServiceBusCodeFixFixture.Usings + $$"""
            namespace CollectionContracts
            {
                public interface Message
                {
                    {{collectionType}} Items { get; }
                    string Label { get; }
                }
                public interface Item
                {
                    int Id { get; }
                    decimal Price { get; }
                }
                class Program
                {
                    static async Task Main()
                    {
                        var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
                        await bus.PublishAsync<Message>(new
                        {
                            Items = {{items}},
                            Label = "keep"
                        });
                    }
                }
            }
            """;

        string fixedSource = await RoslynTestHost.ApplyAllFixesAsync(source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            new global::ViciOne.ServiceBus.Analyzers.CodeFixes.MessageContractCodeFixProvider(),
            ServiceBusCodeFixFixture.ReferenceRoots, TestContext.Current.CancellationToken);

        var root = CSharpSyntaxTree.ParseText(fixedSource, cancellationToken: TestContext.Current.CancellationToken)
            .GetRoot(TestContext.Current.CancellationToken);
        var message = root.DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>().First();
        var expected = SyntaxFactory.ParseExpression($$"""
            new
            {
                Items = {{expectedItems}},
                Label = "keep"
            }
            """);
        Assert.Equal(expected.DescendantTokens().Select(token => (token.RawKind, token.Text)),
            message.DescendantTokens().Select(token => (token.RawKind, token.Text)));
        await RoslynTestHost.ValidateCompilationAsync(fixedSource, ServiceBusCodeFixFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);
    }
}
