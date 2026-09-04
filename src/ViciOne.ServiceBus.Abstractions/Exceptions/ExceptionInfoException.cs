using System;
using System.Collections;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to exception info.
/// </summary>
[Serializable]
public class ExceptionInfoException :
    ViciOneServiceBusException
{
    readonly IDictionary? _data;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exceptionInfo">The exception info value.</param>
    public ExceptionInfoException(ExceptionInfo exceptionInfo)
        : base(exceptionInfo.Message, exceptionInfo.InnerException != null ? new ExceptionInfoException(exceptionInfo.InnerException) : default)
    {
        ExceptionInfo = exceptionInfo;
        if (ExceptionInfo.Data != null)
            _data = (IDictionary)ExceptionInfo.Data;
    }

    /// <summary>
    /// Gets the exception info value.
    /// </summary>
    public ExceptionInfo ExceptionInfo { get; }

    /// <summary>
    /// Gets the stack trace value.
    /// </summary>
    public override string StackTrace => ExceptionInfo.StackTrace;
    /// <summary>
    /// Gets the source value.
    /// </summary>
    public override string Source => ExceptionInfo.Source;

    /// <summary>
    /// Gets the data value.
    /// </summary>
    public override IDictionary Data => _data ?? base.Data;
}
