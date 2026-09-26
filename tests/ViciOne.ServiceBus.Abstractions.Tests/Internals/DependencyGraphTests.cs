using ViciOne.ServiceBus.Internals.GraphValidation;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Internals;

public sealed class DependencyGraphTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-GRAPH-VALIDATION", "self-edge-is-a-cycle")]
    public void SelfEdge_IsRejectedWithItsNodeIdentity()
    {
        var graph = new DependencyGraph<string>(1);
        graph.Add("self-loop", "self-loop");

        CyclicGraphException failure = Assert.Throws<CyclicGraphException>(graph.EnsureGraphIsAcyclic);

        Assert.Equal("The dependency graph contains cycles: (self-loop)", failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GRAPH-VALIDATION", "new-cycle-after-success-is-detected")]
    public void EdgeAddedAfterSuccessfulValidation_IsCheckedAgainstExistingEdges()
    {
        var graph = new DependencyGraph<string>(2);
        graph.Add("orders", "invoices");
        graph.EnsureGraphIsAcyclic();

        graph.Add("invoices", "orders");
        CyclicGraphException failure = Assert.Throws<CyclicGraphException>(graph.EnsureGraphIsAcyclic);

        Assert.Contains("orders", failure.Message, StringComparison.Ordinal);
        Assert.Contains("invoices", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GRAPH-VALIDATION", "failed-validation-remains-repeatable")]
    public void RepeatedValidation_StillRejectsTheCycleAndExcludesDisconnectedNodes()
    {
        var graph = new DependencyGraph<string>(4);
        graph.Add("orders", "invoices");
        graph.Add("invoices", "orders");
        graph.Add("audit", "archive");

        CyclicGraphException first = Assert.Throws<CyclicGraphException>(graph.EnsureGraphIsAcyclic);
        CyclicGraphException second = Assert.Throws<CyclicGraphException>(graph.EnsureGraphIsAcyclic);

        Assert.Equal(first.Message, second.Message);
        Assert.Contains("orders", second.Message, StringComparison.Ordinal);
        Assert.Contains("invoices", second.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("audit", second.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("archive", second.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GRAPH-VALIDATION", "shared-descendants-and-disconnected-edges-remain-acyclic")]
    public void SharedDescendantsAndDisconnectedEdges_RemainValidAcrossRepeatedChecks()
    {
        var graph = new DependencyGraph<string>(6);
        graph.EnsureGraphIsAcyclic();
        graph.Add("orders", "invoices");
        graph.Add("orders", "shipping");
        graph.Add("invoices", "archive");
        graph.Add("shipping", "archive");
        graph.Add("audit", "report");

        Assert.Null(Record.Exception(graph.EnsureGraphIsAcyclic));
        Assert.Null(Record.Exception(graph.EnsureGraphIsAcyclic));
    }
}
