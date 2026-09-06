using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Scheduling;

public sealed class CronExpressionContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-CONSTRUCTION", "null-expression-rejected")]
    public void NullExpression_IsRejected()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CronExpression(null));

        Assert.Equal("cronExpression", exception.ParamName);
    }

    [Theory]
    [InlineData("0 15 10 1,3,6,15,L * ? 2010")]
    [InlineData("0 15 10 15,31 * ? 2010")]
    [InlineData("0 15 10 15,L * ? 2010")]
    [InlineData("0 15 10 6,15 * ? 2010")]
    [InlineData("0 15 10 6,15,LW * ? 2010")]
    [InlineData("0 15 10 15,L-2 * ? 2010")]
    [InlineData("0 15 10 31,L-2 * ? 2010")]
    [InlineData("0 15 10 ? * 6#3")]
    [RequirementCoverage("REQ-VSB-CRON-IDENTITY", "text-equality-and-hash")]
    public void EquivalentExpressions_HaveStableTextEqualityAndHashCode(string text)
    {
        var first = new CronExpression(text) { TimeZone = TimeZoneInfo.Utc };
        var second = new CronExpression(text) { TimeZone = TimeZoneInfo.Utc };

        Assert.Equal(text, first.ToString());
        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals(null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-IDENTITY", "default-and-explicit-local-zone-are-equivalent")]
    public void DefaultAndExplicitLocalTimeZones_HaveTheSameIdentity()
    {
        var defaultTimeZone = new CronExpression("0 15 10 ? * MON-FRI");
        var explicitLocalTimeZone = new CronExpression("0 15 10 ? * MON-FRI") { TimeZone = TimeZoneInfo.Local };

        Assert.Equal(defaultTimeZone, explicitLocalTimeZone);
        Assert.Equal(defaultTimeZone.GetHashCode(), explicitLocalTimeZone.GetHashCode());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-IDENTITY", "whitespace-is-canonicalized")]
    public void EquivalentWhitespace_IsCanonicalizedForIdentity()
    {
        var compact = new CronExpression("0 15 10 ? * MON-FRI") { TimeZone = TimeZoneInfo.Utc };
        var padded = new CronExpression("  0\t15   10 ?  *\tMON-FRI  ") { TimeZone = TimeZoneInfo.Utc };

        Assert.Equal("0 15 10 ? * MON-FRI", padded.ToString());
        Assert.Equal(compact, padded);
        Assert.Equal(compact.GetHashCode(), padded.GetHashCode());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-CONSTRUCTION", "null-time-zone-rejected")]
    public void NullTimeZone_IsRejected()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new CronExpression("0 15 10 ? * MON-FRI") { TimeZone = null! });

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "null-validation-input")]
    public void NullExpression_IsReportedAsInvalidByTheNonThrowingApi()
    {
        Assert.False(CronExpression.IsValidExpression(null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SUMMARY", "all-fields")]
    public void ExpressionSummary_ReportsEveryParsedField()
    {
        var expression = new CronExpression("0 15 15 5 11 ?");

        Assert.Equal(
            """
            seconds: 0
            minutes: 15
            hours: 15
            daysOfMonth: 5
            months: 11
            daysOfWeek: ?
            lastDayOfWeek: False
            nearestWeekday: False
            nthDayOfWeek: 0
            lastDayOfMonth: False
            years: *

            """,
            expression.GetExpressionSummary());
    }

    [Theory]
    [InlineData("0 15 10 * * ? 2005", 0)]
    [InlineData("0 15 10 * * ? 2005", 1)]
    [InlineData("0 15 10 * * ? 2005", 2)]
    [InlineData("0 15 10 * * ? 2005", 3)]
    [InlineData("0 15 10 * * ? 2005", 4)]
    [InlineData("0 15 10 * * ? 2005", 5)]
    [InlineData("58-4 5 21 ? * MON-FRI", 0)]
    [InlineData("0 58-4 21 ? * MON-FRI", 1)]
    [InlineData("0 0/5 21-3 ? * MON-FRI", 2)]
    [InlineData("58 5 21 28-5 1 ?", 3)]
    [InlineData("58 5 21 ? 11-2 FRI", 4)]
    [InlineData("58 5 21 ? * FRI-TUE", 5)]
    [RequirementCoverage("REQ-VSB-CRON-FIELDS", "parsed-fields-are-nonempty")]
    public void ParsedField_ContainsAtLeastOneValue(string text, int fieldIndex)
    {
        var field = new CronExpression(text).GetSet(fieldIndex);

        Assert.NotEmpty(field);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-MATCHING", "exact-instants")]
    public void IsSatisfiedBy_MatchesOnlyNamedInstants()
    {
        var expression = new CronExpression("0 15 10 * * ? 2005") { TimeZone = TimeZoneInfo.Utc };

        Assert.True(expression.IsSatisfiedBy(Utc(2005, 6, 1, 10, 15)));
        Assert.False(expression.IsSatisfiedBy(Utc(2005, 6, 1, 10, 14)));
        Assert.False(expression.IsSatisfiedBy(Utc(2005, 6, 1, 10, 16)));
        Assert.False(expression.IsSatisfiedBy(Utc(2006, 6, 1, 10, 15)));

        var weekdays = new CronExpression("0 15 10 ? * MON-FRI") { TimeZone = TimeZoneInfo.Utc };
        Assert.True(weekdays.IsSatisfiedBy(Utc(2007, 6, 11, 10, 15)));
        Assert.False(weekdays.IsSatisfiedBy(Utc(2007, 6, 9, 10, 15)));
        Assert.False(weekdays.IsSatisfiedBy(Utc(2007, 6, 10, 10, 15)));
    }

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
