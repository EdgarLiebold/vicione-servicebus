using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Util;

/// <summary>Provides utility operations for exception.</summary>
public static class ExceptionUtil
{
    static readonly Regex _trim;
    static readonly Regex _cleanup;

    static ExceptionUtil()
    {
        const string awaiter = @"at System\.Runtime\.CompilerServices\.TaskAwaiter.*";
        const string exception = @"at System\.Runtime\.ExceptionServices\.ExceptionDispatchInfo.*";
        const string dependency = @"at ViciOne.ServiceBus\.DependencyInjection.*";

        _cleanup = new Regex(@"\n\s+(" + string.Join("|", awaiter, exception, dependency) + ")+", RegexOptions.Multiline | RegexOptions.Compiled);
        _trim = new Regex(@"in\s.*ViciOne.ServiceBus.*\.cs.*$", RegexOptions.Multiline | RegexOptions.Compiled);
    }

    /// <summary>Gets message.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The message.</returns>
    public static string GetMessage(Exception exception)
    {
        try
        {
            var exceptionMessage = exception.Message ?? $"An exception of type {exception.GetType()} was thrown but the message was null.";

            if (exceptionMessage.Length > 2048)
                exceptionMessage = exceptionMessage.Substring(0, 2048);

            return exceptionMessage;
        }
        catch
        {
            return $"An exception of type {exception.GetType()} was thrown but the Message property threw an exception.";
        }
    }

    /// <summary>Gets stack trace.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The stack trace.</returns>
    public static string GetStackTrace(Exception? exception)
    {
        string? stackTrace;
        try
        {
            stackTrace = exception?.StackTrace;
        }
        catch
        {
            return "";
        }
        if (string.IsNullOrWhiteSpace(stackTrace))
            return "";

        stackTrace = _cleanup.Replace(stackTrace, "");
        stackTrace = _trim.Replace(stackTrace, "");

        if (stackTrace.Length > 2048)
            stackTrace = stackTrace.Substring(0, 2048);

        return stackTrace;
    }

    /// <summary>Gets exception header detail.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="adapter">The adapter.</param>
    /// <returns>The exception header detail.</returns>
    public static (Dictionary<string, object>, string) GetExceptionHeaderDetail(Exception exception, ITransportSetHeaderAdapter<object> adapter)
    {
        try
        {
            exception = exception.GetBaseException() ?? exception;
        }
        catch
        {
            // Fault reporting must preserve the original exception if its base lookup fails.
        }

        var exceptionMessage = GetMessage(exception);

        var dictionary = new Dictionary<string, object>();

        adapter.Set(dictionary, MessageHeaders.Reason, "fault");
        adapter.Set(dictionary, MessageHeaders.FaultExceptionType, TypeCache.GetShortName(exception.GetType()));
        adapter.Set(dictionary, MessageHeaders.FaultMessage, exceptionMessage);
        adapter.Set(dictionary, MessageHeaders.FaultStackTrace, GetStackTrace(exception));

        return (dictionary, exceptionMessage);
    }
}
