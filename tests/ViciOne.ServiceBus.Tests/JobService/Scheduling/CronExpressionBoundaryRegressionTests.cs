using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Scheduling;

public sealed class CronExpressionBoundaryRegressionTests
{
    [Theory]
    [InlineData("! 0 9 ? * MON 2026")]
    [InlineData("0,! 0 9 ? * MON 2026")]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "invalid-token-start-is-rejected-in-fields-and-lists")]
    public void InvalidTokenStart_IsRejectedByEveryValidationEntryPoint(string text)
    {
        FormatException constructorFailure = Assert.Throws<FormatException>(() => new CronExpression(text));
        FormatException validationFailure = Assert.Throws<FormatException>(() => CronExpression.ValidateExpression(text));

        Assert.Equal("Unexpected character: !", constructorFailure.Message);
        Assert.Equal("Unexpected character: !", validationFailure.Message);
        Assert.False(CronExpression.IsValidExpression(text));
    }

    [Theory]
    [InlineData("\u00a0")]
    [InlineData("\u2003")]
    [InlineData("\r\n")]
    [RequirementCoverage("REQ-VSB-CRON-PARSING", "unicode-whitespace-preserves-normalized-schedule")]
    public void UnicodeWhitespace_PreservesCanonicalTextAndNextOccurrence(string separator)
    {
        string text = $"{separator}0{separator}15{separator}9{separator}?{separator}*{separator}mon{separator}2026{separator}";
        var expression = new CronExpression(text) { TimeZone = TimeZoneInfo.Utc };

        Assert.Equal("0 15 9 ? * MON 2026", expression.ToString());
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 9, 15, 0, TimeSpan.Zero),
            expression.GetTimeAfter(new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero)));
        Assert.Equal(new DateTimeOffset(2026, 6, 8, 9, 15, 0, TimeSpan.Zero),
            expression.GetTimeAfter(new DateTimeOffset(2026, 6, 1, 9, 15, 0, TimeSpan.Zero)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "day-union-enumerates-month-transition-and-coincident-date-once")]
    public void CombinedCalendar_EnumeratesUnionWithoutSkippingOrDuplicatingDates()
    {
        var expression = new CronExpression("0 0 9 15 * MON 2026") { TimeZone = TimeZoneInfo.Utc };
        DateTimeOffset[] expected =
        [
            Utc(6, 1), Utc(6, 8), Utc(6, 15), Utc(6, 22), Utc(6, 29),
            Utc(7, 6), Utc(7, 13), Utc(7, 15), Utc(7, 20),
        ];
        DateTimeOffset cursor = Utc(5, 31);

        foreach (DateTimeOffset occurrence in expected)
        {
            DateTimeOffset? actual = expression.GetTimeAfter(cursor);
            Assert.Equal(occurrence, actual);
            Assert.True(actual > cursor);
            cursor = actual.Value;
        }

        Assert.Null(expression.GetTimeAfter(Utc(12, 31)));
    }

    private static DateTimeOffset Utc(int month, int day) =>
        new(2026, month, day, 9, 0, 0, TimeSpan.Zero);
}
