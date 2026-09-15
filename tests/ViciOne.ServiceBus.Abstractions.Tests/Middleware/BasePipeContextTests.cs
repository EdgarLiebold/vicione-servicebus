using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Payloads;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware;

public sealed class BasePipeContextTests
{
    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, false, true)]
    [InlineData(2, true, false)]
    [InlineData(3, true, true)]
    [InlineData(4, false, true)]
    [InlineData(5, true, true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-constructor-token-and-payload-identity")]
    public void Constructors_PreserveCancellationAndInitialPayloads(int constructor, bool hasCancellation, bool hasInitialPayload)
    {
        using var cancellation = new CancellationTokenSource();
        var initial = new TestPayload("initial");
        var cache = new ListPayloadCache([initial]);
        TestContext context = constructor switch
        {
            0 => new TestContext(),
            1 => new TestContext([initial]),
            2 => new TestContext(cancellation.Token),
            3 => new TestContext(cancellation.Token, [initial]),
            4 => new TestContext(cache),
            5 => new TestContext(cache, cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(constructor))
        };
        var created = new TestPayload("created");
        var factoryCalls = 0;

        TestPayload observed = context.GetOrAddPayload(() =>
        {
            factoryCalls++;
            return created;
        });

        Assert.Equal(hasCancellation ? cancellation.Token : CancellationToken.None, context.CancellationToken);
        Assert.Same(hasInitialPayload ? initial : created, observed);
        Assert.Equal(hasInitialPayload ? 0 : 1, factoryCalls);
        Assert.True(context.HasPayloadType(typeof(TestPayload)));
        Assert.True(context.TryGetPayload(out TestPayload? retained));
        Assert.Same(observed, retained);
        Assert.False(context.HasPayloadType(typeof(MissingPayload)));
        Assert.False(context.TryGetPayload(out MissingPayload? missing));
        Assert.Null(missing);
        if (constructor is 4 or 5)
            Assert.Same(cache, context.Cache);
        cancellation.Cancel();
        Assert.Equal(hasCancellation, context.CancellationToken.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-required-cache-in-both-constructors")]
    public void CacheConstructors_RejectAMissingRequiredCache(bool withCancellation)
    {
        using var cancellation = new CancellationTokenSource();

        var failure = Assert.Throws<ArgumentNullException>(() =>
            withCancellation ? new TestContext(null!, cancellation.Token) : new TestContext((IPayloadCache)null!));

        Assert.Equal("payloadCache", failure.ParamName);
        Assert.False(cancellation.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-optional-null-and-empty-initial-payloads")]
    public void PayloadConstructors_AcceptOptionalNullAndEmptyArrays(bool withCancellation, bool nullArray)
    {
        using var cancellation = new CancellationTokenSource();
        object[]? payloads = nullArray ? null : [];
        var context = withCancellation ? new TestContext(cancellation.Token, payloads) : new TestContext(payloads);
        var expected = new TestPayload("created");

        Assert.Equal(withCancellation ? cancellation.Token : CancellationToken.None, context.CancellationToken);
        Assert.False(context.TryGetPayload(out TestPayload? missing));
        Assert.Null(missing);
        Assert.Same(expected, context.GetOrAddPayload(() => expected));
        Assert.True(context.TryGetPayload(out TestPayload? observed));
        Assert.Same(expected, observed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-initial-array-owned-and-non-null-elements")]
    public void PayloadConstructors_SnapshotTheArrayAndRejectNullElements(bool withCancellation)
    {
        using var cancellation = new CancellationTokenSource();
        var expected = new TestPayload("initial");
        var replacement = new TestPayload("replacement");
        object[] payloads = [expected];
        var context = withCancellation ? new TestContext(cancellation.Token, payloads) : new TestContext(payloads);
        payloads[0] = replacement;

        Assert.True(context.TryGetPayload(out TestPayload? observed));
        Assert.Same(expected, observed);
        var failure = Assert.Throws<ArgumentException>(() =>
            withCancellation ? new TestContext(cancellation.Token, [null!]) : new TestContext([null!]));
        Assert.Equal("payloads", failure.ParamName);
        Assert.Same(expected, context.GetOrAddPayload(() => replacement));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-required-runtime-payload-type")]
    public void HasPayloadType_RejectsANullTypeWithoutChangingTheCache()
    {
        var retained = new TestPayload("retained");
        var context = new TestContext([retained]);

        Assert.Equal("payloadType", Assert.Throws<ArgumentNullException>(() => context.HasPayloadType(null!)).ParamName);
        Assert.True(context.TryGetPayload(out TestPayload? observed));
        Assert.Same(retained, observed);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, false, true)]
    [InlineData(0, true, false)]
    [InlineData(0, true, true)]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(1, true, false)]
    [InlineData(1, true, true)]
    [InlineData(2, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-required-factories-before-self-or-cache-fast-path")]
    public void PayloadFactories_RejectNullBeforeAnyCallbackOrFastPath(int missingFactory, bool requestSelf, bool cached)
    {
        var retained = new TestPayload("retained");
        var context = new TestContext(cached ? [retained] : []);
        var callbackCalls = 0;
        ArgumentNullException failure;
        if (requestSelf)
        {
            failure = Assert.Throws<ArgumentNullException>(() => InvokeMissingFactory<PipeContext>(context, missingFactory,
                () => { callbackCalls++; return context; },
                value => { callbackCalls++; return value; }));
        }
        else
        {
            failure = Assert.Throws<ArgumentNullException>(() => InvokeMissingFactory(context, missingFactory,
                () => { callbackCalls++; return retained; },
                (TestPayload value) => { callbackCalls++; return value; }));
        }

        Assert.Equal(missingFactory switch { 0 => "payloadFactory", 1 => "addFactory", 2 => "updateFactory", _ => throw new ArgumentOutOfRangeException(nameof(missingFactory)) }, failure.ParamName);
        Assert.Equal(0, callbackCalls);
        Assert.Equal(cached, context.TryGetPayload(out TestPayload? observed));
        if (cached)
            Assert.Same(retained, observed);
        else
            Assert.Null(observed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-compatible-self-precedes-cached-context")]
    public void CompatibleSelf_TakesPrecedenceAndInvokesNeitherFactory()
    {
        var other = new TestContext();
        var cache = new ListPayloadCache([other]);
        var context = new TestContext(cache);
        var callbackCalls = 0;

        Assert.True(context.HasPayloadType(typeof(PipeContext)));
        Assert.True(context.TryGetPayload(out PipeContext? observed));
        Assert.Same(context, observed);
        Assert.Same(context, context.GetOrAddPayload<PipeContext>(() => { callbackCalls++; return other; }));
        Assert.Same(context, context.AddOrUpdatePayload<PipeContext>(
            () => { callbackCalls++; return other; },
            value => { callbackCalls++; return value; }));
        Assert.Equal(0, callbackCalls);
        Assert.True(cache.TryGetPayload(out TestContext? cached));
        Assert.Same(other, cached);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-most-recent-compatible-update-and-factory-identity")]
    public void AddAndUpdate_UseTheExactFactoryAndMostRecentCompatiblePayload()
    {
        var first = new TestPayload("first");
        var second = new DerivedPayload("second");
        var context = new TestContext([first, second]);
        var updated = new TestPayload("updated");
        var addCalls = 0;
        var updateCalls = 0;
        TestPayload? updateInput = null;

        Assert.Same(second, context.GetOrAddPayload(() => { addCalls++; return first; }));
        TestPayload result = context.AddOrUpdatePayload(
            () => { addCalls++; return first; },
            value => { updateCalls++; updateInput = value; return updated; });

        Assert.Same(updated, result);
        Assert.Same(second, updateInput);
        Assert.Equal(0, addCalls);
        Assert.Equal(1, updateCalls);
        Assert.True(context.TryGetPayload(out TestPayload? observed));
        Assert.Same(updated, observed);
        var added = new MissingPayload();
        Assert.Same(added, context.AddOrUpdatePayload(
            () => { addCalls++; return added; },
            value => { updateCalls++; return value; }));
        Assert.Equal(1, addCalls);
        Assert.Equal(1, updateCalls);
        Assert.True(context.TryGetPayload(out MissingPayload? observedAdded));
        Assert.Same(added, observedAdded);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-CACHE", "base-factory-failures-preserve-retained-state")]
    public void FactoryFailures_PreserveExactExceptionsAndRetainedPayloads()
    {
        var context = new TestContext();
        var primary = new InvalidOperationException("factory failure");

        Assert.Same(primary, Assert.Throws<InvalidOperationException>(() => context.GetOrAddPayload<TestPayload>(() => throw primary)));
        Assert.False(context.TryGetPayload(out TestPayload? missing));
        Assert.Null(missing);
        Assert.Equal("The payload factory returned null.", Assert.Throws<InvalidOperationException>(() => context.GetOrAddPayload<TestPayload>(() => null!)).Message);
        var retained = context.GetOrAddPayload(() => new TestPayload("retained"));
        Assert.Same(primary, Assert.Throws<InvalidOperationException>(() => context.AddOrUpdatePayload(() => new TestPayload("unused"), _ => throw primary)));
        Assert.Equal("The payload update factory returned null.", Assert.Throws<InvalidOperationException>(() => context.AddOrUpdatePayload(() => new TestPayload("unused"), _ => null!)).Message);
        Assert.True(context.TryGetPayload(out TestPayload? observed));
        Assert.Same(retained, observed);
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

    private sealed class TestContext : BasePipeContext
    {
        public TestContext() { }
        public TestContext(params object[]? payloads) : base(payloads) { }
        public TestContext(CancellationToken cancellationToken) : base(cancellationToken) { }
        public TestContext(CancellationToken cancellationToken, params object[]? payloads) : base(cancellationToken, payloads) { }
        public TestContext(IPayloadCache payloadCache) : base(payloadCache) { }
        public TestContext(IPayloadCache payloadCache, CancellationToken cancellationToken) : base(payloadCache, cancellationToken) { }

        public IPayloadCache Cache => PayloadCache;
    }

    private record TestPayload(string Value);
    private sealed record DerivedPayload(string DerivedValue) : TestPayload(DerivedValue);
    private sealed record MissingPayload;
}
