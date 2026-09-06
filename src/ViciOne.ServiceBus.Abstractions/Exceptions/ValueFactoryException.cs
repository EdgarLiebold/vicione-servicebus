using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to value factory.
/// </summary>
public class ValueFactoryException :
    Exception
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ValueFactoryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ValueFactoryException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ValueFactoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
