using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Analyzers.V5;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests;

public sealed class AnalyzerInstanceStateTests
{
    private static readonly Type[] CompilationBoundTypes =
    [
        typeof(ISymbol),
        typeof(Compilation),
        typeof(SemanticModel),
        typeof(SyntaxNode),
        typeof(SyntaxTree),
        typeof(Location),
        typeof(Diagnostic),
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-ANALYZER-INSTANCE-STATE", "no-compilation-bound-fields")]
    public void ProductAnalyzers_HoldNoCompilationBoundInstanceState()
    {
        var analyzers = AnalyzerTypes().ToArray();
        Assert.NotEmpty(analyzers);

        var offenders = analyzers
            .SelectMany(InstanceFields)
            .Where(field => IsCompilationBound(field.FieldType))
            .Select(field => $"{field.DeclaringType?.Name}.{field.Name} : {Describe(field.FieldType)}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ANALYZER-INSTANCE-STATE", "compilation-bound-field-control")]
    public void CompilationBoundFieldScan_DetectsDirectAndGenericSymbolState()
    {
        var offenders = InstanceFields(typeof(AnalyzerWithCompilationBoundField))
            .Where(field => IsCompilationBound(field.FieldType))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["_members", "_symbol"], offenders);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ANALYZER-INSTANCE-STATE", "complete-analyzer-carrier-set")]
    public void ProductAnalyzerAssembly_ContainsTheExplicitAnalyzerSet()
    {
        var names = AnalyzerTypes()
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                nameof(AsyncMethodAnalyzer),
                nameof(BlockingConsumerCallAnalyzer),
                nameof(CancellationTokenOverloadMethodAnalyzer),
                nameof(ConsumerConcurrencyDeclarationAnalyzer),
                nameof(ConsumerEndpointQosAnalyzer),
                nameof(ExcludedTopologyConsumerAnalyzer),
                nameof(LargeInlinePayloadAnalyzer),
                nameof(MessageContractAnalyzer),
            ],
            names);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ANALYZER-INSTANCE-STATE", "one-instance-multiple-compilations")]
    public async Task OneAnalyzerInstance_ProducesIndependentResultsAcrossCompilationsAsync()
    {
        var analyzer = new CancellationTokenOverloadMethodAnalyzer();

        var first = await AnalyzeAsync(analyzer, CancellableConsumer("FirstOrder"));
        var second = await AnalyzeAsync(analyzer, UncancellableConsumer("SecondOrder"));
        var third = await AnalyzeAsync(analyzer, CancellableConsumer("ThirdOrder"));

        Assert.Equal([CancellationTokenOverloadMethodAnalyzer.CancellationTokenOverloadMethodRuleId], first);
        Assert.Empty(second);
        Assert.Equal([CancellationTokenOverloadMethodAnalyzer.CancellationTokenOverloadMethodRuleId], third);
    }

    private static async Task<IReadOnlyList<string>> AnalyzeAsync(DiagnosticAnalyzer analyzer, string source)
    {
        var diagnostics = await RoslynTestHost.AnalyzeAsync(
            source,
            analyzer,
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        return diagnostics.Select(diagnostic => diagnostic.Id).ToArray();
    }

    private static string CancellableConsumer(string contract) =>
        Source(contract, "return Task.Delay(10);");

    private static string UncancellableConsumer(string contract) =>
        Source(contract, "return Task.CompletedTask;");

    private static string Source(string contract, string body) => $@"
using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ConsoleApplication1
{{
    public interface {contract}
    {{
        Guid Id {{ get; }}
    }}

    class Consumer :
        IConsumer<{contract}>
    {{
        public Task ConsumeAsync(ConsumeContext<{contract}> context)
        {{
            {body}
        }}
    }}
}}
";

    private static IEnumerable<Type> AnalyzerTypes() =>
        typeof(global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type));

    private static IEnumerable<FieldInfo> InstanceFields(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var field in current.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    private static bool IsCompilationBound(Type type)
    {
        if (CompilationBoundTypes.Any(bound => bound.IsAssignableFrom(type)))
        {
            return true;
        }

        if (type.IsArray)
        {
            return IsCompilationBound(type.GetElementType()!);
        }

        return type.IsGenericType && type.GetGenericArguments().Any(IsCompilationBound);
    }

    private static string Describe(Type type) =>
        type.IsGenericType
            ? $"{type.Name}<{string.Join(", ", type.GetGenericArguments().Select(Describe))}>"
            : type.Name;

    private sealed class AnalyzerWithCompilationBoundField
    {
        internal readonly ITypeSymbol _symbol = null!;
        internal readonly Dictionary<string, IMethodSymbol> _members = [];
        internal readonly string _name = string.Empty;
    }
}
