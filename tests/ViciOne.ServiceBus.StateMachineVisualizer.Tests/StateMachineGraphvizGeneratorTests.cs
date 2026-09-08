using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
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
        6 [shape=rectangle, label="catch System.Exception"];
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

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-GRAPHVIZ", "generic-and-fault-type-labels")]
    public void TypedEvents_RenderReadableGenericAndFaultPayloadNames()
    {
        string output = new StateMachineGraphvizGenerator(StateMachineGraphFixtures.TypedEvents())
            .Generate();

        Assert.Contains("1 [shape=rectangle, label=\"Received<Envelope<Int32>>\"]", output, StringComparison.Ordinal);
        Assert.Contains("2 [shape=rectangle, label=\"Faulted<Envelope<String>>\"]", output, StringComparison.Ordinal);
        Assert.Contains("3 [shape=rectangle, label=\"ArrayReceived<Envelope<Int32>[,]>\"]", output, StringComparison.Ordinal);
        Assert.Contains(
            "4 [shape=rectangle, label=\"catch ViciOne.ServiceBus.StateMachineVisualizer.Tests.GenericGraphFixtureException<ViciOne.ServiceBus.StateMachineVisualizer.Tests.Envelope<System.Int32>>\"]",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            "5 [shape=rectangle, label=\"NestedReceived<GenericGraphFixtureOuter<Int32>.Message<String>>\"]",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            "6 [shape=rectangle, label=\"catch ViciOne.ServiceBus.StateMachineVisualizer.Tests.GenericGraphFixtureOuter<System.Int32>.Failure<System.String>\"]",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            "7 [shape=rectangle, label=\"PairReceived<Pair<Int32, String>>\"]",
            output,
            StringComparison.Ordinal);
        Assert.DoesNotContain('`', output);
        Assert.DoesNotContain("Fault<", output, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-GRAPHVIZ", "syntax-sensitive-label-escaping")]
    public void SyntaxSensitiveLabel_IsEscapedWithoutChangingTheDotStructure()
    {
        string output = new StateMachineGraphvizGenerator(StateMachineGraphFixtures.SyntaxSensitiveLabel())
            .Generate();

        string[] lines = output.Split(Environment.NewLine, StringSplitOptions.None);
        Assert.Equal(5, lines.Length);
        Assert.Equal("digraph G {", lines[0]);
        Assert.Contains("label=\"State \\\" hash# <tag> & slash\\\\ tick` line\\nnext\"", lines[1], StringComparison.Ordinal);
        Assert.Contains("label=\"Quote \\\" hash# <tag> & slash\\\\ tick` line\\nnext\"", lines[2], StringComparison.Ordinal);
        Assert.Equal("0 -> 1;", lines[3]);
        Assert.Equal("}", lines[4]);
    }
}
