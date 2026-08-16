using System.Linq;
using BenchmarkDotNet.Running;


namespace ViciOne.ServiceBus.BenchmarkConsole;


internal static class Program
{
    public static int Main(string[] args)
    {
        var summaries = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, BenchmarkConfiguration.Create())
            .ToArray();

        var reports = summaries.SelectMany(summary => summary.Reports).ToArray();

        return BenchmarkRunOutcome.GetExitCode(
            BenchmarkRunOutcome.IsInformationalOnly(args),
            summaries.Length,
            reports.Length,
            reports.Count(report => report.Success),
            summaries.Any(summary => summary.HasCriticalValidationErrors));
    }
}
