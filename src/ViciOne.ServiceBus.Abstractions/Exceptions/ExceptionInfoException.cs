using System;
using System.Collections;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to exception info.</summary>
public class ExceptionInfoException :
    ViciOneServiceBusException
{
    readonly IDictionary? _data;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="exceptionInfo">The exception info.</param>
    public ExceptionInfoException(ExceptionInfo exceptionInfo)
        : base(exceptionInfo.Message, exceptionInfo.InnerException != null ? new ExceptionInfoException(exceptionInfo.InnerException) : default)
    {
        ExceptionInfo = exceptionInfo;
        if (ExceptionInfo.Data != null)
            _data = (IDictionary)ExceptionInfo.Data;
    }

    /// <summary>Gets the exception info.</summary>
    public ExceptionInfo ExceptionInfo { get; }

    /// <summary>Gets the stack trace.</summary>
    public override string StackTrace => ExceptionInfo.StackTrace;
    /// <summary>Gets the source.</summary>
    public override string Source => ExceptionInfo.Source;

    /// <summary>Gets the data.</summary>
    public override IDictionary Data => _data ?? base.Data;
}
