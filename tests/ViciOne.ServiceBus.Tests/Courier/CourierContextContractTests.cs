using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Middleware.Timeout;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierContextContractTests
{
    private static readonly DateTimeOffset ActivityStartedAt = new(2042, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "context-clock-identity-state-and-decorators-are-consistent")]
    public void CourierContext_UsesItsInjectedClockAndDecoratorsPreserveExactActivityState()
    {
        var clock = new FakeTimeProvider(ActivityStartedAt);
        var builder = new RoutingSlipBuilder(Guid.Parse("95baf29e-6842-4052-8c00-ddc40a774aaa"), clock);
        builder.SetVariable("tenant", "north");
        RoutingSlip routingSlip = builder.Build();
        ConsumeContext<RoutingSlip> consumeContext = InMemoryOutboxTestContextFactory.Create(
            routingSlip,
            TestContext.Current.CancellationToken);
        consumeContext.SetTimeProvider(clock);
        var owner = new TestCourierContext(consumeContext);
        ActivityContext original = owner;
        clock.Advance(TimeSpan.FromSeconds(17));

        CourierContext[] contexts =
        [
            owner,
            new TestCourierContextProxy(owner),
            new TestCourierContextScope(owner),
            new TestOutboxCourierContextProxy(owner),
            new TestTimeoutCourierContextProxy(owner, CancellationToken.None, TimeSpan.FromMinutes(1)),
        ];

        foreach (CourierContext context in contexts)
        {
            ActivityContext activity = context;
            Assert.Equal(ActivityStartedAt, activity.Timestamp);
            Assert.Equal(TimeSpan.FromSeconds(17), activity.Elapsed);
            Assert.Equal(routingSlip.TrackingNumber, activity.TrackingNumber);
            Assert.Equal(original.ExecutionId, activity.ExecutionId);
            Assert.NotEqual(Guid.Empty, activity.ExecutionId);
            Assert.Equal("TestActivity", activity.ActivityName);
            Assert.Equal("north", activity.Variables["tenant"]);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "decorators-require-an-underlying-courier-context")]
    public void CourierContextDecorators_RejectAMissingUnderlyingContext()
    {
        Assert.Equal("courierContext", Assert.Throws<ArgumentNullException>(() => new TestCourierContextProxy(null!)).ParamName);
        Assert.Equal("courierContext", Assert.Throws<ArgumentNullException>(() => new TestCourierContextScope(null!)).ParamName);
        Assert.Equal("courierContext", Assert.Throws<ArgumentNullException>(() => new TestOutboxCourierContextProxy(null!)).ParamName);
        Assert.Equal("courierContext", Assert.Throws<ArgumentNullException>(() =>
            new TestTimeoutCourierContextProxy(null!, CancellationToken.None, TimeSpan.FromSeconds(1))).ParamName);
    }

    private sealed class TestCourierContext(ConsumeContext<RoutingSlip> context) : BaseCourierContext(context)
    {
        public override string ActivityName => "TestActivity";
    }

    private sealed class TestCourierContextProxy(CourierContext context) : CourierContextProxy(context);

    private sealed class TestCourierContextScope(CourierContext context) : CourierContextScope(context);

    private sealed class TestOutboxCourierContextProxy(CourierContext context) : InMemoryOutboxCourierContextProxy(context);

    private sealed class TestTimeoutCourierContextProxy(
        CourierContext context,
        CancellationToken cancellationToken,
        TimeSpan timeout) : TimeoutCourierContextProxy(context, cancellationToken, timeout);
}
