#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Events;

[Serializable]
public sealed class FaultExceptionInfo : ExceptionInfo
{
    public FaultExceptionInfo()
    {
    }

    public FaultExceptionInfo(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        IDictionary primaryData = exception.Data;
        Exception reportedException = exception;
        IDictionary? fallbackData = null;

        // The application wrapper adds diagnostic data without replacing the exception identity
        // reported to consumers. Wrapper values win when the wrapped exception contains the same
        // key, which lets an application deliberately refine the diagnostic context.
        if (exception is ViciOneServiceBusApplicationException { InnerException: { } innerException })
        {
            reportedException = innerException;
            fallbackData = innerException.Data;
        }

        Data = SnapshotData(primaryData, fallbackData);
        ExceptionType = reportedException is ExceptionInfoException infoException
            ? infoException.ExceptionInfo.ExceptionType
            : TypeCache.GetShortName(reportedException.GetType());
        InnerException = reportedException.InnerException is { } nestedException
            ? new FaultExceptionInfo(nestedException)
            : null;
        StackTrace = ExceptionUtil.GetStackTrace(reportedException);
        Message = ExceptionUtil.GetMessage(reportedException);
        Source = reportedException.Source ?? string.Empty;
    }

    public string ExceptionType { get; set; } = null!;

    public ExceptionInfo? InnerException { get; set; }

    public string StackTrace { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Source { get; set; } = null!;

    public IDictionary<string, object>? Data { get; set; }

    private static IDictionary<string, object>? SnapshotData(IDictionary primary, IDictionary? fallback)
    {
        Dictionary<string, object>? snapshot = null;

        AddEntries(primary, ref snapshot);
        if (fallback is not null && !ReferenceEquals(primary, fallback))
            AddEntries(fallback, ref snapshot);

        return snapshot;
    }

    private static void AddEntries(IDictionary source, ref Dictionary<string, object>? destination)
    {
        foreach (DictionaryEntry entry in source)
        {
            if (entry.Key is not string key || entry.Value is null)
                continue;

            destination ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            destination.TryAdd(key, entry.Value);
        }
    }
}
