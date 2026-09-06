using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.NewIdFormatters;
using ViciOne.ServiceBus.NewIdParsers;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Represents a future location.</summary>
public readonly struct FutureLocation
{
    /// <summary>Exposes the address used by the containing type.</summary>
    public readonly Uri Address;
    /// <summary>Exposes the id used by the containing type.</summary>
    public readonly Guid Id;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="location">The location.</param>
    public FutureLocation(Uri location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (!location.IsAbsoluteUri)
            throw CreateInvalidLocationException(location);

        string? value;
        bool hasValue;
        try
        {
            hasValue = location.TryGetValueFromQueryString("id", out value);
        }
        catch (InvalidOperationException exception)
        {
            throw CreateInvalidLocationException(location, exception);
        }

        if (!hasValue || string.IsNullOrWhiteSpace(value))
            throw CreateInvalidLocationException(location);

        try
        {
            var parsedId = IdParser.Parse(value);
            Id = parsedId.ToGuid();
        }
        catch (ArgumentException exception)
        {
            throw CreateInvalidLocationException(location, exception);
        }

        Address = new Uri(location.GetLeftPart(UriPartial.Path));
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="id">The id.</param>
    /// <param name="address">The address.</param>
    public FutureLocation(Guid id, Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
            throw new ArgumentException("Address must be an absolute URI.", nameof(address));

        var endpointName = address.GetEndpointName();
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new ArgumentException("Address must contain an endpoint name.", nameof(address));

        Id = id;
        Address = new Uri($"queue:{endpointName}");
    }

    /// <summary>Converts a value to <see cref="Uri" />.</summary>
    /// <param name="location">The location.</param>
    /// <returns>The value produced by the operation.</returns>
    public static implicit operator Uri(FutureLocation location)
    {
        var newId = location.Id.ToNewId();
        var id = newId.ToString(IdFormatter);

        return new UriBuilder(location.Address) { Query = $"id={id}" }.Uri;
    }

    static readonly INewIdFormatter IdFormatter = new ZBase32Formatter();
    static readonly INewIdParser IdParser = new ZBase32Parser(true);

    static FormatException CreateInvalidLocationException(Uri location, Exception? innerException = null)
    {
        var message = $"Location format invalid: {location}";
        return innerException == null
            ? new FormatException(message)
            : new FormatException(message, innerException);
    }
}
