namespace ViciOne.ServiceBus.Testing;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Immutable messages observed while one <c>Act</c> operation and its resulting activity chain were active.
/// </summary>
public sealed record ActiveTestResult
{
    public ActiveTestResult(
        IEnumerable<IReceivedMessage> consumed,
        IEnumerable<IPublishedMessage> published,
        IEnumerable<ISentMessage> sent)
    {
        ArgumentNullException.ThrowIfNull(consumed);
        ArgumentNullException.ThrowIfNull(published);
        ArgumentNullException.ThrowIfNull(sent);

        Consumed = Array.AsReadOnly(consumed.ToArray());
        Published = Array.AsReadOnly(published.ToArray());
        Sent = Array.AsReadOnly(sent.ToArray());
    }

    public IReadOnlyList<IReceivedMessage> Consumed { get; }

    public IReadOnlyList<IPublishedMessage> Published { get; }

    public IReadOnlyList<ISentMessage> Sent { get; }
}
