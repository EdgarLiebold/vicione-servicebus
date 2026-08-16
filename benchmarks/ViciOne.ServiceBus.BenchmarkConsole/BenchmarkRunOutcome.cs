namespace ViciOne.ServiceBus.BenchmarkConsole;

using System;


public static class BenchmarkRunOutcome
{
    public const int NoBenchmarksExecuted = 2;
    public const int BenchmarkFailed = 3;

    public static bool IsInformationalOnly(string[] args)
    {
        return Array.Exists(args, argument =>
            argument.Equals("--help", StringComparison.OrdinalIgnoreCase)
            || argument.Equals("-h", StringComparison.OrdinalIgnoreCase)
            || argument.Equals("-?", StringComparison.OrdinalIgnoreCase)
            || argument.Equals("--list", StringComparison.OrdinalIgnoreCase));
    }

    public static int GetExitCode(
        bool informationalOnly,
        int summaryCount,
        int reportCount,
        int successfulReportCount,
        bool hasCriticalValidationErrors)
    {
        if (informationalOnly)
            return 0;

        if (summaryCount <= 0 || reportCount <= 0)
            return NoBenchmarksExecuted;

        if (hasCriticalValidationErrors || successfulReportCount != reportCount)
            return BenchmarkFailed;

        return 0;
    }
}
