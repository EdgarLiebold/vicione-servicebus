using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Configuration;

public sealed class ValidationResultGuardTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-VALIDATION", "successful-results-are-snapshotted")]
    public void SuccessfulResults_AreReturnedAsAStableSnapshot()
    {
        var source = new List<ValidationResult>
        {
            new StubValidationResult(ValidationResultDisposition.Success, "endpoint", "Valid endpoint.")
        };

        IReadOnlyList<ValidationResult> snapshot = source.ThrowIfContainsFailure();
        source.Clear();

        Assert.Single(snapshot);
        Assert.False(snapshot.ContainsFailure());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-VALIDATION", "failed-results-are-aggregated")]
    public void FailedResults_ArePreservedOnTheConfigurationException()
    {
        var failure = new StubValidationResult(
            ValidationResultDisposition.Failure,
            "endpoint.address",
            "The address is required.");

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            new ValidationResult[] { failure }.ThrowIfContainsFailure("Endpoint configuration failed:"));

        Assert.Same(failure, Assert.Single(exception.Results));
        Assert.Contains("Endpoint configuration failed:", exception.Message, StringComparison.Ordinal);
        Assert.True(exception.Results.ContainsFailure());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-VALIDATION", "null-result-sequences-are-rejected")]
    public void NullResultSequences_AreRejectedAtThePublicBoundary()
    {
        IEnumerable<ValidationResult>? results = null;

        Assert.Equal(
            "results",
            Assert.Throws<ArgumentNullException>(() => results!.ContainsFailure()).ParamName);
        Assert.Equal(
            "results",
            Assert.Throws<ArgumentNullException>(() => results!.ThrowIfContainsFailure()).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-VALIDATION", "result-implementation-is-encapsulated")]
    public void ResultFactories_DoNotExposeTheirImplementationType()
    {
        Assert.Empty(typeof(ValidationResultExtensions).GetNestedTypes(BindingFlags.Public));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-VALIDATION", "factory-inputs-and-parent-paths")]
    public void ResultFactories_ValidateRequiredTextAndComposeParentPaths()
    {
        var specification = new StubSpecification();

        Assert.Equal(
            "message",
            Assert.Throws<ArgumentException>(() => specification.Failure(" ")).ParamName);
        Assert.Equal(
            "key",
            Assert.Throws<ArgumentException>(() => specification.Warning(" ", "warning")).ParamName);

        ValidationResult rootResult = specification.Success("Configuration is valid.");
        ValidationResult memberResult = specification.Failure("Address", "is required");

        Assert.Equal("Endpoint", rootResult.WithParentKey("Endpoint").Key);
        Assert.Equal("Endpoint.Address", memberResult.WithParentKey("Endpoint").Key);
        Assert.Equal(
            "parentKey",
            Assert.Throws<ArgumentException>(() => memberResult.WithParentKey(" ")).ParamName);
        Assert.Equal(
            "result",
            Assert.Throws<ArgumentNullException>(() => ValidationResultExtensions.WithParentKey(null!, "Endpoint")).ParamName);
    }

    private sealed class StubValidationResult(
        ValidationResultDisposition disposition,
        string key,
        string message) : ValidationResult
    {
        public ValidationResultDisposition Disposition { get; } = disposition;

        public string Message { get; } = message;

        public string Key { get; } = key;

        public string? Value => null;

        public override string ToString() => $"{Key}: {Message}";
    }

    private sealed class StubSpecification : ISpecification
    {
        public IEnumerable<ValidationResult> Validate() => [];
    }
}
