using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Immutable messages observed while one <c>Act</c> operation and its resulting activity chain were active.</summary>
public sealed record ActiveTestResult
{
    /// <summary>Creates an immutable activity snapshot.</summary>
    /// <param name="consumed">The consumed-message observations.</param>
    /// <param name="published">The published-message observations.</param>
    /// <param name="sent">The sent-message observations.</param>
    public ActiveTestResult(
        IEnumerable<IConsumedMessage> consumed,
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

    /// <summary>Gets messages consumed during the captured activity.</summary>
    public IReadOnlyList<IConsumedMessage> Consumed { get; }

    /// <summary>Gets messages published during the captured activity.</summary>
    public IReadOnlyList<IPublishedMessage> Published { get; }

    /// <summary>Gets messages sent during the captured activity.</summary>
    public IReadOnlyList<ISentMessage> Sent { get; }
}
