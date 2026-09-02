using ViciOne.ServiceBus.BenchmarkConsole;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Benchmark.Tests;

public sealed class BenchmarkRunOutcomeTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 0)]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RUN-OUTCOME", "no-executed-report-fails")]
    public void ExecutableRun_WithNoExecutedReports_Fails(int summaryCount, int reportCount, int successfulReportCount)
    {
        int result = BenchmarkRunOutcome.GetExitCode(false, summaryCount, reportCount, successfulReportCount, false);

        Assert.Equal(BenchmarkRunOutcome.NoBenchmarksExecuted, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RUN-OUTCOME", "failed-report-fails")]
    public void ExecutableRun_WithAnyFailedReport_Fails()
    {
        Assert.Equal(BenchmarkRunOutcome.BenchmarkFailed, BenchmarkRunOutcome.GetExitCode(false, 1, 3, 2, false));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RUN-OUTCOME", "critical-validation-fails")]
    public void ExecutableRun_WithCriticalValidationError_Fails()
    {
        Assert.Equal(BenchmarkRunOutcome.BenchmarkFailed, BenchmarkRunOutcome.GetExitCode(false, 1, 1, 1, true));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RUN-OUTCOME", "successful-reports-succeed")]
    public void ExecutableRun_WithOnlySuccessfulReports_Succeeds()
    {
        Assert.Equal(0, BenchmarkRunOutcome.GetExitCode(false, 1, 3, 3, false));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("--list")]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RUN-OUTCOME", "informational-command-succeeds")]
    public void InformationalCommand_WithNoExecutedReports_Succeeds(string argument)
    {
        bool informationalOnly = BenchmarkRunOutcome.IsInformationalOnly([argument]);

        Assert.True(informationalOnly);
        Assert.Equal(0, BenchmarkRunOutcome.GetExitCode(informationalOnly, 0, 0, 0, false));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RUN-OUTCOME", "filter-is-executable")]
    public void Filter_IsAnExecutableCommand()
    {
        Assert.False(BenchmarkRunOutcome.IsInformationalOnly(["--filter", "*Missing*"]));
    }
}
