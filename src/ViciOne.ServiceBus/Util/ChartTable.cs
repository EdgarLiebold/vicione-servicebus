using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides a chart table implementation.
/// </summary>
public class ChartTable
{
    readonly int _chartWidth;
    readonly List<Line> _lines;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="chartWidth">The chart width value.</param>
    public ChartTable(int chartWidth = 60)
    {
        _chartWidth = chartWidth;
        _lines = new List<Line>();
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="startTime">The start time value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="columns">The columns value.</param>
    /// <returns>The result of the operation.</returns>
    public ChartTable Add(string text, DateTimeOffset startTime, TimeSpan? duration, params object[] columns)
    {
        _lines.Add(new Line(text, startTime, duration, columns));

        return this;
    }

    /// <summary>
    /// Gets rows.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ChartRow> GetRows()
    {
        if (_lines.Count == 0)
            yield break;

        var (low, high) = CalculateRange();
        var totalDuration = high - low;

        foreach (var line in _lines)
        {
            var offset = (int)(_chartWidth * ((line.StartTime - low).TotalMilliseconds / totalDuration.TotalMilliseconds));
            var length = (int)Math.Max(_chartWidth * (line.Duration.TotalMilliseconds / totalDuration.TotalMilliseconds), 1);

            var bar = new string(' ', offset) + (length > 1 ? '\x2590' : '\x258D');
            if (length > 2)
                bar += new string('\x2592', length - 2);
            if (length > 1)
                bar += '\x258D';

            yield return new ChartRow(line.Text, line.Duration.ToFriendlyString(), bar, line.Columns);
        }
    }

    /// <summary>
    /// Performs the calculate range operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public (DateTimeOffset low, DateTimeOffset high) CalculateRange()
    {
        var low = _lines.Min(x => x.StartTime);
        var high = _lines.Max(x => x.EndTime);

        return (low, high);
    }


    /// <summary>
    /// Provides a line implementation.
    /// </summary>
    public class Line
    {
        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="startTime">The start time value.</param>
        /// <param name="duration">The duration value.</param>
        /// <param name="columns">The columns value.</param>
        public Line(string text, DateTimeOffset startTime, TimeSpan? duration, object[] columns)
        {
            Text = text;
            StartTime = startTime;
            Columns = columns;
            Duration = duration ?? new TimeSpan(1);
        }

        /// <summary>
        /// Gets the text value.
        /// </summary>
        public string Text { get; }
        /// <summary>
        /// Gets the start time value.
        /// </summary>
        public DateTimeOffset StartTime { get; }
        /// <summary>
        /// Gets the duration value.
        /// </summary>
        public TimeSpan Duration { get; }
        /// <summary>
        /// Gets the columns value.
        /// </summary>
        public object[] Columns { get; }

        /// <summary>
        /// Gets the end time value.
        /// </summary>
        public DateTimeOffset EndTime => StartTime + Duration;
    }
}
