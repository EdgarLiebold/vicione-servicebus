using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineMermaidGeneratorTests
{
    private const string Expected = """
        flowchart TB;
            0(["Initial"]);
            1(["Running"]);
            2(["Failed"]);
            3(["Final"]);
            4(["Suspended"]);
            5["Initialized"];
            6["catch System.Exception"];
            7["Finished"];
            8["Suspend"];
            9["Resume"];
            10["Restart«RestartData»"];
            0 --> 5;
            1 --> 7;
            1 --> 8;
            2 --> 10;
            4 --> 9;
            5 --> 1;
            5 --> 6;
            6 --> 2;
            7 --> 3;
            8 --> 4;
            9 --> 1;
            10 --> 1;
        """;

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-MERMAID", "canonical-output")]
    public void CanonicalGraph_RendersTheExactMermaidContract()
    {
        string output = new StateMachineMermaidGenerator(StateMachineGraphFixtures.Canonical())
            .Generate();

        Assert.Equal(StateMachineGraphFixtures.CanonicalLines(Expected), output);
        Assert.DoesNotContain('\r', output);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-MERMAID", "generic-and-fault-type-labels")]
    public void TypedEvents_RenderReadableGenericAndFaultPayloadNames()
    {
        string output = new StateMachineMermaidGenerator(StateMachineGraphFixtures.TypedEvents())
            .Generate();

        Assert.Contains("1[\"Received«Envelope#60;Int32#62;»\"]", output, StringComparison.Ordinal);
        Assert.Contains("2[\"Faulted«Envelope#60;String#62;»\"]", output, StringComparison.Ordinal);
        Assert.Contains("3[\"ArrayReceived«Envelope#60;Int32#62;#91;,#93;»\"]", output, StringComparison.Ordinal);
        Assert.Contains(
            "4[\"catch ViciOne.ServiceBus.StateMachineVisualizer.Tests.GenericGraphFixtureException#60;ViciOne.ServiceBus.StateMachineVisualizer.Tests.Envelope#60;System.Int32#62;#62;\"]",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            "5[\"NestedReceived«GenericGraphFixtureOuter#60;Int32#62;.Message#60;String#62;»\"]",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            "6[\"catch ViciOne.ServiceBus.StateMachineVisualizer.Tests.GenericGraphFixtureOuter#60;System.Int32#62;.Failure#60;System.String#62;\"]",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            "7[\"PairReceived«Pair#60;Int32, String#62;»\"]",
            output,
            StringComparison.Ordinal);
        Assert.DoesNotContain('`', output);
        Assert.DoesNotContain("Fault#60;", output, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-MERMAID", "syntax-sensitive-label-encoding")]
    public void SyntaxSensitiveLabel_IsEncodedWithoutChangingFlowchartStructure()
    {
        string output = new StateMachineMermaidGenerator(StateMachineGraphFixtures.SyntaxSensitiveLabel())
            .Generate();

        string[] lines = output.Split('\n', StringSplitOptions.None);
        Assert.Equal(4, lines.Length);
        Assert.Equal(
            "    0([\"State #quot; hash#35; #60;tag#62; #38; slash#92; tick#96; line#13;#10;next\"]);",
            lines[1]);
        Assert.Equal(
            "    1[\"Quote #quot; hash#35; #60;tag#62; #38; slash#92; tick#96; line#13;#10;next\"];",
            lines[2]);
        Assert.Equal("    0 --> 1;", lines[3]);
        Assert.DoesNotContain("<tag>", output, StringComparison.Ordinal);
        Assert.DoesNotContain("line\r\nnext", output, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-MERMAID", "control-character-encoding")]
    public void ControlCharacters_AreEncodedAsMermaidEntities()
    {
        string output = new StateMachineMermaidGenerator(StateMachineGraphFixtures.ControlCharacters())
            .Generate();

        const string expected =
            "flowchart TB;\n    0([\"State#10;line#13;tail#0;#9;#8;#12;#31;\"]);\n"
            + "    1[\"Event#10;line#13;tail#0;#9;#8;#12;#31;\"];\n    0 --> 1;";
        Assert.Equal(expected, output);
        Assert.DoesNotContain('\0', output);
        Assert.DoesNotContain('\t', output);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-MERMAID", "unicode-scalar-boundary")]
    public void UnicodeScalars_ArePreservedAndUnpairedSurrogatesAreEncoded()
    {
        string output = new StateMachineMermaidGenerator(StateMachineGraphFixtures.UnicodeBoundary())
            .Generate();

        Assert.Contains("State 😀 high#92;uD800 low#92;uDC00", output, StringComparison.Ordinal);
        Assert.Contains("Event 😀 high#92;uD800 low#92;uDC00", output, StringComparison.Ordinal);
        Assert.DoesNotContain('\ud800', output);
        Assert.DoesNotContain('\udc00', output);
    }
}
