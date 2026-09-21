using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that no durable future instance matches a requested identifier.</summary>
public sealed class FutureNotFoundException :
    ViciOneServiceBusException
{
    /// <summary>Creates a future-not-found exception without typed future context.</summary>
    public FutureNotFoundException()
    {
    }

    /// <summary>Creates an exception for the specified missing future instance.</summary>
    /// <param name="type">The runtime type of the future or command.</param>
    /// <param name="id">The identifier used to locate the future instance.</param>
    public FutureNotFoundException(Type type, Guid id)
        : base(FormatMessage(type, id))
    {
        FutureType = type;
        FutureId = id;
    }

    /// <summary>Creates a future-not-found exception with the specified failure message.</summary>
    /// <param name="message">The description of the missing future.</param>
    public FutureNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a future-not-found exception with an underlying failure.</summary>
    /// <param name="message">The description of the missing future.</param>
    /// <param name="innerException">The exception raised while locating the future.</param>
    public FutureNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Gets the runtime type of the missing future or command, when one was supplied.</summary>
    public Type? FutureType { get; }

    /// <summary>Gets the identifier used to locate the missing future, when one was supplied.</summary>
    public Guid? FutureId { get; }

    static string FormatMessage(Type type, Guid id)
    {
        ArgumentNullException.ThrowIfNull(type);

        return $"Future {TypeCache.GetShortName(type)}({id}) not found";
    }
}
