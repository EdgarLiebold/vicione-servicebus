using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Contains the payload and contract identities of a request outcome being forwarded.</summary>
internal sealed class ForwardedRequestOutcome
{
    readonly string[] _payloadTypes;

    /// <summary>Creates an outcome from a payload and its contract identities.</summary>
    /// <param name="payload">The payload being forwarded.</param>
    /// <param name="payloadTypes">The ordered payload contract identities.</param>
    /// <exception cref="ArgumentNullException"><paramref name="payload" /> or <paramref name="payloadTypes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="payloadTypes" /> is empty or contains a <see langword="null" /> entry.</exception>
    public ForwardedRequestOutcome(object payload, string[] payloadTypes)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        ArgumentNullException.ThrowIfNull(payloadTypes);

        if (payloadTypes.Length == 0)
            throw new ArgumentException("At least one payload contract identity is required.", nameof(payloadTypes));

        if (Array.IndexOf(payloadTypes, null) >= 0)
            throw new ArgumentException("Payload contract identities cannot contain null entries.", nameof(payloadTypes));

        _payloadTypes = (string[])payloadTypes.Clone();
    }

    /// <summary>Gets the forwarded payload.</summary>
    public object Payload { get; }

    /// <summary>Gets an independent snapshot of the ordered payload contract identities.</summary>
    public string[] PayloadTypes => (string[])_payloadTypes.Clone();
}
