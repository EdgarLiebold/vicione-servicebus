using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.CodeFixes.Tests;

public sealed class CancellationTokenForwardingBoundaryTests
{
    private const string Prefix = ServiceBusCodeFixFixture.Usings +
        "using System.Threading;\n" + ServiceBusCodeFixFixture.SimpleMessageContracts;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "additional-token-preserves-existing-token-position")]
    public async Task AdditionalTokenOverload_PreservesExistingArgumentAndBindsPipelineTokenAsync(bool hasExistingToken)
    {
        var existingParameter = hasExistingToken ? ", CancellationToken existingToken" : string.Empty;
        var existingArgument = hasExistingToken ? ", CancellationToken.None" : string.Empty;
        var source = Prefix + $$"""

            namespace ConsoleApplication1
            {
                public sealed class Consumer : IConsumer<SubmitOrder>
                {
                    public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
                    {
                        return WorkAsync(7{{existingArgument}});
                    }

                    private static Task WorkAsync(int value{{existingParameter}}) => Task.CompletedTask;
                    private static Task WorkAsync(int value{{existingParameter}}, CancellationToken cancellationToken) => Task.CompletedTask;
                }
            }
            """;

        var token = TestContext.Current.CancellationToken;
        var analyzer = new CancellationTokenOverloadMethodAnalyzer();
        var diagnostic = Assert.Single(await RoslynTestHost.AnalyzeAsync(
            source, analyzer, ServiceBusCodeFixFixture.ReferenceRoots, token));
        Assert.Equal(CancellationTokenOverloadMethodAnalyzer.CancellationTokenOverloadMethodRuleId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("context.CancellationToken", diagnostic.Message);
        Assert.Contains("WorkAsync", diagnostic.Message);

        // The public host registers the real provider's action, applies its changes and
        // rejects compiler errors before returning the resulting source.
        var fixedSource = await RoslynTestHost.ApplyAllFixesAsync(
            source, analyzer, new CancellationTokenOverloadMethodFixer(),
            ServiceBusCodeFixFixture.ReferenceRoots, token);
        var root = SyntaxFactory.ParseSyntaxTree(
            fixedSource, new CSharpParseOptions(LanguageVersion.CSharp14), cancellationToken: token).GetRoot(token);
        var invocation = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>());
        Assert.Equal("WorkAsync", invocation.Expression.ToString());
        Assert.Equal(hasExistingToken ? 3 : 2, invocation.ArgumentList.Arguments.Count);

        if (hasExistingToken)
            Assert.Equal("CancellationToken.None", invocation.ArgumentList.Arguments[1].Expression.ToString());

        Assert.Equal("context.CancellationToken", invocation.ArgumentList.Arguments[^1].Expression.ToString());
        Assert.Empty(await RoslynTestHost.AnalyzeAsync(
            fixedSource, analyzer, ServiceBusCodeFixFixture.ReferenceRoots, token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "escaped-keyword-context-token-produces-binding-compilation")]
    public async Task KeywordContextParameter_ProducesCompilableEscapedTokenForwardingAsync(bool escapedKeyword)
    {
        var contextName = escapedKeyword ? "@class" : "context";
        var source = Prefix + $$"""

            namespace ConsoleApplication1
            {
                public sealed class Consumer : IConsumer<SubmitOrder>
                {
                    public Task ConsumeAsync(ConsumeContext<SubmitOrder> {{contextName}})
                    {
                        return Task.Delay(10);
                    }
                }
            }
            """;

        var token = TestContext.Current.CancellationToken;
        var analyzer = new CancellationTokenOverloadMethodAnalyzer();
        var diagnostic = Assert.Single(await RoslynTestHost.AnalyzeAsync(
            source, analyzer, ServiceBusCodeFixFixture.ReferenceRoots, token));
        Assert.Equal(CancellationTokenOverloadMethodAnalyzer.CancellationTokenOverloadMethodRuleId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("CancellationToken", diagnostic.Message);
        Assert.Contains("Delay", diagnostic.Message);

        string? fixedSource = null;
        var applicationFailure = await Record.ExceptionAsync(async () =>
        {
            fixedSource = await RoslynTestHost.ApplyAllFixesAsync(
                source, analyzer, new CancellationTokenOverloadMethodFixer(),
                ServiceBusCodeFixFixture.ReferenceRoots, token);
        });

        // A valid original compilation and real diagnostic already exist. Any compiler
        // failure here belongs to the actual registered code action's resulting source.
        Assert.Null(applicationFailure);
        Assert.NotNull(fixedSource);
        var root = SyntaxFactory.ParseSyntaxTree(
            fixedSource, new CSharpParseOptions(LanguageVersion.CSharp14), cancellationToken: token).GetRoot(token);
        var invocation = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>());
        Assert.Equal("Task.Delay", invocation.Expression.ToString());
        Assert.Equal(2, invocation.ArgumentList.Arguments.Count);
        Assert.Equal("10", invocation.ArgumentList.Arguments[0].Expression.ToString());
        Assert.Equal($"{contextName}.CancellationToken", invocation.ArgumentList.Arguments[1].Expression.ToString());
        Assert.Empty(await RoslynTestHost.AnalyzeAsync(
            fixedSource, analyzer, ServiceBusCodeFixFixture.ReferenceRoots, token));
    }
}
