using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util;

/// <summary>Represents a row of chart data.</summary>
public class ChartRow
{
    readonly object[] _columns;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="title">The title.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="timeline">The timeline.</param>
    /// <param name="columns">The columns.</param>
    public ChartRow(string title, string duration, string timeline, object[] columns)
    {
        _columns = columns;
        Title = title;
        Duration = duration;
        Timeline = timeline;
    }

    /// <summary>Gets the title.</summary>
    public string Title { get; }
    /// <summary>Gets the duration.</summary>
    public string Duration { get; }
    /// <summary>Gets the timeline.</summary>
    public string Timeline { get; }

    /// <summary>Gets column.</summary>
    /// <param name="column">The column.</param>
    /// <returns>The column.</returns>
    public object GetColumn(int column)
    {
        if (_columns == null || column < 0 || column >= _columns.Length)
            throw new ArgumentOutOfRangeException(nameof(column));

        return _columns[column];
    }
}
