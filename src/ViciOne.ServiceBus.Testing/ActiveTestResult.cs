using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Testing;
/// <summary>
/// Immutable messages observed while one <c>Act</c> operation and its resulting activity chain were active.
/// </summary>
public sealed record ActiveTestResult
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumed">The consumed value.</param>
    /// <param name="published">The published value.</param>
    /// <param name="sent">The sent value.</param>
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

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public IReadOnlyList<IReceivedMessage> Consumed { get; }

    /// <summary>
    /// Gets the published value.
    /// </summary>
    public IReadOnlyList<IPublishedMessage> Published { get; }

    /// <summary>
    /// Gets the sent value.
    /// </summary>
    public IReadOnlyList<ISentMessage> Sent { get; }
}
