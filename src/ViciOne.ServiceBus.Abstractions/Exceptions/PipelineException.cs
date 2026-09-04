using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to pipeline.
/// </summary>
[Serializable]
public class PipelineException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PipelineException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public PipelineException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public PipelineException(string message, Exception innerException)
        :
        base(message, innerException)
    {
    }
}
