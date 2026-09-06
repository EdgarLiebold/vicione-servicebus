using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class BusInstanceTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-INSTANCE", "required-bus-control")]
    public void Constructor_RejectsAMissingBusControl()
    {
        Assert.Equal(
            "busControl",
            Assert.Throws<ArgumentNullException>(() => new TestBusInstance(null!)).ParamName);
    }

    private sealed class TestBusInstance(IBusControl busControl) : BusInstance<IBus>(busControl);
}
