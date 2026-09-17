using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineConfigurationMetadataDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "uncorrelated-event-constructor-null")]
    public void UncorrelatedEventCorrelation_RejectsNullEventAtConstruction()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataState>.UncorrelatedEventCorrelation<MetadataMessage>(null!));

        Assert.Equal("event", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "uncorrelated-event-empty-runtime-contract")]
    public void UncorrelatedEventCorrelation_ExposesOnlyTheOwnedEventAndExactValidationFailure()
    {
        var @event = new MessageEvent<MetadataMessage>("Uncorrelated");
        var correlation =
            new ViciOneServiceBusStateMachine<MetadataState>.UncorrelatedEventCorrelation<MetadataMessage>(@event);

        Assert.Same(@event, correlation.Event);
        Assert.Equal(typeof(MetadataMessage), ((IEventCorrelation)correlation).DataType);
        Assert.False(correlation.ConfigureConsumeTopology);
        Assert.Null(correlation.FilterFactory);
        Assert.Null(correlation.MessageFilter);
        Assert.Null(correlation.Policy);

        ValidationResult failure = Assert.Single(correlation.Validate());
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Equal("Uncorrelated", failure.Key);
        Assert.Equal("Correlation", failure.Value);
        Assert.Equal("was not specified", failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "option-bit-values-and-composition")]
    public void CompositeEventOptions_ExposeStableIndependentAndComposableFlagValues()
    {
        Assert.Equal(0, (int)CompositeEventOptions.None);
        Assert.Equal(1, (int)CompositeEventOptions.IncludeInitial);
        Assert.Equal(2, (int)CompositeEventOptions.IncludeFinal);
        Assert.Equal(4, (int)CompositeEventOptions.RaiseOnce);

        Assert.Equal(
            CompositeEventOptions.None,
            CompositeEventOptions.IncludeInitial & CompositeEventOptions.IncludeFinal);
        Assert.Equal(
            CompositeEventOptions.None,
            CompositeEventOptions.IncludeInitial & CompositeEventOptions.RaiseOnce);
        Assert.Equal(
            CompositeEventOptions.None,
            CompositeEventOptions.IncludeFinal & CompositeEventOptions.RaiseOnce);

        CompositeEventOptions allOptions = CompositeEventOptions.IncludeInitial
            | CompositeEventOptions.IncludeFinal
            | CompositeEventOptions.RaiseOnce;

        Assert.Equal((CompositeEventOptions)7, allOptions);
    }

    private sealed class MetadataState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class MetadataMessage;
}
