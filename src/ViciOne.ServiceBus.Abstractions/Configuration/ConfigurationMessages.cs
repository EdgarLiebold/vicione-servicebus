using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace ViciOne.ServiceBus.Providers.Configuration;

/// <summary>
/// Creates actionable configuration failures with one stable feature, bus, problem and correction shape.
/// Provider packages use this boundary so startup diagnostics remain consistent across transports and features.
/// </summary>
public static class ConfigurationMessages
{
    /// <summary>Creates one actionable configuration-failure line.</summary>
    public static string Create(string feature, string bus, string problem, string fix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        ArgumentException.ThrowIfNullOrWhiteSpace(problem);
        ArgumentException.ThrowIfNullOrWhiteSpace(fix);
        return $"{NormalizeSegment(feature)} for bus '{NormalizeSegment(bus)}': {Terminate(problem)} {Terminate(fix)}";
    }

    /// <summary>Combines previously formatted failure lines without losing their individual causes.</summary>
    public static string Aggregate(IEnumerable<string> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        return string.Join(Environment.NewLine, failures.Distinct(StringComparer.Ordinal));
    }

    static string Terminate(string value)
    {
        string normalized = NormalizeSegment(value);
        return normalized.EndsWith(".", StringComparison.Ordinal) ? normalized : normalized + ".";
    }

    static string NormalizeSegment(string value) => string.Join(
        " ",
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
