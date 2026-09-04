using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util;

public class ChartTable
{
    readonly int _chartWidth;
    readonly List<Line> _lines;

    public ChartTable(int chartWidth = 60)
    {
        _chartWidth = chartWidth;
        _lines = new List<Line>();
    }

    public ChartTable Add(string text, DateTimeOffset startTime, TimeSpan? duration, params object[] columns)
    {
        _lines.Add(new Line(text, startTime, duration, columns));

        return this;
    }

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

    public (DateTimeOffset low, DateTimeOffset high) CalculateRange()
    {
        var low = _lines.Min(x => x.StartTime);
        var high = _lines.Max(x => x.EndTime);

        return (low, high);
    }


    public class Line
    {
        public Line(string text, DateTimeOffset startTime, TimeSpan? duration, object[] columns)
        {
            Text = text;
            StartTime = startTime;
            Columns = columns;
            Duration = duration ?? new TimeSpan(1);
        }

        public string Text { get; }
        public DateTimeOffset StartTime { get; }
        public TimeSpan Duration { get; }
        public object[] Columns { get; }

        public DateTimeOffset EndTime => StartTime + Duration;
    }
}


public class ChartRow
{
    readonly object[] _columns;

    public ChartRow(string title, string duration, string timeline, object[] columns)
    {
        _columns = columns;
        Title = title;
        Duration = duration;
        Timeline = timeline;
    }

    public string Title { get; }
    public string Duration { get; }
    public string Timeline { get; }

    public object GetColumn(int column)
    {
        if (_columns == null || column < 0 || column >= _columns.Length)
            throw new ArgumentOutOfRangeException(nameof(column));

        return _columns[column];
    }
}
