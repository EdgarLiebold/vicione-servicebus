using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides a transport receive context together with the exception that interrupted its pipeline.</summary>
public interface ExceptionReceiveContext :
    ReceiveContext
{
    /// <summary>Gets the exception that interrupted transport receive processing.</summary>
    Exception Exception { get; }

    /// <summary>Gets the timestamp at which the exception was observed.</summary>
    DateTimeOffset ExceptionTimestamp { get; }

    /// <summary>Gets the serializable exception details included in fault messages.</summary>
    ExceptionInfo ExceptionInfo { get; }

    /// <summary>Gets the headers added when the transport message is moved to its error queue.</summary>
    SendHeaders ExceptionHeaders { get; }
}
