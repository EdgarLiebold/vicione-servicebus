#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a fault exception info implementation.
/// </summary>
public sealed class FaultExceptionInfo : ExceptionInfo
{
    const int MaximumDataCount = 32;
    const int MaximumInnerExceptionCount = 16;
    const int MaximumKeyLength = 256;
    const int MaximumTextLength = 2048;


    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public FaultExceptionInfo()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public FaultExceptionInfo(Exception exception)
        : this(exception, 0)
    {
    }

    FaultExceptionInfo(Exception exception, int depth)
    {
        ArgumentNullException.ThrowIfNull(exception);

        IDictionary? primaryData = GetData(exception);
        Exception reportedException = exception;
        IDictionary? fallbackData = null;

        // The application wrapper adds diagnostic data without replacing the exception identity
        // reported to consumers. Wrapper values win when the wrapped exception contains the same
        // key, which lets an application deliberately refine the diagnostic context.
        if (exception is ViciOneServiceBusApplicationException { InnerException: { } innerException })
        {
            reportedException = innerException;
            fallbackData = GetData(innerException);
        }

        Data = SnapshotData(primaryData, fallbackData);
        ExceptionType = Limit(GetExceptionType(reportedException), MaximumTextLength);
        InnerException = depth < MaximumInnerExceptionCount - 1 && GetInnerException(reportedException) is { } nestedException
            ? new FaultExceptionInfo(nestedException, depth + 1)
            : null;
        StackTrace = Limit(GetStackTrace(reportedException), MaximumTextLength);
        Message = Limit(GetMessage(reportedException), MaximumTextLength);
        Source = Limit(GetSource(reportedException), MaximumTextLength);
    }

    /// <summary>
    /// Gets or sets the exception type value.
    /// </summary>
    public string ExceptionType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the inner exception value.
    /// </summary>
    public ExceptionInfo? InnerException { get; set; }

    /// <summary>
    /// Gets or sets the stack trace value.
    /// </summary>
    public string StackTrace { get; set; } = null!;

    /// <summary>
    /// Gets or sets the message value.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the source value.
    /// </summary>
    public string Source { get; set; } = null!;

    /// <summary>
    /// Gets or sets the data value.
    /// </summary>
    public IDictionary<string, object>? Data { get; set; }

    static IDictionary? GetData(Exception exception)
    {
        try
        {
            return exception.Data;
        }
        catch
        {
            return null;
        }
    }

    static Exception? GetInnerException(Exception exception)
    {
        try
        {
            return exception.InnerException;
        }
        catch
        {
            return null;
        }
    }

    static string GetExceptionType(Exception exception)
    {
        try
        {
            if (exception is ExceptionInfoException infoException)
                return infoException.ExceptionInfo.ExceptionType ?? TypeCache.GetShortName(exception.GetType());
        }
        catch
        {
        }

        return TypeCache.GetShortName(exception.GetType());
    }

    static string GetMessage(Exception exception)
    {
        try
        {
            return ExceptionUtil.GetMessage(exception);
        }
        catch
        {
            return $"An exception of type {exception.GetType()} was thrown but its message could not be read.";
        }
    }

    static string GetStackTrace(Exception exception)
    {
        try
        {
            return ExceptionUtil.GetStackTrace(exception);
        }
        catch
        {
            return string.Empty;
        }
    }

    static string GetSource(Exception exception)
    {
        try
        {
            return exception.Source ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    static IDictionary<string, object>? SnapshotData(IDictionary? primary, IDictionary? fallback)
    {
        Dictionary<string, object>? snapshot = null;
        var remaining = MaximumDataCount;

        if (primary is not null)
            AddEntries(primary, ref snapshot, ref remaining);
        if (remaining > 0 && fallback is not null && !ReferenceEquals(primary, fallback))
            AddEntries(fallback, ref snapshot, ref remaining);

        return snapshot;
    }

    static void AddEntries(IDictionary source, ref Dictionary<string, object>? destination, ref int remaining)
    {
        try
        {
            foreach (DictionaryEntry entry in source)
            {
                if (remaining == 0)
                    return;
                if (entry.Key is not string key || entry.Value is null)
                    continue;

                key = Limit(key, MaximumKeyLength);
                destination ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (destination.TryAdd(key, NormalizeDiagnosticValue(entry.Value)))
                    remaining--;
            }
        }
        catch
        {
            // Exception diagnostics are observational and must not replace the original failure.
        }
    }

    static object NormalizeDiagnosticValue(object value)
    {
        return value switch
        {
            string text => Limit(text, MaximumTextLength),
            bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or char
                or Guid or DateTime or DateTimeOffset or TimeSpan => value,
            Enum => Limit(value.ToString() ?? value.GetType().Name, MaximumTextLength),
            _ => Limit(value.GetType().FullName ?? value.GetType().Name, MaximumTextLength)
        };
    }

    static string Limit(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }
}
