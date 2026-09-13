using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Renders the messages observed by a test harness as a chronological conversation tree.</summary>
public static class TimelineExtensions
{
    /// <summary>Writes the published, sent, and consumed messages observed by a test harness as a timeline.</summary>
    /// <param name="harness">The harness whose observations are rendered.</param>
    /// <param name="textWriter">The destination for the rendered timeline.</param>
    /// <param name="configure">An optional action that configures the output.</param>
    /// <param name="cancellationToken">The token that cancels observation and rendering.</param>
    /// <returns>A task that represents timeline generation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static async Task OutputTimelineAsync(this IBaseTestHarness harness, TextWriter textWriter,
        Action<TimelineOutputOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(textWriter);
        cancellationToken.ThrowIfCancellationRequested();

        var options = new TimelineOutputOptions();
        configure?.Invoke(options);

        options.Apply(harness);

        await harness.InactivityTask.WaitAsync(cancellationToken).ConfigureAwait(false);

        var produced = new List<TimelineMessage>();

        await foreach (var message in harness.Published.SelectAsync(_ => true, cancellationToken: cancellationToken).ConfigureAwait(false))
            produced.Add(new TimelineMessage(message));

        await foreach (var message in harness.Sent.SelectAsync(_ => true, cancellationToken: cancellationToken).ConfigureAwait(false))
            produced.Add(new TimelineMessage(message));

        var consumed = new List<TimelineMessage>();

        await foreach (var message in harness.Consumed.SelectAsync(_ => true, cancellationToken: cancellationToken).ConfigureAwait(false))
            consumed.Add(new TimelineMessage(message));

        List<ConversationThread> conversations = produced.GroupBy(message => message.ConversationKey).SelectMany(group =>
        {
            List<TimelineMessage> messages = group.OrderBy(message => message.StartTime).ToList();
            var roots = new List<ConversationThread>();
            var visited = new HashSet<TimelineMessage>();

            IEnumerable<TimelineMessage> rootCandidates = messages.Where(message => message.ParentMessageId == null).Concat(messages);
            foreach (TimelineMessage initiator in rootCandidates)
            {
                if (!visited.Add(initiator))
                    continue;

                var initiatorThread = new ConversationThread(initiator, 1);
                roots.Add(initiatorThread);

                var stack = new Stack<ConversationThread>();
                stack.Push(initiatorThread);

                while (stack.Any())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var thread = stack.Pop();

                    List<TimelineMessage> consumes = consumed.Where(message => message.MessageId == thread.Message.MessageId).ToList();
                    thread.Consumers.AddRange(consumes.Select(message => new ConversationConsumer(message)));

                    IEnumerable<TimelineMessage> threadMessages = messages.Where(
                        message => message.ParentMessageId == thread.Message.MessageId);
                    foreach (TimelineMessage message in threadMessages)
                    {
                        if (!visited.Add(message))
                            continue;

                        var nextThread = new ConversationThread(message, thread.Depth + 1);
                        thread.Nodes.Add(nextThread);
                        stack.Push(nextThread);
                    }
                }
            }

            return roots;
        }).ToList();

        var chart = new ChartTable();

        foreach (var conversation in conversations.OrderBy(x => x.Message.StartTime))
        {
            var whitespace = new string(' ', (conversation.Depth - 1) * 2);
            var conversationLine = $"{whitespace}{conversation.Message.EventType} {options.GetMessageTypeName(conversation.Message)}";

            chart.Add(conversationLine, conversation.Message.StartTime, conversation.Message.ElapsedTime, conversation.Message.Address ?? string.Empty);

            AddConsumers(conversation, chart, options);

            var stack = new Stack<ConversationThread>(conversation.Nodes.OrderByDescending(x => x.Message.StartTime));
            while (stack.Any())
            {
                var current = stack.Pop();

                whitespace = new string(' ', (current.Depth - 1) * 2);
                var line = $"{whitespace}{current.Message.EventType} {options.GetMessageTypeName(current.Message)}";

                chart.Add(line, current.Message.StartTime, current.Message.ElapsedTime, current.Message.Address ?? string.Empty);

                AddConsumers(current, chart, options);

                foreach (var node in current.Nodes.OrderByDescending(x => x.Message.StartTime))
                    stack.Push(node);
            }
        }

        options.CreateTable(chart)
            .SetColumn(1, "Duration", typeof(int))
            .SetRightNumberAlignment()
            .OutputTo(textWriter)
            .Write();
    }

    static void AddConsumers(ConversationThread conversation, ChartTable chart, TimelineOutputOptions options)
    {
        foreach (var consumer in conversation.Consumers.OrderBy(x => x.Message.StartTime))
        {
            var sb = new StringBuilder(60);
            if (conversation.Depth > 1)
                sb.Append(' ', (conversation.Depth - 1) * 2);
            if (conversation.Depth > 0)
                sb.Append("\x2514 ");

            sb.Append("Consume ");
            sb.Append(options.GetMessageTypeName(consumer.Message));

            chart.Add(sb.ToString(), consumer.Message.StartTime, consumer.Message.ElapsedTime, consumer.Message.Address ?? string.Empty);
        }
    }
    sealed class ConversationConsumer
    {
        public ConversationConsumer(TimelineMessage message)
        {
            Message = message;
        }

        public TimelineMessage Message { get; }
    }


    sealed class ConversationThread
    {
        public ConversationThread(TimelineMessage message, int depth)
        {
            Message = message;
            Depth = depth;
            Nodes = new List<ConversationThread>();
            Consumers = new List<ConversationConsumer>();
        }

        public TimelineMessage Message { get; }
        public int Depth { get; }
        public List<ConversationConsumer> Consumers { get; }
        public List<ConversationThread> Nodes { get; }
    }
}
