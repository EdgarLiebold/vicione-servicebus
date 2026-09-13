using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.JobService.Scheduling;

/// <summary>Parses and evaluates a six- or seven-field cron expression.</summary>
internal sealed class CronExpression :
    IEquatable<CronExpression>
{
    static readonly Regex _regex = new(@"^L(-\d{1,2})?(W(-\d{1,2})?)?$", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(5));
    static readonly Regex _offsetRegex = new("LW-(?<offset>[0-9]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(5));

    readonly CronField _daysOfMonth = [];
    readonly CronField _daysOfWeek = [];
    readonly CronField _hours = [];
    readonly CronField _minutes = [];
    readonly CronField _months = [];
    readonly CronField _seconds = [];
    readonly CronField _years = [];

    int _everyNthWeek;
    int _lastDayOffset;
    bool _lastDayOfMonth;
    bool _lastDayOfWeek;
    int _lastWeekdayOffset;
    bool _nearestWeekday;
    int _nthDayOfWeek;
    TimeZoneInfo? _timeZone;

    /// <summary>Parses and normalizes a cron expression.</summary>
    /// <param name="cronExpression">The cron expression to parse.</param>
    public CronExpression(string? cronExpression)
    {
        ArgumentNullException.ThrowIfNull(cronExpression);

        string normalizedSpacing = string.Join(
            ' ',
            cronExpression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        CronExpressionString = CultureInfo.InvariantCulture.TextInfo.ToUpper(normalizedSpacing);

        BuildExpression(CronExpressionString);
    }

    /// <summary>Gets or initializes the time zone used to evaluate calendar fields; the local time zone is used by default.</summary>
    public TimeZoneInfo TimeZone
    {
        init => _timeZone = value ?? throw new ArgumentNullException(nameof(value));
        get => _timeZone ??= TimeZoneInfo.Local;
    }

    string CronExpressionString { get; }

    /// <summary>Compares the normalized expression and evaluation time zone.</summary>
    /// <param name="other">The cron expression to compare.</param>
    /// <returns><see langword="true" /> when both expressions and time zones are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(CronExpression? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Equals(_timeZone ?? TimeZoneInfo.Local, other._timeZone ?? TimeZoneInfo.Local)
            && CronExpressionString == other.CronExpressionString;
    }

    /// <summary>Compares an object with this parsed expression.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> is an equal cron expression; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is CronExpression other && Equals(other));
    }

    /// <summary>Returns a hash code for the normalized expression and evaluation time zone.</summary>
    /// <returns>The expression hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return ((_timeZone ?? TimeZoneInfo.Local).GetHashCode() * 397) ^ CronExpressionString.GetHashCode();
        }
    }

    /// <summary>Determines whether two parsed expressions are equal.</summary>
    /// <param name="left">The first expression.</param>
    /// <param name="right">The second expression.</param>
    /// <returns><see langword="true" /> when the expressions are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(CronExpression? left, CronExpression? right)
    {
        return Equals(left, right);
    }

    /// <summary>Determines whether two parsed expressions differ.</summary>
    /// <param name="left">The first expression.</param>
    /// <param name="right">The second expression.</param>
    /// <returns><see langword="true" /> when the expressions differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(CronExpression? left, CronExpression? right)
    {
        return !Equals(left, right);
    }

    /// <summary>Determines whether the expression selects the supplied instant to second precision.</summary>
    /// <param name="date">The instant to evaluate.</param>
    /// <returns><see langword="true" /> when the instant matches the schedule; otherwise, <see langword="false" />.</returns>
    public bool IsSatisfiedBy(DateTimeOffset date)
    {
        var withoutMilliseconds = new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Offset);
        var test = withoutMilliseconds.AddSeconds(-1);
        DateTimeOffset? timeAfter = GetTimeAfter(test);

        return timeAfter.HasValue && timeAfter.Value.Equals(withoutMilliseconds);
    }

    /// <summary>Returns the first scheduled instant strictly after the supplied instant.</summary>
    /// <param name="date">The exclusive lower bound.</param>
    /// <returns>The next scheduled instant, or <see langword="null" /> when the configured year range is exhausted.</returns>
    public DateTimeOffset? GetNextValidTimeAfter(DateTimeOffset date)
    {
        return GetTimeAfter(date);
    }

    /// <summary>Returns the normalized, uppercase cron expression with canonical field spacing.</summary>
    /// <returns>The normalized cron expression.</returns>
    public override string ToString()
    {
        return CronExpressionString;
    }

    /// <summary>Determines whether a cron expression has a supported format.</summary>
    /// <param name="cronExpression">The expression to parse.</param>
    /// <returns><see langword="true" /> when parsing succeeds; otherwise, <see langword="false" />.</returns>
    public static bool IsValidExpression(string? cronExpression)
    {
        try
        {
            _ = new CronExpression(cronExpression);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentNullException)
        {
            return false;
        }

        return true;
    }

    /// <summary>Validates a cron expression and throws when its format is unsupported.</summary>
    /// <param name="cronExpression">The expression to validate.</param>
    public static void ValidateExpression(string cronExpression)
    {
        _ = new CronExpression(cronExpression);
    }

    void BuildExpression(string expression)
    {
        ClearExpressionFields();

        var index = CronExpressionConstants.Second;

        foreach ((ReadOnlySpan<char> expr, ReadOnlySpan<char> _) in expression.SpanSplit(' ', '\t'))
        {
            if (expr.IsEmpty)
                continue;

            if (index > CronExpressionConstants.Year)
                throw new FormatException("Cron expressions contain six required fields and at most one optional year field.");

            ValidateListSyntax(expr, index);

            if (expr.IndexOf(',') != -1)
            {
                foreach (var value in expr.SpanSplit(','))
                    StoreExpressionValues(0, value, index);
            }
            else
                StoreExpressionValues(0, expr, index);

            index++;
        }

        if (index <= CronExpressionConstants.DayOfWeek)
            throw new FormatException("Unexpected end of expression.");

        if (index <= CronExpressionConstants.Year)
            StoreExpressionValues(0, "*".AsSpan(), CronExpressionConstants.Year);
    }

    static void ValidateListSyntax(ReadOnlySpan<char> field, int type)
    {
        if (field.IndexOf(',') < 0)
            return;

        foreach (ReadOnlySpan<char> value in field.SpanSplit(','))
        {
            if (value.IsEmpty)
                throw new FormatException("Cron field lists cannot contain empty values.");
        }

        if (field.IndexOfAny('*', '?') >= 0)
            throw new FormatException("'*' and '?' must be the only value in a cron field.");

        if (type == CronExpressionConstants.DayOfMonth)
        {
            int lastDayTokenCount = 0;
            foreach (ReadOnlySpan<char> value in field.SpanSplit(','))
            {
                if (value.IndexOf('L') >= 0)
                    lastDayTokenCount++;

                if (value[0] != 'L' && value.IndexOf('W') >= 0)
                    throw new FormatException("A numeric 'W' value cannot be combined with other days of the month.");
            }

            if (lastDayTokenCount > 1)
                throw new FormatException("Support for specifying 'L' with other days of the month is limited to one instance of L");
        }

        if (type == CronExpressionConstants.DayOfWeek && field.IndexOf('L') >= 0)
            throw new FormatException("A last day-of-week value using 'L' must be the only value in its field.");

        if (type == CronExpressionConstants.DayOfWeek && field.IndexOf('#') >= 0)
            throw new FormatException("An nth day-of-week value using '#' must be the only value in its field.");
    }

    void ClearExpressionFields()
    {
        _seconds.Clear();
        _minutes.Clear();
        _hours.Clear();
        _daysOfMonth.Clear();
        _months.Clear();
        _daysOfWeek.Clear();
        _years.Clear();
    }

    void StoreExpressionQuestionMark(int type, ReadOnlySpan<char> span, int index)
    {
        index++;
        if (index < span.Length && !char.IsWhiteSpace(span[index]))
            throw new FormatException("Illegal character after '?': " + span[index]);

        if (type != CronExpressionConstants.DayOfWeek && type != CronExpressionConstants.DayOfMonth)
            throw new FormatException("'?' can only be specified for Day-of-Month or Day-of-Week.");

        if (type == CronExpressionConstants.DayOfWeek && !_lastDayOfMonth)
        {
            var val = _daysOfMonth.LastOrDefault();
            if (val == CronExpressionConstants.NoSpec)
                throw new FormatException("'?' can only be specified for Day-of-Month -OR- Day-of-Week.");
        }

        AddToSet(CronExpressionConstants.NoSpec, -1, 0, type);
    }

    void StoreExpressionStarOrSlash(int type, ReadOnlySpan<char> span, int index)
    {
        var ch = span[index];
        var incr = 0;
        var startsWithAsterisk = ch == '*';
        if (startsWithAsterisk && index + 1 >= span.Length)
        {
            AddToSet(CronExpressionConstants.AllSpec, -1, incr, type);
            return;
        }

        if (ch == '/' && (index + 1 >= span.Length || char.IsWhiteSpace(span[index + 1])))
            throw new FormatException("'/' must be followed by an integer.");

        if (startsWithAsterisk)
            index++;

        ch = span[index];
        if (ch == '/')
        {
            incr = ParseIncrement(span, index, type);
        }
        else
        {
            if (startsWithAsterisk)
                throw new FormatException("Illegal characters after asterisk: " + span.ToString());

            incr = 1;
        }

        AddToSet(CronExpressionConstants.AllSpec, -1, incr, type);
    }

    void StoreExpressionL(int type, ReadOnlySpan<char> span, int index)
    {
        index++;
        switch (type)
        {
            case CronExpressionConstants.DayOfMonth:
                {
                    _lastDayOfMonth = true;
                    if (span.Length > index)
                    {
                        var ch = span[index];
                        if (ch == '-')
                        {
                            (_lastDayOffset, index) = GetValue(0, span, index + 1);
                            if (_lastDayOffset > 30)
                                throw new FormatException("Offset from last day must be <= 30");
                        }

                        if (span.Length > index)
                        {
                            ch = span[index];
                            if (ch == 'W')
                                _nearestWeekday = true;

                            var match = _offsetRegex.Match(span.ToString());
                            if (match.Success)
                            {
                                var offSetGroup = match.Groups["offset"];
                                if (offSetGroup.Success)
                                    _lastWeekdayOffset = int.Parse(offSetGroup.Value);
                            }
                        }
                    }

                    break;
                }

            case CronExpressionConstants.DayOfWeek:
                AddToSet(7, 7, 0, type);
                break;

            default:
                throw new FormatException($"'L' option is not valid here. (pos={index})");
        }
    }

    void StoreExpressionNumeric(int type, ReadOnlySpan<char> span, int index)
    {
        if (int.TryParse(span, NumberStyles.None, CultureInfo.InvariantCulture, out var temp))
        {
            AddToSet(temp, -1, -1, type);
            return;
        }

        var ch = span[index];
        var value = ToInt32(ch);
        index++;
        if (index >= span.Length)
            AddToSet(value, -1, -1, type);
        else
        {
            ch = span[index];
            if (char.IsDigit(ch))
                (value, index) = GetValue(value, span, index);

            CheckNext(index, span, value, type);
        }
    }

    void StoreExpressionGeneralValue(int type, ReadOnlySpan<char> span, int index)
    {
        var incr = 0;
        if (span.Length - index < 3)
            throw new FormatException($"Incomplete named cron value: '{span}'.");

        ReadOnlySpan<char> sub = span[index..(index + 3)];
        int sval;
        var eval = -1;
        if (type == CronExpressionConstants.Month)
        {
            sval = GetMonthNumber(sub) + 1;
            if (sval <= 0)
                throw new FormatException($"Invalid Month value: '{sub}'");

            int suffixIndex = index + 3;
            if (span.Length > suffixIndex)
            {
                switch (span[suffixIndex])
                {
                    case '-':
                        index = suffixIndex + 1;
                        if (span.Length - index < 3)
                            throw new FormatException($"Incomplete named cron range: '{span}'.");

                        sub = span[index..(index + 3)];
                        eval = GetMonthNumber(sub) + 1;
                        if (eval <= 0)
                            throw new FormatException($"Invalid Month value: '{sub}'");

                        suffixIndex = index + 3;
                        incr = 1;
                        if (span.Length > suffixIndex)
                        {
                            if (span[suffixIndex] != '/')
                                throw new FormatException($"Unexpected character '{span[suffixIndex]}'.");

                            incr = ParseIncrement(span, suffixIndex, type);
                        }

                        break;
                    case '/':
                        incr = ParseIncrement(span, suffixIndex, type);
                        break;
                    default:
                        throw new FormatException($"Unexpected character '{span[suffixIndex]}'.");
                }
            }
        }
        else if (type == CronExpressionConstants.DayOfWeek)
        {
            sval = GetDayOfWeekNumber(sub);
            if (sval < 0)
                throw new FormatException($"Invalid Day-of-Week value: '{sub}'");

            if (span.Length > index + 3)
            {
                var c = span[index + 3];
                switch (c)
                {
                    case '-':
                        index += 4;
                        if (span.Length - index < 3)
                            throw new FormatException($"Incomplete named cron range: '{span}'.");

                        sub = span[index..(index + 3)];
                        eval = GetDayOfWeekNumber(sub);
                        if (eval < 0)
                            throw new FormatException($"Invalid Day-of-Week value: '{sub}'");

                        int suffixIndex = index + 3;
                        if (span.Length > suffixIndex)
                        {
                            if (span[suffixIndex] != '/')
                                throw new FormatException($"Unexpected character '{span[suffixIndex]}'.");

                            incr = ParseIncrement(span, suffixIndex, type);
                        }

                        break;
                    case '#':
                        index += 4;
                        if (!TryParsePositiveInteger(span[index..], out _nthDayOfWeek)
                            || _nthDayOfWeek is < 1 or > 5)
                            throw new FormatException("A numeric value between 1 and 5 must follow the '#' option");

                        break;
                    case '/':
                        index += 4;
                        if (!TryParsePositiveInteger(span[index..], out _everyNthWeek)
                            || _everyNthWeek is < 1 or > 5)
                            throw new FormatException("A numeric value between 1 and 5 must follow the '/' option");

                        break;
                    case 'L':
                        if (span.Length != index + 4)
                            throw new FormatException($"Unexpected character '{span[index + 4]}'.");

                        _lastDayOfWeek = true;
                        break;
                    default:
                        throw new FormatException($"Illegal characters for this position: '{sub}'");
                }
            }
        }
        else
            throw new FormatException($"Illegal characters for this position: '{sub}'");

        if (eval != -1 && incr == 0)
            incr = 1;

        AddToSet(sval, eval, incr, type);
    }

    void StoreExpressionValues(int position, ReadOnlySpan<char> span, int type)
    {
        var index = position;
        if (index < span.Length && char.IsWhiteSpace(span[index]))
            index = SkipWhiteSpace(position, span);

        if (index >= span.Length)
            return;

        switch (span[index])
        {
            case >= 'A' and <= 'Z' when !span.SequenceEqual("L".AsSpan()) && !_regex.IsMatch(span.ToString()):
                StoreExpressionGeneralValue(type, span, index);
                break;

            case '?':
                StoreExpressionQuestionMark(type, span, index);
                break;

            case '*':
            case '/':
                StoreExpressionStarOrSlash(type, span, index);
                break;

            case 'L':
                StoreExpressionL(type, span, index);
                break;

            case >= '0' and <= '9':
                StoreExpressionNumeric(type, span, index);
                break;
            default:
                throw new FormatException($"Unexpected character: {span[index]}");
        }
    }

    static void CheckIncrementRange(int increment, int type)
    {
        if (increment <= 0)
            throw new FormatException($"Increment must be greater than zero: {increment}");

        switch (type)
        {
            case CronExpressionConstants.Second or CronExpressionConstants.Minute when increment > 59:
                throw new FormatException($"Increment > 59 : {increment}");
            case CronExpressionConstants.Hour when increment > 23:
                throw new FormatException($"Increment > 23 : {increment}");
            case CronExpressionConstants.DayOfMonth when increment > 31:
                throw new FormatException($"Increment > 31 : {increment}");
            case CronExpressionConstants.DayOfWeek when increment > 7:
                throw new FormatException($"Increment > 7 : {increment}");
            case CronExpressionConstants.Month when increment > 12:
                throw new FormatException($"Increment > 12 : {increment}");
        }
    }

    void CheckNext(int position, ReadOnlySpan<char> span, int value, int type)
    {
        if (position >= span.Length)
        {
            AddToSet(value, -1, -1, type);
            return;
        }

        switch (span[position])
        {
            case 'L':
                if (position + 1 != span.Length)
                    throw new FormatException($"Unexpected character '{span[position + 1]}'.");

                HandleLOption(value, type, position);
                return;

            case 'W':
                if (position + 1 != span.Length)
                    throw new FormatException($"Unexpected character '{span[position + 1]}'.");

                HandleWOption(value, type, position);
                return;

            case '#':
                HandleHashOption(span, value, type, position);
                return;

            case '-':
                HandleDashOption(span, value, type, position);
                return;

            case '/':
                HandleSlashOption(span, value, type, position, -1);
                return;

            default:
                throw new FormatException($"Unexpected character '{span[position]}'.");
        }
    }

    void HandleSlashOption(ReadOnlySpan<char> span, int value, int type, int index, int end)
    {
        int increment = ParseIncrement(span, index, type);
        AddToSet(value, end, increment, type);
    }

    void HandleDashOption(ReadOnlySpan<char> span, int value, int type, int index)
    {
        index++;
        if (index >= span.Length || !char.IsAsciiDigit(span[index]))
            throw new FormatException("'-' must be followed by an integer.");

        var ch = span[index];
        var charValue = ToInt32(ch);
        var end = charValue;
        index++;
        if (index >= span.Length)
        {
            AddToSet(value, end, 1, type);
            return;
        }

        ch = span[index];
        if (char.IsDigit(ch))
            (end, index) = GetValue(charValue, span, index);

        if (index < span.Length && span[index] == '/')
        {
            int increment = ParseIncrement(span, index, type);
            AddToSet(value, end, increment, type);
            return;
        }

        if (index < span.Length)
            throw new FormatException($"Unexpected character '{span[index]}'.");

        AddToSet(value, end, 1, type);
    }

    static int ParseIncrement(ReadOnlySpan<char> span, int slashIndex, int type)
    {
        ReadOnlySpan<char> incrementText = span.Slice(slashIndex + 1);
        if (incrementText.IsEmpty)
            throw new FormatException("'/' must be followed by an integer.");

        if (!int.TryParse(incrementText, NumberStyles.None, CultureInfo.InvariantCulture, out int increment))
        {
            foreach (char character in incrementText)
            {
                if (!char.IsAsciiDigit(character))
                    throw new FormatException($"Unexpected character '{character}' after '/'");
            }

            throw new FormatException("The increment is too large.");
        }

        CheckIncrementRange(increment, type);
        return increment;
    }

    void HandleHashOption(ReadOnlySpan<char> span, int value, int type, int index)
    {
        if (type != CronExpressionConstants.DayOfWeek)
            throw new FormatException($"'#' option is not valid here. (pos={index})");

        index++;
        if (index >= span.Length || !char.IsAsciiDigit(span[index]))
            throw new FormatException("A numeric value between 1 and 5 must follow the '#' option");

        if (!TryParsePositiveInteger(span.Slice(index), out _nthDayOfWeek)
            || _nthDayOfWeek is < 1 or > 5)
            throw new FormatException("A numeric value between 1 and 5 must follow the '#' option");

        if (value is < 1 or > 7)
            throw new FormatException("Day-of-Week values must be between 1 and 7");

        GetSet(type).Add(value);
    }

    static bool TryParsePositiveInteger(ReadOnlySpan<char> value, out int result)
    {
        result = 0;

        return !value.IsEmpty
            && value.IndexOfAnyExceptInRange('0', '9') < 0
            && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
    }

    void HandleWOption(int value, int type, int index)
    {
        if (type == CronExpressionConstants.DayOfMonth)
            _nearestWeekday = true;
        else
            throw new FormatException($"'W' option is not valid here. (pos={index})");

        if (value > 31)
            throw new FormatException("The 'W' option does not make sense with values larger than 31 (max number of days in a month)");

        var data = GetSet(type);
        data.Add(value);
    }

    void HandleLOption(int value, int type, int position)
    {
        if (type == CronExpressionConstants.DayOfWeek)
        {
            if (value is < 1 or > 7)
                throw new FormatException("Day-of-Week values must be between 1 and 7");

            _lastDayOfWeek = true;
        }
        else
            throw new FormatException($"'L' option is not valid here. (pos={position})");

        var data = GetSet(type);
        data.Add(value);
    }

    /// <summary>Formats the parsed values of every cron field for diagnostics.</summary>
    /// <returns>A multi-line field summary.</returns>
    public string GetExpressionSummary()
    {
        return new CronExpressionSummary(
            _seconds,
            _minutes,
            _hours,
            _daysOfMonth,
            _months,
            _daysOfWeek,
            _lastDayOfWeek,
            _nearestWeekday,
            _nthDayOfWeek,
            _lastDayOfMonth,
            _years
        ).ToString();
    }

    static int SkipWhiteSpace(int position, ReadOnlySpan<char> span)
    {
        for (; position < span.Length && char.IsWhiteSpace(span[position]); position++)
        {
        }

        return position;
    }

    static (int min, int max, string errorMessage) GetValidationParameters(int type)
    {
        return type switch
        {
            CronExpressionConstants.Second or CronExpressionConstants.Minute
                => (0, 59, "Minute and Second values must be between 0 and 59"),
            CronExpressionConstants.Hour
                => (0, 23, "Hour values must be between 0 and 23"),
            CronExpressionConstants.DayOfMonth
                => (1, 31, "Day of month values must be between 1 and 31"),
            CronExpressionConstants.Month
                => (1, 12, "Month values must be between 1 and 12"),
            CronExpressionConstants.DayOfWeek
                => (1, 7, "Day-of-Week values must be between 1 and 7"),
            CronExpressionConstants.Year
                => (CronYearRange.FirstYear, CronYearRange.LastYear, $"Year values must be between {CronYearRange.FirstYear} and {CronYearRange.LastYear}"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid cron expression type")
        };
    }

    static bool IsSpecialValue(int value, int type)
    {
        return value == CronExpressionConstants.AllSpec ||
            (type is CronExpressionConstants.DayOfMonth or CronExpressionConstants.DayOfWeek && value == CronExpressionConstants.NoSpec);
    }

    static void ValidateSetValues(int value, int end, int type)
    {
        var (min, max, errorMessage) = GetValidationParameters(type);

        if ((value < min || value > max || end > max) && !IsSpecialValue(value, type))
            throw new FormatException(errorMessage);
    }

    static (int startAt, int stopAt) GetRangeForType(int type, int value, int end)
    {
        return type switch
        {
            CronExpressionConstants.Second or CronExpressionConstants.Minute => (GetStartAt(value, 0), GetStopAt(end, 59)),
            CronExpressionConstants.Hour => (GetStartAt(value, 0), GetStopAt(end, 23)),
            CronExpressionConstants.DayOfMonth => (GetStartAt(value, 1), GetStopAt(end, 31)),
            CronExpressionConstants.Month => (GetStartAt(value, 1), GetStopAt(end, 12)),
            CronExpressionConstants.DayOfWeek => (GetStartAt(value, 1), GetStopAt(end, 7)),
            CronExpressionConstants.Year => (GetStartAt(value, CronYearRange.FirstYear), GetStopAt(end, CronYearRange.LastYear)),
            _ => throw new ArgumentException("Unexpected type encountered")
        };
    }

    /// <summary>Returns the field width required to expand a wraparound range.</summary>
    /// <param name="type">The cron field index.</param>
    /// <param name="startAt">The inclusive range start.</param>
    /// <param name="stopAt">The inclusive range end.</param>
    /// <returns>The field width when the range wraps, or <c>-1</c> when it does not wrap.</returns>
    static int GetMaxValueForType(int type, int startAt, int stopAt)
    {
        if (stopAt >= startAt)
            return -1;

        return type switch
        {
            CronExpressionConstants.Second or CronExpressionConstants.Minute => 60,
            CronExpressionConstants.Hour => 24,
            CronExpressionConstants.Month => 12,
            CronExpressionConstants.DayOfWeek => 7,
            CronExpressionConstants.DayOfMonth => 31,
            CronExpressionConstants.Year => throw new ArgumentException("Start year must be less than stop year"),
            _ => throw new ArgumentException("Unexpected type encountered")
        };
    }

    static int GetStartAt(int value, int defaultValue)
    {
        return value is -1 or CronExpressionConstants.AllSpec ? defaultValue : value;
    }

    static int GetStopAt(int end, int defaultValue)
    {
        return end == -1 ? defaultValue : end;
    }

    void AddToSet(int value, int end, int increment, int type)
    {
        ValidateSetValues(value, end, type);

        var data = GetSet(type);

        if (increment is 0 or -1 && value != CronExpressionConstants.AllSpec)
        {
            data.Add(value != -1 ? value : CronExpressionConstants.NoSpec);
            return;
        }

        if (value == CronExpressionConstants.AllSpec && increment <= 0)
        {
            data.Add(CronExpressionConstants.AllSpec);
            return;
        }

        var (startAt, stopAt) = GetRangeForType(type, value, end);

        var max = GetMaxValueForType(type, startAt, stopAt);
        if (max != -1)
            stopAt += max;

        for (var i = startAt; i <= stopAt; i += increment)
        {
            if (max == -1)
                data.Add(i);
            else
            {
                var i2 = i % max;

                if (i2 == 0 && type is CronExpressionConstants.Month or CronExpressionConstants.DayOfWeek or CronExpressionConstants.DayOfMonth)
                    i2 = max;

                data.Add(i2);
            }
        }
    }

    /// <summary>Returns the parsed values for a cron field.</summary>
    /// <param name="type">The zero-based cron field index.</param>
    /// <returns>The parsed field values.</returns>
    public CronField GetSet(int type)
    {
        var field = type switch
        {
            CronExpressionConstants.Second => _seconds,
            CronExpressionConstants.Minute => _minutes,
            CronExpressionConstants.Hour => _hours,
            CronExpressionConstants.DayOfMonth => _daysOfMonth,
            CronExpressionConstants.Month => _months,
            CronExpressionConstants.DayOfWeek => _daysOfWeek,
            CronExpressionConstants.Year => _years,
            _ => default
        };

        return field ?? throw new ArgumentOutOfRangeException(nameof(type));
    }

    static (int Value, int Position) GetValue(int value, ReadOnlySpan<char> span, int index)
    {
        var ch = span[index];

        var builder = new StringBuilder(span.Length);
        builder.Append(value);

        while (char.IsDigit(ch))
        {
            builder.Append(ch);
            index++;
            if (index >= span.Length)
                break;

            ch = span[index];
        }

        if (!int.TryParse(builder.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out int parsedValue))
            throw new FormatException("Numeric cron value is too large.");

        return (parsedValue, index < span.Length ? index : index + 1);
    }

    /// <summary>Maps a three-letter month abbreviation to its zero-based ordinal.</summary>
    /// <param name="span">The uppercase abbreviation.</param>
    /// <returns>The zero-based month ordinal, or <c>-1</c> when the abbreviation is unknown.</returns>
    static int GetMonthNumber(ReadOnlySpan<char> span)
    {
        return span switch
        {
            "JAN" => 0,
            "FEB" => 1,
            "MAR" => 2,
            "APR" => 3,
            "MAY" => 4,
            "JUN" => 5,
            "JUL" => 6,
            "AUG" => 7,
            "SEP" => 8,
            "OCT" => 9,
            "NOV" => 10,
            "DEC" => 11,
            _ => -1
        };
    }

    static int GetDayOfWeekNumber(ReadOnlySpan<char> span)
    {
        return span switch
        {
            "SUN" => 1,
            "MON" => 2,
            "TUE" => 3,
            "WED" => 4,
            "THU" => 5,
            "FRI" => 6,
            "SAT" => 7,
            _ => -1
        };
    }

    /// <summary>Advances a candidate to the next permitted second.</summary>
    /// <param name="date">The candidate local time.</param>
    /// <returns>The next fire time cursor produced by the operation.</returns>
    NextFireTimeCursor ProgressNextFireTimeSecond(DateTimeOffset date)
    {
        var second = date.Second;
        if (_seconds.TryGetMinValueStartingFrom(second, out var min))
            second = min;
        else
        {
            second = _seconds.Min;
            date = date.AddMinutes(1);
        }

        return new NextFireTimeCursor(false,
            new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, second, date.Millisecond, date.Offset));
    }

    /// <summary>Advances a candidate to the next permitted minute.</summary>
    /// <param name="date">The candidate local time.</param>
    /// <returns>The next fire time cursor produced by the operation.</returns>
    NextFireTimeCursor ProgressNextFireTimeMinute(DateTimeOffset date)
    {
        var minute = date.Minute;
        var hour = date.Hour;
        var t = -1;

        if (_minutes.TryGetMinValueStartingFrom(minute, out var min))
        {
            t = minute;
            minute = min;
        }
        else
        {
            minute = _minutes.Min;
            hour++;
        }

        if (minute != t)
        {
            date = new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, minute, 0, date.Millisecond, date.Offset);
            date = SetCalendarHour(date, hour);
            return new NextFireTimeCursor(true, date);
        }

        return new NextFireTimeCursor(false,
            new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, minute, date.Second, date.Millisecond, date.Offset));
    }

    /// <summary>Advances a candidate to the next permitted hour.</summary>
    /// <param name="date">The candidate local time.</param>
    /// <returns>The next fire time cursor produced by the operation.</returns>
    NextFireTimeCursor ProgressNextFireTimeHour(DateTimeOffset date)
    {
        int hour;
        var day = date.Day;
        var t = -1;

        if (_hours.TryGetMinValueStartingFrom(date.Hour, out var min))
        {
            t = date.Hour;
            hour = min;
        }
        else
        {
            hour = _hours.Min;
            day++;
        }

        if (hour != t)
        {
            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            date = day > daysInMonth
                ? new DateTimeOffset(date.Year, date.Month, daysInMonth, date.Hour, 0, 0, date.Millisecond, date.Offset).AddDays(day - daysInMonth)
                : new DateTimeOffset(date.Year, date.Month, day, date.Hour, 0, 0, date.Millisecond, date.Offset);

            date = SetCalendarHour(date, hour);
            return new NextFireTimeCursor(true, date);
        }

        return new NextFireTimeCursor(false,
            new DateTimeOffset(date.Year, date.Month, date.Day, hour, date.Minute, date.Second, date.Millisecond, date.Offset));
    }

    (SortedSet<int> daysOfMonthSet, bool dayHasNegativeOffset) CalculateDaysOfMonth(DateTimeOffset date)
    {
        var daysOfMonthSet = new SortedSet<int>(_daysOfMonth);
        var dayHasNegativeOffset = false;

        if (_lastDayOfMonth)
        {
            var lastDayOfMonthValue = GetLastDayOfMonth(date.Month, date.Year);
            var lastDayOfMonthWithOffset = lastDayOfMonthValue - _lastDayOffset;

            if (_nearestWeekday)
            {
                var calculatedLastDay = CalculateNearestWeekdayForLastDay(date, lastDayOfMonthWithOffset);
                daysOfMonthSet.Add(calculatedLastDay);
            }
            else
                daysOfMonthSet.Add(lastDayOfMonthWithOffset);
        }
        else if (_nearestWeekday)
            (daysOfMonthSet, dayHasNegativeOffset) = CalculateNearestWeekdayForDaysOfMonth(date, daysOfMonthSet);

        return (daysOfMonthSet, dayHasNegativeOffset);
    }

    int CalculateNearestWeekdayForLastDay(DateTimeOffset date, int lastDayOfMonthWithOffset)
    {
        var checkDay = new DateTimeOffset(date.Year, date.Month, lastDayOfMonthWithOffset, date.Hour, date.Minute, date.Second, date.Millisecond, date.Offset);
        var calculatedDay = lastDayOfMonthWithOffset;

        switch (checkDay.DayOfWeek)
        {
            case DayOfWeek.Saturday:
                calculatedDay -= 1;
                break;
            case DayOfWeek.Sunday:
                calculatedDay -= 2;
                break;
        }

        var calculatedLastDayWithOffset = calculatedDay - _lastWeekdayOffset;

        if (calculatedLastDayWithOffset <= 0)
            calculatedLastDayWithOffset = 1;

        return calculatedLastDayWithOffset;
    }

    static (SortedSet<int> daysOfMonthSet, bool dayHasNegativeOffset) CalculateNearestWeekdayForDaysOfMonth(DateTimeOffset date, SortedSet<int> daysOfMonthSet)
    {
        var endDayOfMonth = GetLastDayOfMonth(date.Month, date.Year);
        var minDay = daysOfMonthSet.Min > endDayOfMonth ? endDayOfMonth : daysOfMonthSet.Min;

        var firstDayOfMonth = new DateTimeOffset(date.Year, date.Month, minDay, 0, 0, 0, date.Offset);
        var dayOfWeek = firstDayOfMonth.DayOfWeek;

        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            daysOfMonthSet.Remove(minDay);

        var (adjustedDay, dayHasNegativeOffset) = AdjustDayToNearestWeekday(minDay, dayOfWeek, endDayOfMonth);
        daysOfMonthSet.Add(adjustedDay);

        return (daysOfMonthSet, dayHasNegativeOffset);
    }

    static (int day, bool dayHasNegativeOffset) AdjustDayToNearestWeekday(int day, DayOfWeek dayOfWeek, int endDayOfMonth)
    {
        var dayHasNegativeOffset = false;

        switch (dayOfWeek)
        {
            case DayOfWeek.Saturday when day == 1:
                day += 2;
                break;
            case DayOfWeek.Saturday:
                day -= 1;
                dayHasNegativeOffset = true;
                break;
            case DayOfWeek.Sunday when day == endDayOfMonth:
                day -= 2;
                dayHasNegativeOffset = true;
                break;
            case DayOfWeek.Sunday:
                day += 1;
                break;
        }

        return (day, dayHasNegativeOffset);
    }

    NextFireTimeCursor ProgressNextFireTimeDayOfMonth(DateTimeOffset date)
    {
        var day = date.Day;
        var month = date.Month;
        var tDay = -1;
        var tMonth = month;

        // Resolve the next eligible day from the day-of-month rule.
        (SortedSet<int>? daysOfMonthCalculated, var setIncludesDayBeforeStartDay) = CalculateDaysOfMonth(date);
        if (daysOfMonthCalculated.TryGetMinValueStartingFrom(date, setIncludesDayBeforeStartDay, out var min))
        {
            tDay = day;
            day = min;

            // Short months advance to the next month instead of producing an invalid date.
            var lastDay = GetLastDayOfMonth(month, date.Year);
            if (day > lastDay)
            {
                day = daysOfMonthCalculated.Min;
                month++;
            }
        }
        else
        {
            day = _lastDayOfMonth ? daysOfMonthCalculated.Min : _daysOfMonth.Min;

            month++;
        }

        if (day != tDay || month != tMonth)
        {
            if (month > 12)
                date = new DateTimeOffset(date.Year, 12, day, 0, 0, 0, date.Offset).AddMonths(month - 12);
            else
            {
                var daysInMonth = DateTime.DaysInMonth(date.Year, month);

                date = day <= daysInMonth
                    ? new DateTimeOffset(date.Year, month, day, 0, 0, 0, date.Offset)
                    : new DateTimeOffset(date.Year, month, daysInMonth, 0, 0, 0, date.Offset).AddDays(day - daysInMonth);
            }

            return new NextFireTimeCursor(true, date);
        }

        return new NextFireTimeCursor(false, date);
    }

    NextFireTimeCursor ProgressNextFireTimeDayOfWeek(DateTimeOffset date)
    {
        var day = date.Day;
        var month = date.Month;

        if (_lastDayOfWeek)
        {
            var dayOfWeek = _daysOfWeek.Min;
            var currentDayOfWeek = (int)date.DayOfWeek + 1;
            var daysToAdd = 0;
            if (currentDayOfWeek < dayOfWeek)
                daysToAdd = dayOfWeek - currentDayOfWeek;

            if (currentDayOfWeek > dayOfWeek)
                daysToAdd = dayOfWeek + (7 - currentDayOfWeek);

            var lastDayOfMonth = GetLastDayOfMonth(month, date.Year);

            if (day + daysToAdd > lastDayOfMonth)
            {
                if (month == 12)
                    date = new DateTimeOffset(date.Year, month - 11, 1, 0, 0, 0, date.Offset).AddYears(1);
                else
                    date = new DateTimeOffset(date.Year, month + 1, 1, 0, 0, 0, date.Offset);

                return new NextFireTimeCursor(true, date);
            }

            while (day + daysToAdd + 7 <= lastDayOfMonth)
                daysToAdd += 7;

            day += daysToAdd;

            if (daysToAdd > 0)
                return new NextFireTimeCursor(true, new DateTimeOffset(date.Year, month, day, 0, 0, 0, date.Offset));
        }
        else if (_nthDayOfWeek != 0)
        {
            var dayOfWeek = _daysOfWeek.Min;
            var currentDayOfWeek = (int)date.DayOfWeek + 1;
            var daysToAdd = 0;
            if (currentDayOfWeek < dayOfWeek)
                daysToAdd = dayOfWeek - currentDayOfWeek;
            else if (currentDayOfWeek > dayOfWeek)
                daysToAdd = dayOfWeek + (7 - currentDayOfWeek);

            var dayShifted = daysToAdd > 0;

            day += daysToAdd;
            var weekOfMonth = day / 7;
            if (day % 7 > 0)
                weekOfMonth++;

            daysToAdd = (_nthDayOfWeek - weekOfMonth) * 7;
            day += daysToAdd;
            if (daysToAdd < 0 || day > GetLastDayOfMonth(month, date.Year))
            {
                date = month == 12
                    ? new DateTimeOffset(date.Year, month - 11, 1, 0, 0, 0, date.Offset).AddYears(1)
                    : new DateTimeOffset(date.Year, month + 1, 1, 0, 0, 0, date.Offset);

                return new NextFireTimeCursor(true, date);
            }

            if (daysToAdd > 0 || dayShifted)
                return new NextFireTimeCursor(true, new DateTimeOffset(date.Year, month, day, 0, 0, 0, date.Offset));
        }
        else if (_everyNthWeek != 0)
        {
            var currentDayOfWeek = (int)date.DayOfWeek + 1;
            var dayOfWeek = _daysOfWeek.Min;
            if (_daysOfWeek.TryGetMinValueStartingFrom(currentDayOfWeek, out var min))
                dayOfWeek = min;

            var daysToAdd = 0;
            if (currentDayOfWeek < dayOfWeek)
                daysToAdd = dayOfWeek - currentDayOfWeek + 7 * (_everyNthWeek - 1);

            if (currentDayOfWeek > dayOfWeek)
                daysToAdd = dayOfWeek + (7 - currentDayOfWeek) + 7 * (_everyNthWeek - 1);

            if (daysToAdd > 0)
            {
                date = new DateTimeOffset(date.Year, month, day, 0, 0, 0, date.Offset);
                date = date.AddDays(daysToAdd);
                return new NextFireTimeCursor(true, date);
            }
        }
        else
        {
            var currentDayOfWeek = (int)date.DayOfWeek + 1;
            var dayOfWeek = _daysOfWeek.Min;
            if (_daysOfWeek.TryGetMinValueStartingFrom(currentDayOfWeek, out var min))
                dayOfWeek = min;

            var daysToAdd = 0;
            if (currentDayOfWeek < dayOfWeek)
                daysToAdd = dayOfWeek - currentDayOfWeek;

            if (currentDayOfWeek > dayOfWeek)
                daysToAdd = dayOfWeek + (7 - currentDayOfWeek);

            var lDay = GetLastDayOfMonth(month, date.Year);

            if (day + daysToAdd > lDay)
            {
                date = month == 12
                    ? new DateTimeOffset(date.Year, month - 11, 1, 0, 0, 0, date.Offset).AddYears(1)
                    : new DateTimeOffset(date.Year, month + 1, 1, 0, 0, 0, date.Offset);

                return new NextFireTimeCursor(true, date);
            }

            if (daysToAdd > 0)
                return new NextFireTimeCursor(true, new DateTimeOffset(date.Year, month, day + daysToAdd, 0, 0, 0, date.Offset));
        }

        return new NextFireTimeCursor(false, new DateTimeOffset(date.Year, date.Month, day, date.Hour, date.Minute, date.Second, date.Offset));
    }

    NextFireTimeCursor ProgressNextFireTimeDay(DateTimeOffset date)
    {
        var dayOfMonthSpec = !_daysOfMonth.Contains(CronExpressionConstants.NoSpec);
        var dayOfWeekSpec = !_daysOfWeek.Contains(CronExpressionConstants.NoSpec);
        if (dayOfMonthSpec && !dayOfWeekSpec)
            return ProgressNextFireTimeDayOfMonth(date);

        if (dayOfWeekSpec && !dayOfMonthSpec)
            return ProgressNextFireTimeDayOfWeek(date);

        var dayOfMonthProgressResult = ProgressNextFireTimeDayOfMonth(date);
        var dayOfWeekProgressResult = ProgressNextFireTimeDayOfWeek(date);
        if (dayOfMonthProgressResult.RestartLoop && dayOfWeekProgressResult.RestartLoop)
        {
            return dayOfWeekProgressResult.Date < dayOfMonthProgressResult.Date
                ? dayOfWeekProgressResult
                : dayOfMonthProgressResult;
        }

        if (dayOfWeekProgressResult is { Date: not null, RestartLoop: false })
            return dayOfWeekProgressResult;
        if (dayOfMonthProgressResult is { Date: not null, RestartLoop: false })
            return dayOfMonthProgressResult;

        return dayOfWeekProgressResult.Date!.Value < dayOfMonthProgressResult.Date!.Value
            ? dayOfWeekProgressResult
            : dayOfMonthProgressResult;
    }

    NextFireTimeCursor ProgressNextFireTimeMonth(DateTimeOffset date)
    {
        var month = date.Month;
        var year = date.Year;
        var tMonth = -1;

        if (_months.TryGetMinValueStartingFrom(month, out var min))
        {
            tMonth = month;
            month = min;
        }
        else
        {
            month = _months.Min;
            year++;
        }

        return month != tMonth
            ? new NextFireTimeCursor(true, new DateTimeOffset(year, month, 1, 0, 0, 0, date.Offset))
            : new NextFireTimeCursor(false, new DateTimeOffset(date.Year, month, date.Day, date.Hour, date.Minute, date.Second, date.Offset));
    }

    NextFireTimeCursor ProgressNextFireTimeYear(DateTimeOffset date)
    {
        var year = date.Year;
        int tYear;
        if (_years.TryGetMinValueStartingFrom(date.Year, out var min))
        {
            tYear = year;
            year = min;
        }
        else
            return new NextFireTimeCursor(false, null);

        return year != tYear
            ? new NextFireTimeCursor(true, new DateTimeOffset(year, 1, 1, 0, 0, 0, date.Offset))
            : new NextFireTimeCursor(false, new DateTimeOffset(year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Offset));
    }

    /// <summary>Calculates the first scheduled instant strictly after a lower bound.</summary>
    /// <param name="afterTime">The exclusive lower bound.</param>
    /// <returns>The next scheduled UTC instant, or <see langword="null" /> when no supported year remains.</returns>
    public DateTimeOffset? GetTimeAfter(DateTimeOffset afterTime)
    {
        afterTime = afterTime.AddSeconds(1);

        var date = StripMilliseconds(afterTime);

        date = TimeZoneResolver.ConvertTime(date, TimeZone);

        Func<DateTimeOffset, NextFireTimeCursor>[] nextFireTimeProgressions =
        [
            ProgressNextFireTimeSecond,
            ProgressNextFireTimeMinute,
            ProgressNextFireTimeHour,
            ProgressNextFireTimeDay,
            ProgressNextFireTimeMonth,
            ProgressNextFireTimeYear
        ];

        var nextFireTimeCursor = new NextFireTimeCursor(false, date);
        var foundNextFireTime = false;

        while (!foundNextFireTime)
        {
            foreach (Func<DateTimeOffset, NextFireTimeCursor> progression in nextFireTimeProgressions)
            {
                if (nextFireTimeCursor.Date.HasValue)
                    nextFireTimeCursor = progression(nextFireTimeCursor.Date.Value);
                else
                    break;

                if (nextFireTimeCursor.RestartLoop)
                    break;
            }

            if (nextFireTimeCursor.Date is null || nextFireTimeCursor.Date.Value.Year > CronYearRange.LastYear)
                return null;

            if (nextFireTimeCursor.RestartLoop)
                continue;

            date = new DateTimeOffset(nextFireTimeCursor.Date.Value.DateTime,
                TimeZoneResolver.GetAmbiguousTimeUtcOffset(nextFireTimeCursor.Date.Value, TimeZone));
            foundNextFireTime = true;
        }

        return date.ToUniversalTime();
    }

    static DateTimeOffset StripMilliseconds(DateTimeOffset time)
    {
        return new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Offset);
    }

    static DateTimeOffset SetCalendarHour(DateTimeOffset date, int hour)
    {
        var hourToSet = hour;
        if (hourToSet == 24)
            hourToSet = 0;

        var d = new DateTimeOffset(date.Year, date.Month, date.Day, hourToSet, date.Minute, date.Second, date.Millisecond, date.Offset);
        if (hour == 24)
            d = d.AddDays(1);

        return d;
    }

    static int GetLastDayOfMonth(int month, int year)
    {
        return DateTime.DaysInMonth(year, month);
    }

    static int ToInt32(char c)
    {
        return c - '0';
    }

    static int ToInt32(ReadOnlySpan<char> span)
    {
        return int.Parse(span, CultureInfo.InvariantCulture);
    }
}
