using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Contexts;

public sealed class BindContextProxyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BIND-CONTEXT", "bound-value-precedes-left-payloads")]
    public void BoundValue_PrecedesLeftPayloadsAndFactories()
    {
        using var cancellation = new CancellationTokenSource();
        var left = new TestPipeContext(cancellation.Token);
        var leftValue = new Service("left");
        left.GetOrAddPayload<IService>(() => leftValue);
        var right = new Service("right");
        var context = new BindContextProxy<TestPipeContext, Service>(left, right);
        var factoryInvoked = false;

        Assert.Same(left, context.Left);
        Assert.Same(right, context.Right);
        Assert.Equal(cancellation.Token, context.CancellationToken);
        Assert.True(context.HasPayloadType(typeof(IService)));
        Assert.True(context.TryGetPayload(out IService? selected));
        Assert.Same(right, selected);
        Assert.Same(right, context.GetOrAddPayload<IService>(() =>
        {
            factoryInvoked = true;
            return leftValue;
        }));
        Assert.Same(right, context.AddOrUpdatePayload<IService>(
            () =>
            {
                factoryInvoked = true;
                return leftValue;
            },
            current =>
            {
                factoryInvoked = true;
                return current;
            }));
        Assert.False(factoryInvoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BIND-CONTEXT", "unmatched-payload-operations-delegate-left")]
    public void UnmatchedPayloadOperations_DelegateToTheLeftContext()
    {
        var left = new TestPipeContext(CancellationToken.None);
        var context = new BindContextProxy<TestPipeContext, Service>(left, new Service("right"));

        Payload first = context.GetOrAddPayload(() => new Payload(1));
        Payload updated = context.AddOrUpdatePayload(
            () => new Payload(2),
            current => new Payload(current.Value + 1));

        Assert.Equal(1, first.Value);
        Assert.Equal(2, updated.Value);
        Assert.True(left.TryGetPayload(out Payload? stored));
        Assert.Same(updated, stored);
        Assert.False(context.HasPayloadType(typeof(UnrelatedPayload)));
        Assert.False(context.TryGetPayload(out UnrelatedPayload? missing));
        Assert.Null(missing);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BIND-CONTEXT", "constructor-and-factory-input-validation")]
    public void ConstructionAndPayloadFactories_RejectMissingInputs()
    {
        var left = new TestPipeContext(CancellationToken.None);
        var right = new Service("right");
        var context = new BindContextProxy<TestPipeContext, Service>(left, right);

        Assert.Equal("left", Assert.Throws<ArgumentNullException>(() => new BindContextProxy<TestPipeContext, Service>(null!, right)).ParamName);
        Assert.Equal("right", Assert.Throws<ArgumentNullException>(() => new BindContextProxy<TestPipeContext, Service>(left, null!)).ParamName);
        Assert.Equal("payloadType", Assert.Throws<ArgumentNullException>(() => context.HasPayloadType(null!)).ParamName);
        Assert.Equal("payloadFactory", Assert.Throws<ArgumentNullException>(() => context.GetOrAddPayload<Payload>(null!)).ParamName);
        Assert.Equal(
            "addFactory",
            Assert.Throws<ArgumentNullException>(() => context.AddOrUpdatePayload<Payload>(null!, current => current)).ParamName);
        Assert.Equal(
            "updateFactory",
            Assert.Throws<ArgumentNullException>(() => context.AddOrUpdatePayload(() => new Payload(1), null!)).ParamName);
    }

    private interface IService
    {
    }

    private sealed record Service(string Name) : IService;

    private sealed record Payload(int Value);

    private sealed record UnrelatedPayload;

    private sealed class TestPipeContext(CancellationToken cancellationToken) : BasePipeContext(cancellationToken)
    {
    }
}
