using Microsoft.CodeAnalysis;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;

/// <summary>An analyzer diagnostic reduced to its stable externally observable contract.</summary>
public sealed record DiagnosticObservation(
    string Id,
    DiagnosticSeverity Severity,
    string Message,
    string Path,
    int Line,
    int Column)
{
    internal static DiagnosticObservation From(Diagnostic diagnostic)
    {
        if (diagnostic.Location == Location.None)
        {
            return new DiagnosticObservation(
                diagnostic.Id,
                diagnostic.Severity,
                diagnostic.GetMessage(),
                string.Empty,
                -1,
                -1);
        }

        var lineSpan = diagnostic.Location.GetLineSpan();
        var position = lineSpan.StartLinePosition;

        return new DiagnosticObservation(
            diagnostic.Id,
            diagnostic.Severity,
            diagnostic.GetMessage(),
            lineSpan.Path,
            position.Line + 1,
            position.Character + 1);
    }
}
