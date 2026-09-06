using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides a chart row implementation.
/// </summary>
public class ChartRow
{
    readonly object[] _columns;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="title">The title value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="timeline">The timeline value.</param>
    /// <param name="columns">The columns value.</param>
    public ChartRow(string title, string duration, string timeline, object[] columns)
    {
        _columns = columns;
        Title = title;
        Duration = duration;
        Timeline = timeline;
    }

    /// <summary>
    /// Gets the title value.
    /// </summary>
    public string Title { get; }
    /// <summary>
    /// Gets the duration value.
    /// </summary>
    public string Duration { get; }
    /// <summary>
    /// Gets the timeline value.
    /// </summary>
    public string Timeline { get; }

    /// <summary>
    /// Gets column.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the operation.</returns>
    public object GetColumn(int column)
    {
        if (_columns == null || column < 0 || column >= _columns.Length)
            throw new ArgumentOutOfRangeException(nameof(column));

        return _columns[column];
    }
}
