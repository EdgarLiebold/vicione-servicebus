using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>Describes a serialization-safe snapshot of an exception.</summary>
public interface ExceptionInfo
{
    /// <summary>Gets the reported exception type name.</summary>
    string ExceptionType { get; }

    /// <summary>Gets the next exception in the bounded inner-exception chain, when present.</summary>
    ExceptionInfo? InnerException { get; }

    /// <summary>Gets the captured stack trace.</summary>
    string StackTrace { get; }

    /// <summary>Gets the captured exception message.</summary>
    string Message { get; }

    /// <summary>Gets the captured exception source.</summary>
    string Source { get; }

    /// <summary>Gets the serialization-safe diagnostic entries, when any were captured.</summary>
    IDictionary<string, object>? Data { get; }
}
