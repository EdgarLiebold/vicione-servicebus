using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Testing;
/// <summary>Immutable messages observed while one <c>Act</c> operation and its resulting activity chain were active.</summary>
public sealed record ActiveTestResult
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumed">The consumed.</param>
    /// <param name="published">The published.</param>
    /// <param name="sent">The sent.</param>
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

    /// <summary>Gets the consumed.</summary>
    public IReadOnlyList<IReceivedMessage> Consumed { get; }

    /// <summary>Gets the published.</summary>
    public IReadOnlyList<IPublishedMessage> Published { get; }

    /// <summary>Gets the sent.</summary>
    public IReadOnlyList<ISentMessage> Sent { get; }
}
