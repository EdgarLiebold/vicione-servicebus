using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class RegistrationConfiguratorExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT-CONFIGURATION", "explicit-default-invariant")]
    public void DefaultRequestTimeout_AcceptsOnlyAnExplicitPositiveDuration()
    {
        var configurator = new InspectableServiceCollectionBusConfigurator(new ServiceCollection());
        var expected = new RequestTimeout(TimeSpan.FromSeconds(41));

        configurator.SetDefaultRequestTimeout(expected);
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configurator.SetDefaultRequestTimeout(RequestTimeout.None));

        Assert.Equal(expected, configurator.ConfiguredDefaultRequestTimeout);
        Assert.Equal("timeout", exception.ParamName);
        Assert.Equal("The default request timeout must be explicit. (Parameter 'timeout')", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT-CONFIGURATION", "no-unit-ambiguous-components")]
    public void AdvancedRegistration_DoesNotExposeUnitAmbiguousTimeoutComponents()
    {
        Assert.DoesNotContain(
            typeof(Advanced.Registration.IAdvancedRegistrationConfigurator).GetMethods(),
            method => method.Name == "SetDefaultRequestTimeout");
    }

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
            Assert.Throws<ArgumentNullException>(() => FutureRegistrationConfiguratorRuntimeExtensions.AddFuture(null!,
                typeof(ValidFutureStateMachine))).ParamName);
        Assert.Equal("futureType", Assert.Throws<ArgumentNullException>(() => configurator.AddFuture(null!)).ParamName);
    }

    private sealed class ValidFutureStateMachine : ViciOneServiceBusStateMachine<FutureState>;

    private sealed class WrongStateMachine : ViciOneServiceBusStateMachine<WrongState>;

    private sealed class WrongState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class InspectableServiceCollectionBusConfigurator(IServiceCollection services)
        : ServiceCollectionBusConfigurator(services)
    {
        public RequestTimeout ConfiguredDefaultRequestTimeout => DefaultRequestTimeout;
    }
}
