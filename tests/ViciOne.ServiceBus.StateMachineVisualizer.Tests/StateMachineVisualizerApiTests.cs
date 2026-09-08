using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineVisualizerApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-API", "sealed-generators-with-canonical-generate-method")]
    public void Generators_ExposeSealedGenerateOnlyShape()
    {
        Assembly assembly = typeof(StateMachineGraphvizGenerator).Assembly;
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.StateMachineVisualizer.Abstractions.StateMachineGenerator", throwOnError: false));

        AssertGeneratorShape(typeof(StateMachineGraphvizGenerator));
        AssertGeneratorShape(typeof(StateMachineMermaidGenerator));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-API", "null-graph-rejected-by-every-generator")]
    public void Constructors_RejectNullGraph()
    {
        ArgumentNullException graphviz = Assert.Throws<ArgumentNullException>(() => new StateMachineGraphvizGenerator(null!));
        ArgumentNullException mermaid = Assert.Throws<ArgumentNullException>(() => new StateMachineMermaidGenerator(null!));

        Assert.Equal("graph", graphviz.ParamName);
        Assert.Equal("graph", mermaid.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-API", "package-aligned-namespace")]
    public void PublicTypes_UseThePackageAndAssemblyNamespace()
    {
        Assembly assembly = typeof(StateMachineGraphvizGenerator).Assembly;

        Assert.Equal("ViciOne.ServiceBus.StateMachineVisualizer", assembly.GetName().Name);
        Assert.All(
            assembly.GetExportedTypes(),
            type => Assert.Equal("ViciOne.ServiceBus.StateMachineVisualizer", type.Namespace));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Visualizer.StateMachineGraphvizGenerator", throwOnError: false));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Visualizer.StateMachineMermaidGenerator", throwOnError: false));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-API", "repeatable-concurrent-generation")]
    public void Generators_ProduceStableOutputDuringConcurrentUse()
    {
        var graphviz = new StateMachineGraphvizGenerator(StateMachineGraphFixtures.Canonical());
        var mermaid = new StateMachineMermaidGenerator(StateMachineGraphFixtures.Canonical());
        string expectedGraphviz = graphviz.Generate();
        string expectedMermaid = mermaid.Generate();
        string[] graphvizResults = new string[32];
        string[] mermaidResults = new string[32];

        Parallel.For(
            0,
            graphvizResults.Length,
            index =>
            {
                graphvizResults[index] = graphviz.Generate();
                mermaidResults[index] = mermaid.Generate();
            });

        Assert.All(graphvizResults, result => Assert.Equal(expectedGraphviz, result));
        Assert.All(mermaidResults, result => Assert.Equal(expectedMermaid, result));
    }

    static void AssertGeneratorShape(Type generatorType)
    {
        Assert.True(generatorType.IsPublic);
        Assert.True(generatorType.IsSealed);
        Assert.Equal("ViciOne.ServiceBus.StateMachineVisualizer", generatorType.Namespace);

        ConstructorInfo constructor = Assert.Single(generatorType.GetConstructors(BindingFlags.Instance | BindingFlags.Public));
        ParameterInfo graph = Assert.Single(constructor.GetParameters());
        Assert.Equal("graph", graph.Name);
        Assert.Equal(typeof(StateMachineGraph), graph.ParameterType);

        MethodInfo generate = Assert.Single(generatorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        Assert.Equal("Generate", generate.Name);
        Assert.Equal(typeof(string), generate.ReturnType);
        Assert.Empty(generate.GetParameters());
    }
}
