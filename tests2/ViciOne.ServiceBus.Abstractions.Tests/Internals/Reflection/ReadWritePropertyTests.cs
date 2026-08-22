namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Reflection;

using global::ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ReadWritePropertyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-READ-WRITE-PROPERTY", "private-setter")]
    public void PrivateSetter_CanBeWrittenAndRead()
    {
        var property = typeof(PrivateSetterTarget).GetProperty(nameof(PrivateSetterTarget.Name))
            ?? throw new InvalidOperationException("The test target must expose its Name property.");
        var accessor = new ReadWriteProperty<PrivateSetterTarget>(property);
        var target = new PrivateSetterTarget();

        accessor.Set(target, "Chris");

        Assert.Equal("Chris", target.Name);
        Assert.Equal("Chris", accessor.Get(target));
    }

    private sealed class PrivateSetterTarget
    {
        public string Name { get; private set; } = string.Empty;
    }
}
