using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration.Retry;

public sealed class RetryPipeSpecificationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "specification-requires-builder-and-policy")]
    public void Apply_RejectsANullBuilderAndAMissingPolicy()
    {
        var specification = new RetryPipeSpecification<TestPipeContext>();

        ArgumentNullException builderException = Assert.Throws<ArgumentNullException>(() => specification.Apply(null!));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            specification.Apply(new PipeConfigurator<TestPipeContext>.PipeBuilder()));

        Assert.Equal("builder", builderException.ParamName);
        Assert.Equal("A retry policy must be configured before the specification is applied.", exception.Message);
        Assert.Single(specification.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "specification-rejects-null-policy-results-and-observers")]
    public void Configuration_RejectsANullFactoryResultAndObserver()
    {
        var specification = new RetryPipeSpecification<TestPipeContext>();

        ArgumentNullException factoryException = Assert.Throws<ArgumentNullException>(() =>
            specification.SetRetryPolicy(null!));
        ArgumentNullException observerException = Assert.Throws<ArgumentNullException>(() =>
            specification.ConnectRetryObserver(null!));

        specification.SetRetryPolicy(_ => null!);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            specification.Apply(new PipeConfigurator<TestPipeContext>.PipeBuilder()));

        Assert.Equal("factory", factoryException.ParamName);
        Assert.Equal("observer", observerException.ParamName);
        Assert.Equal("The retry policy factory returned null.", exception.Message);
    }

    private sealed class TestPipeContext : BasePipeContext
    {
    }
}
