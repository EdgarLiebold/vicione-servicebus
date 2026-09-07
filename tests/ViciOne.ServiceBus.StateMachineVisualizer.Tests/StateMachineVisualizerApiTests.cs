using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Visualizer;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineVisualizerApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-API", "sealed-generators-with-canonical-generate-method")]
    public void Generators_ExposeSealedGenerateOnlyShape()
    {
        Assembly assembly = typeof(StateMachineGraphvizGenerator).Assembly;
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Visualizer.Abstractions.StateMachineGenerator", throwOnError: false));

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

    static void AssertGeneratorShape(Type generatorType)
    {
        Assert.True(generatorType.IsPublic);
        Assert.True(generatorType.IsSealed);

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
