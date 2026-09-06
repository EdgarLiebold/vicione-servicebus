namespace ViciOne.ServiceBus.Transports;

/// <summary>Represents the method that handles allow transport header.</summary>
/// <param name="headerValue">The header value to convert or store.</param>
/// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool AllowTransportHeader(HeaderValue<string> headerValue);
