using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureConfigurationContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "response-requires-terminal-or-pending-behavior")]
    public void ResponseWithoutResultOrPendingTracking_IsRejectedDuringConstruction()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new UnconfiguredResponseFuture());

        Assert.Contains("SetResultFactory", exception.Message, StringComparison.Ordinal);
        Assert.Contains("CompletePendingRequest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "protected-callback-boundaries")]
    public void ProtectedConfigurationMethods_RejectNullBeforeChangingTheStateMachine()
    {
        var future = new ConfigurationProbeFuture();

        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(future.ConfigureCommandWithNull).ParamName);
        Assert.Equal("inputSelector", Assert.Throws<ArgumentNullException>(future.SendRequestWithNullSelector).ParamName);
        Assert.Equal("inputSelector", Assert.Throws<ArgumentNullException>(future.SendRequestsWithNullSelector).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(future.SendRequestsWithNullConfiguration).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(future.ExecuteRoutingSlipWithNullConfiguration).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(future.ConfigureCompletedResultWithNull).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(future.ConfigureImmediateFaultWithNull).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(future.ConfigureDeferredFaultWithNull).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-API", "greenfield-result-request-fault-method-names")]
    public void ConfigurationInterfaces_ExposeOnlyTheGreenfieldFactoryAndInitializerNames()
    {
        string[] methodNames =
        [
            .. typeof(IFutureRequestConfigurator<,,>).GetMethods().Select(method => method.Name),
            .. typeof(IFutureResultConfigurator<,>).GetMethods().Select(method => method.Name),
            .. typeof(IFutureFaultConfigurator<,>).GetMethods().Select(method => method.Name),
        ];

        Assert.Contains("SetRequestFactory", methodNames);
        Assert.Contains("SetRequestInitializer", methodNames);
        Assert.Contains("SetResultFactory", methodNames);
        Assert.Contains("SetResultInitializer", methodNames);
        Assert.Contains("SetFaultFactory", methodNames);
        Assert.Contains("SetFaultInitializer", methodNames);
        Assert.DoesNotContain("UsingRequestFactory", methodNames);
        Assert.DoesNotContain("UsingRequestInitializer", methodNames);
        Assert.DoesNotContain("SetCompletedUsingFactory", methodNames);
        Assert.DoesNotContain("SetCompletedUsingInitializer", methodNames);
        Assert.DoesNotContain("SetFaultedUsingFactory", methodNames);
        Assert.DoesNotContain("SetFaultedUsingInitializer", methodNames);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-FUTURE-DEFINITION", "positive-concurrency-limit")]
    public void Definition_RejectsNonPositiveConcurrencyLimits(int value)
    {
        var definition = new DefinitionProbe();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => definition.SetConcurrencyLimit(value));

        Assert.Equal("value", exception.ParamName);
        Assert.Null(definition.ConcurrentMessageLimit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-FUTURE-DEFINITION", "nonempty-endpoint-name")]
    public void Definition_RejectsMissingEndpointNames(string value)
    {
        var definition = new DefinitionProbe();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => definition.SetEndpointName(value));

        Assert.Equal("value", exception.ParamName);
    }

    private sealed class ConfigurationProbeFuture : Future<ProbeCommand, ProbeResult>
    {
        public void ConfigureCommandWithNull() => ConfigureCommand(null!);

        public void SendRequestWithNullSelector() => SendRequest<ProbeInput, ProbeRequest>(null!);

        public void SendRequestsWithNullSelector() => SendRequests<ProbeInput, ProbeRequest>(null!, _ => { });

        public void SendRequestsWithNullConfiguration() => SendRequests<ProbeInput, ProbeRequest>(_ => [], null!);

        public void ExecuteRoutingSlipWithNullConfiguration() => ExecuteRoutingSlip(null!);

        public void ConfigureCompletedResultWithNull() => WhenAllCompleted(null!);

        public void ConfigureImmediateFaultWithNull() => WhenAnyFaulted(null!);

        public void ConfigureDeferredFaultWithNull() => WhenAllCompletedOrFaulted(null!);
    }

    private sealed class UnconfiguredResponseFuture : Future<ProbeCommand, ProbeResult>
    {
        public UnconfiguredResponseFuture()
        {
            SendRequest<ProbeRequest>()
                .OnResponseReceived<ProbeResponse>(_ => { });
        }
    }

    private sealed class DefinitionProbe : FutureDefinition<ConfigurationProbeFuture>
    {
        public void SetConcurrencyLimit(int value) => ConcurrentMessageLimit = value;

        public void SetEndpointName(string value) => EndpointName = value;
    }

    private sealed class ProbeCommand;

    private sealed class ProbeInput;

    private sealed class ProbeRequest;

    private sealed class ProbeResponse;

    private sealed class ProbeResult;
}
