using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Contains the payload and contract identities of a request outcome being forwarded.</summary>
internal sealed class ForwardedRequestOutcome
{
    public ForwardedRequestOutcome(object payload, string[] payloadTypes)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        PayloadTypes = payloadTypes ?? throw new ArgumentNullException(nameof(payloadTypes));

        if (payloadTypes.Length == 0)
            throw new ArgumentException("At least one payload contract identity is required.", nameof(payloadTypes));
    }

    public object Payload { get; }

    public string[] PayloadTypes { get; }
}
