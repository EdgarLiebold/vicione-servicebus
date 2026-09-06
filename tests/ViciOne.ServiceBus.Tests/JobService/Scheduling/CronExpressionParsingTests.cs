using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Scheduling;

public sealed class CronExpressionParsingTests
{
    public static TheoryData<string, int[]> LastDayListCases => new()
    {
        { "0 15 10 6,15,LW * ? 2010", [6, 15, 29] },
        { "0 15 10 6,15,L * ? 2010", [6, 15, 31] },
        { "0 15 10 15,L * ? 2010", [15, 31] },
        { "0 15 10 15,31 * ? 2010", [15, 31] },
        { "0 15 10 15,L-2 * ? 2010", [15, 29] },
        { "0 15 10 31,L-2 * ? 2010", [29, 31] },
        { "0 15 10 1,3,6,15,L * ? 2010", [1, 3, 6, 15, 31] },
        { "0 15 10 15,LW-2 * ? 2010", [15, 27] },
    };

    [Theory]
    [MemberData(nameof(LastDayListCases))]
    [RequirementCoverage("REQ-VSB-CRON-PARSING", "last-day-list")]
    public void LastDayLists_MatchEveryDeclaredDay(string text, int[] expectedDays)
    {
        var expression = UtcExpression(text);
        var expected = expectedDays.ToHashSet();

        foreach (var day in Enumerable.Range(1, 31))
            Assert.Equal(expected.Contains(day), expression.IsSatisfiedBy(Utc(2010, 10, day, 10, 15)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "one-last-day-token")]
    public void MultipleLastDayTokens_AreRejected()
    {
        var exception = Assert.Throws<FormatException>(
            () => new CronExpression("0 15 10 L-1,L-2 * ? 2010"));

        Assert.Equal(
            "Support for specifying 'L' with other days of the month is limited to one instance of L",
            exception.Message);
    }

    [Theory]
    [InlineData("L 15 10 15 * ? 2010", false)]
    [InlineData("0 L 10 15 * ? 2010", false)]
    [InlineData("0 15 L 15 * ? 2010", false)]
    [InlineData("0 15 10 L * ? 2010", true)]
    [InlineData("0 15 10 15 L ? 2010", false)]
    [InlineData("0 15 10 ? * L 2010", true)]
    [InlineData("0 15 10 15 * ? L", false)]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "last-token-field")]
    public void LastToken_IsAcceptedOnlyInDayFields(string text, bool isValid)
    {
        Assert.Equal(isValid, CronExpression.IsValidExpression(text));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "nth-week-index")]
    public void NthWeekIndex_MustBeBetweenOneAndFive(int index, bool isValid)
    {
        var text = $"0 15 10 ? * 1#{index} 2010";

        Assert.Equal(isValid, CronExpression.IsValidExpression(text));
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(1, true, 3)]
    [InlineData(2, true, 4)]
    [InlineData(3, true, 5)]
    [InlineData(4, true, 6)]
    [InlineData(5, true, 7)]
    [InlineData(6, true, 1)]
    [InlineData(7, true, 2)]
    [InlineData(8, false, 0)]
    [InlineData(14, false, 0)]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "nth-weekday-index")]
    public void NthWeekdayIndex_MustBeBetweenOneAndSeven(int weekday, bool isValid, int januaryDay)
    {
        var text = $"0 15 10 ? * {weekday}#1 2010";

        Assert.Equal(isValid, CronExpression.IsValidExpression(text));
        if (isValid)
            Assert.True(UtcExpression(text).IsSatisfiedBy(Utc(2010, 1, januaryDay, 10, 15)));
    }

    [Theory]
    [InlineData(' ', true)]
    [InlineData('\t', true)]
    [InlineData('h', false)]
    [InlineData('?', false)]
    [InlineData('*', false)]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "question-mark-suffix")]
    public void QuestionMark_AllowsOnlyTrailingWhitespace(char suffix, bool isValid)
    {
        Assert.Equal(isValid, CronExpression.IsValidExpression($"0 0 * * * ?{suffix}"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "last-day-offset-limit")]
    public void LastDayOffset_GreaterThanThirtyIsRejected()
    {
        var exception = Assert.Throws<FormatException>(
            () => new CronExpression("0 15 10 15,L-31 * ? 2010"));

        Assert.Equal("Offset from last day must be <= 30", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "nearest-weekday-limit")]
    public void NearestWeekday_GreaterThanThirtyOneIsRejected()
    {
        var exception = Assert.Throws<FormatException>(
            () => new CronExpression("0/5 * * 32W 1 ?"));

        Assert.StartsWith("The 'W' option does not make sense with values larger than 31", exception.Message);
    }

    [Theory]
    [InlineData("0 43 9 ? * SAT,SUN,L", false)]
    [InlineData("0 43 9 ? * 6,7,L", false)]
    [InlineData("0 43 9 ? * 5L", true)]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "last-weekday-combination")]
    public void LastWeekdayToken_CannotBeCombinedAsAList(string text, bool isValid)
    {
        Assert.Equal(isValid, CronExpression.IsValidExpression(text));
    }

    [Theory]
    [InlineData("58-4 5 21 ? * MON-FRI", 0, new[] { 0, 1, 2, 3, 4, 58, 59 })]
    [InlineData("0 58-4 21 ? * MON-FRI", 1, new[] { 0, 1, 2, 3, 4, 58, 59 })]
    [InlineData("0 0/5 21-3 ? * MON-FRI", 2, new[] { 0, 1, 2, 3, 21, 22, 23 })]
    [InlineData("58 5 21 28-5 1 ?", 3, new[] { 1, 2, 3, 4, 5, 28, 29, 30, 31 })]
    [InlineData("58 5 21 ? 11-2 FRI", 4, new[] { 1, 2, 11, 12 })]
    [InlineData("58 5 21 ? * 6-2", 5, new[] { 1, 2, 6, 7 })]
    [InlineData("58 5 21 ? * FRI-TUE", 5, new[] { 1, 2, 3, 6, 7 })]
    [RequirementCoverage("REQ-VSB-CRON-PARSING", "wraparound-ranges")]
    public void WraparoundRange_ContainsExactlyTheExpectedValues(string text, int fieldIndex, int[] expected)
    {
        Assert.Equal(expected, new CronExpression(text).GetSet(fieldIndex));
    }

    public static TheoryData<string, string> InvalidIncrementCases => new()
    {
        { "0/0 0 8 ? * 2-6", "Increment must be greater than zero: 0" },
        { "0 0/0 8 ? * 2-6", "Increment must be greater than zero: 0" },
        { "0 0 8/0 ? * 2-6", "Increment must be greater than zero: 0" },
        { "0 0 8 ? 1/0 2-6", "Increment must be greater than zero: 0" },
        { "0 0 8 ? * 2/0", "Increment must be greater than zero: 0" },
        { "0 0 8 ? * 2-6 2026/0", "Increment must be greater than zero: 0" },
        { "/120 0 8-18 ? * 2-6", "Increment > 59 : 120" },
        { "0/120 0 8-18 ? * 2-6", "Increment > 59 : 120" },
        { "/ 0 8-18 ? * 2-6", "'/' must be followed by an integer." },
        { "0/ 0 8-18 ? * 2-6", "'/' must be followed by an integer." },
        { "0 /120 8-18 ? * 2-6", "Increment > 59 : 120" },
        { "0 0/120 8-18 ? * 2-6", "Increment > 59 : 120" },
        { "0 / 8-18 ? * 2-6", "'/' must be followed by an integer." },
        { "0 0/ 8-18 ? * 2-6", "'/' must be followed by an integer." },
        { "0 0 /120 ? * 2-6", "Increment > 23 : 120" },
        { "0 0 0/120 ? * 2-6", "Increment > 23 : 120" },
        { "0 0 / ? * 2-6", "'/' must be followed by an integer." },
        { "0 0 0/ ? * 2-6", "'/' must be followed by an integer." },
        { "0 0 0 /120 * 2-6", "Increment > 31 : 120" },
        { "0 0 0 0/120 * 2-6", "Increment > 31 : 120" },
        { "0 0 0 / * 2-6", "'/' must be followed by an integer." },
        { "0 0 0 0/ * 2-6", "'/' must be followed by an integer." },
        { "0 0 0 ? /120 2-6", "Increment > 12 : 120" },
        { "0 0 0 ? 0/120 2-6", "Increment > 12 : 120" },
        { "0 0 0 ? / 2-6", "'/' must be followed by an integer." },
        { "0 0 0 ? 0/ 2-6", "'/' must be followed by an integer." },
        { "0 0 0 ? * /120", "Increment > 7 : 120" },
        { "0 0 0 ? * 0/120", "Increment > 7 : 120" },
        { "0 0 0 ? * /", "'/' must be followed by an integer." },
        { "0 0 0 ? * 0/", "'/' must be followed by an integer." },
    };

    [Theory]
    [MemberData(nameof(InvalidIncrementCases))]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "increment-range-and-syntax")]
    public void InvalidIncrement_IsRejectedWithItsFieldSpecificReason(string text, string expectedMessage)
    {
        var exception = Assert.Throws<FormatException>(() => new CronExpression(text));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "extra-fields-rejected")]
    public void FieldAfterOptionalYear_IsRejected()
    {
        var exception = Assert.Throws<FormatException>(() => new CronExpression("0 0 8 ? * MON 2026 unexpected"));

        Assert.Equal("Cron expressions contain six required fields and at most one optional year field.", exception.Message);
    }

    [Theory]
    [InlineData("* * * ? * *A&/5:", false)]
    [InlineData("* * * ? *14 ", false)]
    [InlineData(" * * ? *A&/5 *", false)]
    [InlineData("* * ? */5 *", false)]
    [InlineData("* * ? */52 *", false)]
    [InlineData("0 0 15 ? * FRI*", false)]
    [InlineData("0 0 15 5C * ?", false)]
    [InlineData("0 0 15 ? * 5C", false)]
    [InlineData("0 0 15 ? * 5X", false)]
    [InlineData("0 0/30 * * * ?", true)]
    [InlineData("0 0/1 * * * ?", true)]
    [InlineData("0 0/30 * * */2 ?", true)]
    [InlineData("0 0 18-21/1 ? * MON,TUE,WED,THU,FRI,SAT,SUN", true)]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "general-syntax")]
    public void ValidationApi_RejectsGarbageAndAcceptsValidSteps(string text, bool expected)
    {
        Assert.Equal(expected, CronExpression.IsValidExpression(text));
    }

    [Theory]
    [InlineData(" 30 *   * * * ?  ")]
    [InlineData("\t30\t*\t\t*\t*\t*\t?\t")]
    [RequirementCoverage("REQ-VSB-CRON-PARSING", "extra-whitespace")]
    public void ExtraWhitespace_DoesNotChangeTheSchedule(string text)
    {
        var expression = new CronExpression(text) { TimeZone = TimeZoneInfo.Utc };

        Assert.Equal(
            Utc(2025, 1, 1, 0, 1, 30),
            expression.GetTimeAfter(Utc(2025, 1, 1, 0, 0, 31)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "day-of-week-range")]
    public void DayOfWeekOutsideItsRange_IsRejectedWithTheExactReason()
    {
        var exception = Assert.Throws<FormatException>(() => new CronExpression("* * * * * 2025"));

        Assert.Equal("Day-of-Week values must be between 1 and 7", exception.Message);
    }

    private static CronExpression UtcExpression(string text) =>
        new(text) { TimeZone = TimeZoneInfo.Utc };

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute, int second = 0) =>
        new(year, month, day, hour, minute, second, TimeSpan.Zero);
}
