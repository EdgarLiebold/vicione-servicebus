using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.NewIdFormatters;
using ViciOne.ServiceBus.NewIdParsers;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Identifies a future instance by its durable endpoint address and correlation identifier.</summary>
public readonly struct FutureLocation
{
    /// <summary>Gets the endpoint address that owns the future.</summary>
    public Uri Address { get; }

    /// <summary>Gets the future correlation identifier.</summary>
    public Guid Id { get; }

    /// <summary>Parses a durable future location containing an encoded <c>id</c> query parameter.</summary>
    /// <param name="location">The absolute future location URI.</param>
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

    /// <summary>Creates a durable future location from its correlation identifier and owning endpoint.</summary>
    /// <param name="id">The future correlation identifier.</param>
    /// <param name="address">The absolute endpoint address that owns the future.</param>
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

    /// <summary>Creates a location URI that contains the future identifier.</summary>
    /// <param name="location">The future location to encode.</param>
    /// <returns>The durable endpoint URI with the encoded future identifier.</returns>
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
