using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware.Configuration.Filters;

public sealed class SplitFilterPipeSpecificationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SPLIT-PIPE-SPECIFICATION", "constructor-rejects-every-required-input")]
    public void Constructor_RejectsEveryMissingRequiredInput()
    {
        var inner = new RecordingSpecification();

        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            new PipeConfigurator<TestPipeContext>.SplitFilterPipeSpecification<TestPipeContext>(
                null!, (input, _) => input, context => context)).ParamName);
        Assert.Equal("contextProvider", Assert.Throws<ArgumentNullException>(() =>
            new PipeConfigurator<TestPipeContext>.SplitFilterPipeSpecification<TestPipeContext>(
                inner, null!, context => context)).ParamName);
        Assert.Equal("inputContextProvider", Assert.Throws<ArgumentNullException>(() =>
            new PipeConfigurator<TestPipeContext>.SplitFilterPipeSpecification<TestPipeContext>(
                inner, (input, _) => input, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SPLIT-PIPE-SPECIFICATION", "inner-validation-sequence-is-preserved")]
    public void Validate_ReturnsTheExactInnerValidationSequence()
    {
        var inner = new RecordingSpecification();
        ValidationResult[] results = [inner.Failure("inner", "invalid inner configuration")];
        inner.Results = results;
        var specification = Create(inner);

        IEnumerable<ValidationResult> actual = specification.Validate();

        Assert.Same(results, actual);
        Assert.Equal("inner", Assert.Single(actual).Key);
        Assert.False(inner.Applied);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SPLIT-PIPE-SPECIFICATION", "null-inner-validation-sequence-is-rejected")]
    public void Validate_RejectsANullInnerValidationSequence()
    {
        var specification = Create(new RecordingSpecification { Results = null });

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => specification.Validate());

        Assert.Equal("The inner pipe specification returned null validation results.", actual.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SPLIT-PIPE-SPECIFICATION", "apply-rejects-null-builder-and-filter")]
    public void Apply_RejectsANullOuterBuilderAndNullInnerFilter()
    {
        var inner = new RecordingSpecification();
        var specification = Create(inner);

        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => specification.Apply(null!)).ParamName);
        Assert.False(inner.Applied);

        inner.AddNullFilter = true;
        var builder = new RecordingBuilder();

        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() => specification.Apply(builder)).ParamName);
        Assert.Equal(0, builder.AddedFilterCount);
    }

    private static PipeConfigurator<TestPipeContext>.SplitFilterPipeSpecification<TestPipeContext> Create(
        IPipeSpecification<TestPipeContext> inner) =>
        new(inner, (input, _) => input, context => context);

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RecordingSpecification : IPipeSpecification<TestPipeContext>
    {
        public IEnumerable<ValidationResult>? Results { get; set; } = [];

        public bool Applied { get; private set; }

        public bool AddNullFilter { get; set; }

        public void Apply(IPipeBuilder<TestPipeContext> builder)
        {
            Applied = true;
            if (AddNullFilter)
                builder.AddFilter(null!);
        }

        public IEnumerable<ValidationResult> Validate() => Results!;
    }

    private sealed class RecordingBuilder : IPipeBuilder<TestPipeContext>
    {
        public int AddedFilterCount { get; private set; }

        public void AddFilter(IFilter<TestPipeContext> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            AddedFilterCount++;
        }
    }
}
