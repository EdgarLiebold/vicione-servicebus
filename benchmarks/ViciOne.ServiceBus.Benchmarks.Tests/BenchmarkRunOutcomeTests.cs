namespace ViciOne.ServiceBus.Benchmarks.Tests;

using NUnit.Framework;
using ViciOne.ServiceBus.BenchmarkConsole;


public class BenchmarkRunOutcomeTests
{
    [TestCase(0, 0, 0, ExpectedResult = BenchmarkRunOutcome.NoBenchmarksExecuted)]
    [TestCase(1, 0, 0, ExpectedResult = BenchmarkRunOutcome.NoBenchmarksExecuted)]
    public int ExecutableRun_WithNoExecutedReports_Fails(int summaryCount, int reportCount, int successfulReportCount)
    {
        return BenchmarkRunOutcome.GetExitCode(false, summaryCount, reportCount, successfulReportCount, false);
    }

    [Test]
    public void ExecutableRun_WithAnyFailedReport_Fails()
    {
        var result = BenchmarkRunOutcome.GetExitCode(false, 1, 3, 2, false);

        Assert.That(result, Is.EqualTo(BenchmarkRunOutcome.BenchmarkFailed));
    }

    [Test]
    public void ExecutableRun_WithCriticalValidationError_Fails()
    {
        var result = BenchmarkRunOutcome.GetExitCode(false, 1, 1, 1, true);

        Assert.That(result, Is.EqualTo(BenchmarkRunOutcome.BenchmarkFailed));
    }

    [Test]
    public void ExecutableRun_WithOnlySuccessfulReports_Succeeds()
    {
        var result = BenchmarkRunOutcome.GetExitCode(false, 1, 3, 3, false);

        Assert.That(result, Is.Zero);
    }

    [TestCase("--help")]
    [TestCase("-h")]
    [TestCase("-?")]
    [TestCase("--list")]
    public void InformationalCommand_WithNoExecutedReports_Succeeds(string argument)
    {
        var informationalOnly = BenchmarkRunOutcome.IsInformationalOnly(new[] { argument });
        var result = BenchmarkRunOutcome.GetExitCode(informationalOnly, 0, 0, 0, false);

        Assert.That(result, Is.Zero);
    }

    [Test]
    public void Filter_IsAnExecutableCommand()
    {
        Assert.That(BenchmarkRunOutcome.IsInformationalOnly(new[] { "--filter", "*Missing*" }), Is.False);
    }
}
