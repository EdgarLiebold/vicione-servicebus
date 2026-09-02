namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class RegistrationConfiguratorExtensionsTests
{
    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(WrongStateMachine))]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "invalid-runtime-future-type")]
    public void AddFuture_RejectsTypesThatDoNotUseFutureState(Type invalidFutureType)
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        ArgumentException exception = Assert.Throws<ArgumentException>(() => configurator.AddFuture(invalidFutureType));

        Assert.Equal("futureType", exception.ParamName);
        Assert.Contains("not a future", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "valid-runtime-future-type")]
    public void AddFuture_RegistersAStateMachineThatUsesFutureState()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        IFutureRegistrationConfigurator registration = configurator.AddFuture(typeof(ValidFutureStateMachine));

        Assert.NotNull(registration);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "null-runtime-inputs")]
    public void AddFuture_RejectsMissingInputsAtThePublicBoundary()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        Assert.Equal("configurator",
            Assert.Throws<ArgumentNullException>(() => RegistrationConfiguratorExtensions.AddFuture(null!, typeof(ValidFutureStateMachine))).ParamName);
        Assert.Equal("futureType", Assert.Throws<ArgumentNullException>(() => configurator.AddFuture(null!)).ParamName);
    }

    private sealed class ValidFutureStateMachine : ViciOneServiceBusStateMachine<FutureState>;

    private sealed class WrongStateMachine : ViciOneServiceBusStateMachine<WrongState>;

    private sealed class WrongState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }
}
