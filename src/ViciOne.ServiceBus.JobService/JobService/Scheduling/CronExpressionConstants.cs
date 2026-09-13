namespace ViciOne.ServiceBus.JobService.Scheduling;

internal static class CronExpressionConstants
{
    /// <summary>Zero-based index of the seconds field.</summary>
    public const int Second = 0;

    /// <summary>Zero-based index of the minutes field.</summary>
    public const int Minute = 1;

    /// <summary>Zero-based index of the hours field.</summary>
    public const int Hour = 2;

    /// <summary>Zero-based index of the day-of-month field.</summary>
    public const int DayOfMonth = 3;

    /// <summary>Zero-based index of the month field.</summary>
    public const int Month = 4;

    /// <summary>Zero-based index of the day-of-week field.</summary>
    public const int DayOfWeek = 5;

    /// <summary>Zero-based index of the optional year field.</summary>
    public const int Year = 6;

    /// <summary>Internal marker representing the wildcard token <c>*</c>.</summary>
    public const int AllSpec = 99;

    /// <summary>Internal marker representing the no-specification token <c>?</c>.</summary>
    public const int NoSpec = 98;
}
