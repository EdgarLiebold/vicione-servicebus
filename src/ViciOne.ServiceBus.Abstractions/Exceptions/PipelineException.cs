using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to pipeline.</summary>
public class PipelineException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public PipelineException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public PipelineException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public PipelineException(string message, Exception innerException)
        :
        base(message, innerException)
    {
    }
}
