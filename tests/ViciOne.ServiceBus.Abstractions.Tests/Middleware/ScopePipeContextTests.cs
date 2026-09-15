using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Payloads;
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "optional-payloads-and-current-parent-cancellation")]
    public void Constructors_AcceptOptionalPayloadsAndReadCurrentParentCancellation(bool withPayloads, bool nullArray)
    {
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        var parent = new MutableCancellationContext { CurrentToken = firstCancellation.Token };
        ScopePipeContext scope = withPayloads
            ? new TestScopeWithPayloads(parent, nullArray ? null : [])
            : new TestScope(parent);
        var expected = new ScopePayload("created");

        Assert.Equal(firstCancellation.Token, scope.CancellationToken);
        parent.CurrentToken = secondCancellation.Token;
        Assert.Equal(secondCancellation.Token, scope.CancellationToken);
        firstCancellation.Cancel();
        Assert.False(scope.CancellationToken.IsCancellationRequested);
        secondCancellation.Cancel();
        Assert.True(scope.CancellationToken.IsCancellationRequested);
        Assert.False(scope.TryGetPayload(out ScopePayload? missing));
        Assert.Null(missing);
        Assert.Same(expected, scope.GetOrAddPayload(() => expected));
        Assert.True(scope.TryGetPayload(out ScopePayload? retained));
        Assert.Same(expected, retained);
        Assert.False(parent.TryGetPayload(out ScopePayload? _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "required-runtime-payload-type")]
    public void HasPayloadType_RejectsANullTypeWithoutChangingLocalOrParentPayloads()
    {
        var parent = new TestContext(CancellationToken.None);
        var parentPayload = parent.GetOrAddPayload(() => new RootPayload("parent"));
        var localPayload = new ScopePayload("local");
        var scope = new TestScopeWithPayloads(parent, localPayload);

        Assert.Equal("payloadType", Assert.Throws<ArgumentNullException>(() => scope.HasPayloadType(null!)).ParamName);
        Assert.True(scope.TryGetPayload(out ScopePayload? local));
        Assert.Same(localPayload, local);
        Assert.True(parent.TryGetPayload(out RootPayload? inherited));
        Assert.Same(parentPayload, inherited);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(0, 3)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 3)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "required-factories-before-self-local-parent-fast-path")]
    public void PayloadFactories_RejectNullBeforeCallbacksOrAnyResolutionPath(int missingFactory, int resolutionPath)
    {
        var parent = new TestContext(CancellationToken.None);
        var parentPayload = new RootPayload("parent");
        var localPayload = new RootPayload("local");
        var parentHasPayload = resolutionPath != 0;
        if (parentHasPayload)
            parent.GetOrAddPayload(() => parentPayload);
        var localHasPayload = resolutionPath is 1 or 3;
        var scope = new TestScopeWithPayloads(parent, localHasPayload ? [localPayload] : []);
        var callbackCalls = 0;
        ArgumentNullException failure;
        if (resolutionPath == 3)
        {
            failure = Assert.Throws<ArgumentNullException>(() => InvokeMissingFactory<PipeContext>(scope, missingFactory,
                () => { callbackCalls++; return scope; },
                value => { callbackCalls++; return value; }));
        }
        else
        {
            failure = Assert.Throws<ArgumentNullException>(() => InvokeMissingFactory(scope, missingFactory,
                () => { callbackCalls++; return localPayload; },
                (RootPayload value) => { callbackCalls++; return value; }));
        }

        Assert.Equal(missingFactory switch { 0 => "payloadFactory", 1 => "addFactory", 2 => "updateFactory", _ => throw new ArgumentOutOfRangeException(nameof(missingFactory)) }, failure.ParamName);
        Assert.Equal(0, callbackCalls);
        Assert.Equal(parentHasPayload, parent.TryGetPayload(out RootPayload? parentObserved));
        Assert.Same(parentHasPayload ? parentPayload : null, parentObserved);
        Assert.Equal(parentHasPayload || localHasPayload, scope.TryGetPayload(out RootPayload? scopeObserved));
        Assert.Same(localHasPayload ? localPayload : parentHasPayload ? parentPayload : null, scopeObserved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "self-local-parent-precedence-and-local-repeated-update")]
    public void Resolution_PrefersSelfThenLocalThenParentAndKeepsRepeatedUpdatesLocal()
    {
        var parent = new TestContext(CancellationToken.None);
        var parentPayload = parent.GetOrAddPayload(() => new RootPayload("parent"));
        var firstLocal = new RootPayload("first-local");
        var lastLocal = new RootPayload("last-local");
        var scope = new TestScopeWithPayloads(parent, firstLocal, lastLocal, parent);
        var sibling = new TestScope(parent);
        var addCalls = 0;
        var updateCalls = 0;

        Assert.True(scope.HasPayloadType(typeof(PipeContext)));
        Assert.True(scope.TryGetPayload(out PipeContext? self));
        Assert.Same(scope, self);
        Assert.Same(scope, scope.GetOrAddPayload<PipeContext>(() => { addCalls++; return parent; }));
        Assert.Same(scope, scope.AddOrUpdatePayload<PipeContext>(
            () => { addCalls++; return parent; },
            value => { updateCalls++; return value; }));
        Assert.Equal(0, addCalls);
        Assert.Equal(0, updateCalls);
        Assert.True(scope.HasPayloadType(typeof(RootPayload)));
        Assert.True(scope.TryGetPayload(out RootPayload? local));
        Assert.Same(lastLocal, local);
        Assert.Same(lastLocal, scope.GetOrAddPayload(() => { addCalls++; return parentPayload; }));
        var updated = new RootPayload("updated");
        var twiceUpdated = new RootPayload("twice-updated");
        RootPayload? firstInput = null;
        RootPayload? secondInput = null;
        Assert.Same(updated, scope.AddOrUpdatePayload(
            () => { addCalls++; return firstLocal; },
            value => { updateCalls++; firstInput = value; return updated; }));
        Assert.Same(twiceUpdated, scope.AddOrUpdatePayload(
            () => { addCalls++; return firstLocal; },
            value => { updateCalls++; secondInput = value; return twiceUpdated; }));
        Assert.Same(lastLocal, firstInput);
        Assert.Same(updated, secondInput);
        Assert.Equal(0, addCalls);
        Assert.Equal(2, updateCalls);
        Assert.True(scope.TryGetPayload(out RootPayload? retained));
        Assert.Same(twiceUpdated, retained);
        Assert.True(sibling.TryGetPayload(out RootPayload? siblingObserved));
        Assert.Same(parentPayload, siblingObserved);
        Assert.True(parent.TryGetPayload(out RootPayload? parentObserved));
        Assert.Same(parentPayload, parentObserved);
        Assert.False(scope.HasPayloadType(typeof(MissingPayload)));
        Assert.False(scope.TryGetPayload(out MissingPayload? missing));
        Assert.Null(missing);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "initial-array-snapshot-and-required-parent-elements")]
    public void Constructor_SnapshotsInitialPayloadsAndRejectsMissingParentOrNullElements()
    {
        var parent = new TestContext(CancellationToken.None);
        var expected = new ScopePayload("initial");
        object[] payloads = [expected];
        var scope = new TestScopeWithPayloads(parent, payloads);
        payloads[0] = new ScopePayload("replacement");

        Assert.True(scope.TryGetPayload(out ScopePayload? observed));
        Assert.Same(expected, observed);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new TestScope(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new TestScopeWithPayloads(null!, [null!])).ParamName);
        Assert.Equal("payloads", Assert.Throws<ArgumentException>(() => new TestScopeWithPayloads(parent, [null!])).ParamName);
        Assert.False(parent.TryGetPayload(out ScopePayload? _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "failed-parent-or-local-update-preserves-exact-state")]
    public void FailedUpdate_PreservesParentAndLocalStateAndCanBeRetried(bool initiallyLocal)
    {
        var parent = new TestContext(CancellationToken.None);
        var parentPayload = parent.GetOrAddPayload(() => new RootPayload("parent"));
        var localPayload = new RootPayload("local");
        var scope = new TestScopeWithPayloads(parent, initiallyLocal ? [localPayload] : []);
        var expectedInput = initiallyLocal ? localPayload : parentPayload;
        var primary = new InvalidOperationException("update failure");
        var callbackCalls = 0;

        Assert.Same(primary, Assert.Throws<InvalidOperationException>(() => scope.AddOrUpdatePayload(
            () => { callbackCalls++; return localPayload; },
            value => { callbackCalls++; Assert.Same(expectedInput, value); throw primary; })));
        Assert.Equal(1, callbackCalls);
        Assert.True(scope.TryGetPayload(out RootPayload? afterFailure));
        Assert.Same(expectedInput, afterFailure);
        var nullFailure = Assert.Throws<InvalidOperationException>(() => scope.AddOrUpdatePayload(
            () => { callbackCalls++; return localPayload; },
            value => { callbackCalls++; Assert.Same(expectedInput, value); return null!; }));
        Assert.Equal(initiallyLocal ? "The payload update factory returned null." : "The payload factory returned null.", nullFailure.Message);
        Assert.Equal(2, callbackCalls);
        Assert.True(scope.TryGetPayload(out RootPayload? afterNull));
        Assert.Same(expectedInput, afterNull);
        var recovered = new RootPayload("recovered");
        Assert.Same(recovered, scope.AddOrUpdatePayload(
            () => { callbackCalls++; return localPayload; },
            value => { callbackCalls++; Assert.Same(expectedInput, value); return recovered; }));
        Assert.Equal(3, callbackCalls);
        Assert.True(scope.TryGetPayload(out RootPayload? retained));
        Assert.Same(recovered, retained);
        Assert.True(parent.TryGetPayload(out RootPayload? parentObserved));
        Assert.Same(parentPayload, parentObserved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "absent-payload-add-is-local-and-exactly-once")]
    public void AbsentPayload_AddsOnceLocallyWithoutUpdatingOrExposingItToParentOrSibling()
    {
        var parent = new TestContext(CancellationToken.None);
        var scope = new TestScope(parent);
        var sibling = new TestScope(parent);
        var expected = new ScopePayload("added");
        var addCalls = 0;
        var updateCalls = 0;

        ScopePayload result = scope.AddOrUpdatePayload(
            () => { addCalls++; return expected; },
            value => { updateCalls++; return value; });

        Assert.Same(expected, result);
        Assert.Equal(1, addCalls);
        Assert.Equal(0, updateCalls);
        Assert.True(scope.TryGetPayload(out ScopePayload? local));
        Assert.Same(expected, local);
        Assert.True(scope.HasPayloadType(typeof(ScopePayload)));
        Assert.False(parent.TryGetPayload(out ScopePayload? parentObserved));
        Assert.Null(parentObserved);
        Assert.False(sibling.TryGetPayload(out ScopePayload? siblingObserved));
        Assert.Null(siblingObserved);
        Assert.False(parent.HasPayloadType(typeof(ScopePayload)));
        Assert.False(sibling.HasPayloadType(typeof(ScopePayload)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SCOPED-PAYLOADS", "failed-absent-add-is-atomic-and-local-recovery")]
    public void FailedAbsentAdd_LeavesEveryContextEmptyAndRecoveryRemainsLocal(bool returnsNull)
    {
        var parent = new TestContext(CancellationToken.None);
        var scope = new TestScope(parent);
        var sibling = new TestScope(parent);
        var primary = new InvalidOperationException("add failure");
        var addCalls = 0;
        var updateCalls = 0;

        var failure = Assert.Throws<InvalidOperationException>(() => scope.AddOrUpdatePayload<ScopePayload>(
            () => { addCalls++; return returnsNull ? null! : throw primary; },
            value => { updateCalls++; return value; }));

        if (returnsNull)
            Assert.Equal("The payload factory returned null.", failure.Message);
        else
            Assert.Same(primary, failure);
        Assert.Equal(1, addCalls);
        Assert.Equal(0, updateCalls);
        Assert.False(scope.TryGetPayload(out ScopePayload? failedLocal));
        Assert.Null(failedLocal);
        Assert.False(parent.TryGetPayload(out ScopePayload? failedParent));
        Assert.Null(failedParent);
        Assert.False(sibling.TryGetPayload(out ScopePayload? failedSibling));
        Assert.Null(failedSibling);
        var expected = new ScopePayload("recovered");
        Assert.Same(expected, scope.AddOrUpdatePayload(
            () => { addCalls++; return expected; },
            value => { updateCalls++; return value; }));
        Assert.Equal(2, addCalls);
        Assert.Equal(0, updateCalls);
        Assert.True(scope.TryGetPayload(out ScopePayload? retained));
        Assert.Same(expected, retained);
        Assert.False(parent.TryGetPayload(out ScopePayload? parentAfterRecovery));
        Assert.Null(parentAfterRecovery);
        Assert.False(sibling.TryGetPayload(out ScopePayload? siblingAfterRecovery));
        Assert.Null(siblingAfterRecovery);
    }

    private static void InvokeMissingFactory<T>(PipeContext context, int missingFactory, PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        switch (missingFactory)
        {
            case 0: context.GetOrAddPayload<T>(null!); break;
            case 1: context.AddOrUpdatePayload(null!, updateFactory); break;
            case 2: context.AddOrUpdatePayload(addFactory, null!); break;
            default: throw new ArgumentOutOfRangeException(nameof(missingFactory));
        }
    }

    private sealed class TestContext(CancellationToken cancellationToken) : BasePipeContext(cancellationToken);

    private sealed class MutableCancellationContext : BasePipeContext
    {
        public CancellationToken CurrentToken { get; set; }
        public override CancellationToken CancellationToken => CurrentToken;
    }

    private sealed class TestScope(PipeContext parent) : ScopePipeContext(parent), PipeContext;

    private sealed class TestScopeWithPayloads(PipeContext parent, params object[]? payloads) : ScopePipeContext(parent, payloads), PipeContext;

    private sealed record RootPayload(string Value);

    private sealed record ScopePayload(string Value);

    private sealed record MissingPayload;
}
