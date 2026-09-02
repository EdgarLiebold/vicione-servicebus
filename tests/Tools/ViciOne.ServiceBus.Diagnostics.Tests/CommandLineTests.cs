using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Diagnostics.Tests;

public sealed class CommandLineTests
{
    private static readonly string[] Known =
        ["messages", "concurrency", "prefetch", "completion-limit-seconds", "output"];

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "option-value")]
    public void ReadsOptionAndValue()
    {
        Dictionary<string, string> options = Parse("publish-load", "--messages", "10");

        Assert.Equal("10", options["messages"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "scenario-without-options")]
    public void ReadsScenarioWithoutOptions()
    {
        Assert.Empty(Parse("publish-load"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "unknown-option")]
    public void UnknownOptionListsAcceptedOptions()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Parse("publish-load", "--unknown", "5"));

        Assert.Contains("--unknown", exception.Message, StringComparison.Ordinal);
        Assert.Contains("--messages", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "duplicate-option")]
    public void DuplicateOptionIsRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Parse("publish-load", "--messages", "1", "--messages", "2"));

        Assert.Contains("more than once", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "missing-option-value")]
    public void MissingOptionValueIsRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Parse("publish-load", "--messages"));

        Assert.Contains("without a value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "option-followed-by-option")]
    public void OptionFollowedByOptionIsRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Parse("publish-load", "--messages", "--prefetch", "10"));

        Assert.Contains("--messages", exception.Message, StringComparison.Ordinal);
        Assert.Contains("without a value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "positional-argument")]
    public void PositionalArgumentIsRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Parse("publish-load", "extra"));

        Assert.Contains("positional", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "non-positive-number")]
    public void NonPositiveNumberIsRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Program.Number(Parse("publish-load", "--messages", "0"), "messages", 1));

        Assert.Contains("positive number", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "non-numeric-number")]
    public void NonNumericNumberIsRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Program.Number(Parse("publish-load", "--messages", "many"), "messages", 1));

        Assert.Contains("'many'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "fallback")]
    public void MissingOptionUsesFallback()
    {
        Assert.Equal(7, Program.Number(Parse("publish-load"), "messages", 7));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-COMMAND-LINE", "canonical-runner-usage")]
    public void UsageShowsCanonicalRunner()
    {
        Assert.Contains("run_broker_category.py", Program.Usage, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> Parse(params string[] args)
    {
        return Program.ParseOptions(args, Known);
    }
}
