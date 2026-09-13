using System.Globalization;
using System.Text;

namespace ViciOne.ServiceBus.JobService.Scheduling;

readonly struct CronExpressionSummary(
    CronField seconds,
    CronField minutes,
    CronField hours,
    CronField daysOfMonth,
    CronField months,
    CronField daysOfWeek,
    bool lastDayOfWeek,
    bool nearestWeekday,
    int nthDayOfWeek,
    bool lastDayOfMonth,
    CronField years)
{
    public CronField Seconds { get; } = seconds;
    public CronField Minutes { get; } = minutes;
    public CronField Hours { get; } = hours;
    public CronField DaysOfMonth { get; } = daysOfMonth;
    public CronField Months { get; } = months;
    public CronField DaysOfWeek { get; } = daysOfWeek;
    public bool LastDayOfWeek { get; } = lastDayOfWeek;
    public bool NearestWeekday { get; } = nearestWeekday;
    public int NthDayOfWeek { get; } = nthDayOfWeek;
    public bool LastDayOfMonth { get; } = lastDayOfMonth;
    public CronField Years { get; } = years;

    /// <summary>Formats one parsed cron field using its numeric values or special marker.</summary>
    /// <param name="data">The parsed field.</param>
    /// <returns>The diagnostic representation of the field.</returns>
    static string GetExpressionSetSummary(CronField data)
    {
        if (data.Contains(CronExpressionConstants.NoSpec))
            return "?";

        if (data.Contains(CronExpressionConstants.AllSpec))
            return "*";

        var b = new StringBuilder();

        var first = true;
        foreach (var iVal in data)
        {
            var val = iVal.ToString(CultureInfo.InvariantCulture);
            if (!first)
                b.Append(',');

            b.Append(val);
            first = false;
        }

        return b.ToString();
    }

    public override string ToString()
    {
        var b = new StringBuilder();

        b.Append("seconds: ");
        b.AppendLine(GetExpressionSetSummary(Seconds));
        b.Append("minutes: ");
        b.AppendLine(GetExpressionSetSummary(Minutes));
        b.Append("hours: ");
        b.AppendLine(GetExpressionSetSummary(Hours));
        b.Append("daysOfMonth: ");
        b.AppendLine(GetExpressionSetSummary(DaysOfMonth));
        b.Append("months: ");
        b.AppendLine(GetExpressionSetSummary(Months));
        b.Append("daysOfWeek: ");
        b.AppendLine(GetExpressionSetSummary(DaysOfWeek));
        b.Append("lastDayOfWeek: ");
        b.AppendLine(LastDayOfWeek.ToString());
        b.Append("nearestWeekday: ");
        b.AppendLine(NearestWeekday.ToString());
        b.Append("nthDayOfWeek: ");
        b.AppendLine(NthDayOfWeek.ToString());
        b.Append("lastDayOfMonth: ");
        b.AppendLine(LastDayOfMonth.ToString());
        b.Append("years: ");
        b.AppendLine(GetExpressionSetSummary(Years));
        return b.ToString();
    }
}
