using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware;

public sealed class ScopePipeContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "parent-readable-local-write-isolated")]
    public void Scope_ReadsParentPayloadsButKeepsAddsAndUpdatesLocal()
    {
        using var cancellation = new CancellationTokenSource();
        var parent = new TestContext(cancellation.Token);
        var parentPayload = parent.GetOrAddPayload(() => new RootPayload("parent"));
        var scope = new TestScope(parent);
        var inheritedFactoryCalls = 0;

        RootPayload inherited = scope.GetOrAddPayload(() =>
        {
            Interlocked.Increment(ref inheritedFactoryCalls);
            return new RootPayload("unexpected");
        });
        var localPayload = scope.GetOrAddPayload(() => new ScopePayload("scope"));
        RootPayload updated = scope.AddOrUpdatePayload(
            () => new RootPayload("unexpected-add"),
            payload => new RootPayload($"{payload.Value}-updated"));

        Assert.Equal(cancellation.Token, scope.CancellationToken);
        Assert.Same(parentPayload, inherited);
        Assert.Equal(0, inheritedFactoryCalls);
        Assert.True(scope.HasPayloadType(typeof(RootPayload)));
        Assert.True(scope.TryGetPayload(out ScopePayload? observedLocal));
        Assert.Same(localPayload, observedLocal);
        Assert.False(parent.TryGetPayload(out ScopePayload? _));
        Assert.Same(updated, scope.GetOrAddPayload(() => new RootPayload("unexpected")));
        Assert.Equal("parent-updated", updated.Value);
        Assert.Same(parentPayload, parent.GetOrAddPayload(() => new RootPayload("unexpected")));
        Assert.Equal("parent", parentPayload.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "nested-isolation-and-null-parent")]
    public void NestedScope_InheritsNearestPayloadWithoutWritingBackAndRejectsANullParent()
    {
        var parent = new TestContext(CancellationToken.None);
        var firstScope = new TestScopeWithPayloads(parent, new RootPayload("first"));
        var nestedScope = new TestScope(firstScope);
        var nestedPayload = nestedScope.GetOrAddPayload(() => new ScopePayload("nested"));

        Assert.Equal("first", nestedScope.GetOrAddPayload(() => new RootPayload("unexpected")).Value);
        Assert.Same(nestedPayload, nestedScope.GetOrAddPayload(() => new ScopePayload("unexpected")));
        Assert.False(firstScope.TryGetPayload(out ScopePayload? _));
        Assert.False(parent.TryGetPayload(out ScopePayload? _));
        Assert.Throws<ArgumentNullException>(() => new TestScope(null!));
        Assert.Throws<ArgumentNullException>(() => new TestScopeWithPayloads(null!, new object()));
    }

    private sealed class TestContext(CancellationToken cancellationToken) : BasePipeContext(cancellationToken);

    private sealed class TestScope(PipeContext parent) : ScopePipeContext(parent), PipeContext;

    private sealed class TestScopeWithPayloads(PipeContext parent, params object[] payloads) : ScopePipeContext(parent, payloads), PipeContext;

    private sealed record RootPayload(string Value);

    private sealed record ScopePayload(string Value);
}
