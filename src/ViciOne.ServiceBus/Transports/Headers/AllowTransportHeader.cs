namespace ViciOne.ServiceBus.Transports;

/// <summary>Determines whether a string header may be copied to a transport message.</summary>
/// <param name="headerValue">The named header value being considered.</param>
/// <returns><see langword="true" /> to include the header; otherwise, <see langword="false" />.</returns>
public delegate bool AllowTransportHeader(HeaderValue<string> headerValue);
