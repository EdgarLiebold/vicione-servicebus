using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Initializers;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class InitializerConventionRegistryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVENTIONS", "idempotent-bootstrap-and-freeze-on-first-read")]
    public void Registry_IsIdempotentDuringBootstrapAndImmutableAfterItsFirstSnapshot()
    {
        InitializerConventionRegistrySnapshot snapshot = InitializerConventionRegistryTestDriver.ExerciseLifecycle();

        Assert.Equal(1, snapshot.Count);
        Assert.True(snapshot.ReusedSnapshot);
        Assert.Equal(typeof(DefaultInitializerConvention), snapshot.ConventionType);
        InvalidOperationException exception = Assert.IsType<InvalidOperationException>(snapshot.LateMutationException);
        Assert.Equal("Message initializer conventions are immutable after the first initializer is created.", exception.Message);
    }
}
