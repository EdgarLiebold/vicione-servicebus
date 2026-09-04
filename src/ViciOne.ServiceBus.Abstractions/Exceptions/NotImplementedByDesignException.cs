using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to not implemented by design.
/// </summary>
[Serializable]
public class NotImplementedByDesignException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public NotImplementedByDesignException()
        : this("This method has not been implemented by design.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public NotImplementedByDesignException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public NotImplementedByDesignException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
