using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>Rehydrates a transport-safe exception snapshot as a local exception chain.</summary>
public class ExceptionInfoException :
    ViciOneServiceBusException
{
    readonly IDictionary? _data;

    /// <summary>Creates a local exception that exposes the supplied remote diagnostic snapshot.</summary>
    /// <param name="exceptionInfo">The remote exception snapshot to expose.</param>
    public ExceptionInfoException(ExceptionInfo exceptionInfo)
        : base(
            (exceptionInfo ?? throw new ArgumentNullException(nameof(exceptionInfo))).Message,
            exceptionInfo.InnerException != null ? new ExceptionInfoException(exceptionInfo.InnerException) : default)
    {
        ExceptionInfo = exceptionInfo;
        if (ExceptionInfo.Data != null)
            _data = new Dictionary<string, object>(ExceptionInfo.Data, StringComparer.Ordinal);
    }

    /// <summary>Gets the remote exception snapshot represented by this exception.</summary>
    public ExceptionInfo ExceptionInfo { get; }

    /// <summary>Gets the captured remote stack trace.</summary>
    public override string StackTrace => ExceptionInfo.StackTrace;

    /// <summary>Gets the captured remote exception source.</summary>
    public override string Source => ExceptionInfo.Source;

    /// <summary>Gets the captured remote diagnostic entries.</summary>
    public override IDictionary Data => _data ?? base.Data;
}
