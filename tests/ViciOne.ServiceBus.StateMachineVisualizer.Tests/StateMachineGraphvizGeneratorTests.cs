using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Visualizer;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineGraphvizGeneratorTests
{
    private const string Expected = """
        digraph G {
        0 [shape=ellipse, label="Initial"];
        1 [shape=ellipse, label="Running"];
        2 [shape=ellipse, label="Failed"];
        3 [shape=ellipse, label="Final"];
        4 [shape=ellipse, label="Suspended"];
        5 [shape=rectangle, label="Initialized"];
        6 [shape=rectangle, label="Exception"];
        7 [shape=rectangle, label="Finished"];
        8 [shape=rectangle, label="Suspend"];
        9 [shape=rectangle, label="Resume"];
        10 [shape=rectangle, label="Restart<RestartData>"];
        0 -> 5;
        1 -> 7;
        1 -> 8;
        2 -> 10;
        4 -> 9;
        5 -> 1;
        5 -> 6;
        6 -> 2;
        7 -> 3;
        8 -> 4;
        9 -> 1;
        10 -> 1;
        }
        """;

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-GRAPHVIZ", "canonical-output")]
    public void CanonicalGraph_RendersTheExactDotContract()
    {
        string output = new StateMachineGraphvizGenerator(StateMachineGraphFixtures.Canonical())
            .Generate();

        Assert.Equal(StateMachineGraphFixtures.PlatformLines(Expected), output);
    }
}
