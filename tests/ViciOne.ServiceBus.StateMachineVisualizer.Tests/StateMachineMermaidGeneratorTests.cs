using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Visualizer;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineMermaidGeneratorTests
{
    private const string Expected = """
        flowchart TB;
            0(["Initial"]) --> 5["Initialized"];
            1(["Running"]) --> 7["Finished"];
            1(["Running"]) --> 8["Suspend"];
            2(["Failed"]) --> 10["Restart«RestartData»"];
            4(["Suspended"]) --> 9["Resume"];
            5["Initialized"] --> 1(["Running"]);
            5["Initialized"] --> 6["Exception"];
            6["Exception"] --> 2(["Failed"]);
            7["Finished"] --> 3(["Final"]);
            8["Suspend"] --> 4(["Suspended"]);
            9["Resume"] --> 1(["Running"]);
            10["Restart«RestartData»"] --> 1(["Running"]);
        """;

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-MERMAID", "canonical-output")]
    public void CanonicalGraph_RendersTheExactMermaidContract()
    {
        string output = new StateMachineMermaidGenerator(StateMachineGraphFixtures.Canonical())
            .Generate();

        Assert.Equal(StateMachineGraphFixtures.PlatformLines(Expected), output);
    }
}
