using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration.Retry;

public sealed class ConsumeContextRetryPipeSpecificationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "consume-specification-requires-builder-and-policy")]
    public void Apply_RejectsANullBuilderAndAMissingPolicy()
    {
        var specification = new ConsumeContextRetryPipeSpecification();

        ArgumentNullException missingBuilder = Assert.Throws<ArgumentNullException>(() => specification.Apply(null!));
        InvalidOperationException missingPolicy = Assert.Throws<InvalidOperationException>(() =>
            specification.Apply(new PipeConfigurator<ConsumeContext>.PipeBuilder()));

        Assert.Equal("builder", missingBuilder.ParamName);
        Assert.Equal("A retry policy must be configured before the specification is applied.", missingPolicy.Message);
        Assert.Single(specification.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "consume-specification-rejects-null-policy-and-observer")]
    public void Configuration_RejectsNullFactoriesFactoryResultsAndObservers()
    {
        var specification = new ConsumeContextRetryPipeSpecification();

        ArgumentNullException missingFactory = Assert.Throws<ArgumentNullException>(() =>
            specification.SetRetryPolicy(null!));
        ArgumentNullException missingObserver = Assert.Throws<ArgumentNullException>(() =>
            ((IRetryObserverConnector)specification).ConnectRetryObserver(null!));

        specification.SetRetryPolicy(_ => null!);
        InvalidOperationException missingPolicy = Assert.Throws<InvalidOperationException>(() =>
            specification.Apply(new PipeConfigurator<ConsumeContext>.PipeBuilder()));

        Assert.Equal("factory", missingFactory.ParamName);
        Assert.Equal("observer", missingObserver.ParamName);
        Assert.Equal("The retry policy factory returned null.", missingPolicy.Message);
    }
}
