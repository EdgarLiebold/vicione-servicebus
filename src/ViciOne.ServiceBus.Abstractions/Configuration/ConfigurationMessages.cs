using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Providers.Configuration;

/// <summary>
/// Creates actionable configuration failures with one stable feature, bus, problem and correction shape.
/// Provider packages use this boundary so startup diagnostics remain consistent across transports and features.
/// </summary>
public static class ConfigurationMessages
{
    /// <summary>Creates one actionable configuration-failure line.</summary>
    /// <param name="feature">The feature whose configuration failed.</param>
    /// <param name="bus">The affected bus identity.</param>
    /// <param name="problem">A description of the invalid configuration.</param>
    /// <param name="fix">The action required to correct the configuration.</param>
    /// <returns>The normalized configuration-failure message.</returns>
    public static string Create(string feature, string bus, string problem, string fix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        ArgumentException.ThrowIfNullOrWhiteSpace(problem);
        ArgumentException.ThrowIfNullOrWhiteSpace(fix);
        return $"{NormalizeSegment(feature)} for bus '{NormalizeSegment(bus)}': {Terminate(problem)} {Terminate(fix)}";
    }

    /// <summary>Combines previously formatted failure lines without losing their individual causes.</summary>
    /// <param name="failures">The non-empty failure lines to combine.</param>
    /// <returns>The distinct failure lines in first-occurrence order.</returns>
    public static string Aggregate(IEnumerable<string> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        var uniqueFailures = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string failure in failures)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(failure);
            if (seen.Add(failure))
                uniqueFailures.Add(failure);
        }

        return string.Join(Environment.NewLine, uniqueFailures);
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
