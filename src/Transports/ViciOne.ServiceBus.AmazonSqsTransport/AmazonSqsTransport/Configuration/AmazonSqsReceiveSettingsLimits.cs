namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using System;


static class AmazonSqsReceiveSettingsLimits
{
    public const int MaximumWaitTimeSeconds = 20;
    public const int MaximumVisibilityTimeoutSeconds = 43_200;
    public const int MinimumVisibilityRenewalSeconds = 60;
    public static readonly TimeSpan MaximumVisibilityDuration = TimeSpan.FromHours(12);

    public static int WaitTimeSeconds(int value)
    {
        if (value is < 0 or > MaximumWaitTimeSeconds)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"The SQS wait time must be between 0 and {MaximumWaitTimeSeconds} seconds.");

        return value;
    }

    public static int VisibilityTimeoutSeconds(int value, string parameterName)
    {
        if (value is < 0 or > MaximumVisibilityTimeoutSeconds)
            throw new ArgumentOutOfRangeException(parameterName, value, $"The SQS visibility timeout must be between 0 and {MaximumVisibilityTimeoutSeconds} seconds.");

        return value;
    }

    public static int PositiveConcurrency(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, "The concurrency limit must be at least 1.");

        return value;
    }

    public static TimeSpan MaximumVisibilityTimeout(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(value), value, "The maximum visibility duration must be positive.");

        return value > MaximumVisibilityDuration ? MaximumVisibilityDuration : value;
    }

    public static int VisibilityRenewalSeconds(int value)
    {
        VisibilityTimeoutSeconds(value, nameof(value));
        return Math.Max(MinimumVisibilityRenewalSeconds, value);
    }
}
