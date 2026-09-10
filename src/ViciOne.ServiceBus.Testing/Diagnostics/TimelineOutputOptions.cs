using System;
using System.Linq;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Configures the columns, type names, and completion behavior of a test-harness timeline.</summary>
public sealed class TimelineOutputOptions
{
    bool _includeEndpointAddress;
    bool _includeMessageNamespace;
    bool _renderImmediately;

    /// <summary>Includes the namespace in each rendered message type name.</summary>
    /// <returns>This options instance.</returns>
    public TimelineOutputOptions IncludeMessageNamespace()
    {
        _includeMessageNamespace = true;
        return this;
    }

    /// <summary>Includes a column containing each destination or input endpoint address.</summary>
    /// <returns>This options instance.</returns>
    public TimelineOutputOptions IncludeEndpointAddress()
    {
        _includeEndpointAddress = true;
        return this;
    }

    /// <summary>Renders current observations immediately instead of waiting for the harness to become inactive.</summary>
    /// <returns>This options instance.</returns>
    public TimelineOutputOptions RenderImmediately()
    {
        _renderImmediately = true;
        return this;
    }

    internal void Apply(IBaseTestHarness harness)
    {
        if (_renderImmediately)
            harness.ForceInactive();
    }

    internal string GetMessageTypeName(TimelineMessage message)
    {
        return _includeMessageNamespace
            ? message.ShortTypeName
            : GetTypeName(message.MessageType);
    }

    internal TextTable CreateTable(ChartTable chart)
    {
        return _includeEndpointAddress
            ? TextTable.Create(chart.GetRows().Select(row => new
            {
                Operation = row.Title,
                row.Duration,
                row.Timeline,
                Address = row.GetColumn(0)
            }))
            : TextTable.Create(chart.GetRows());
    }

    static string GetTypeName(Type type)
    {
        string name = type.Name;

        if (!type.IsGenericType)
            return name;

        int genericMarker = name.LastIndexOf('`');
        if (genericMarker >= 0)
            name = name[..genericMarker];

        return $"{name}<{string.Join(",", type.GetGenericArguments().Select(GetTypeName))}>";
    }
}
