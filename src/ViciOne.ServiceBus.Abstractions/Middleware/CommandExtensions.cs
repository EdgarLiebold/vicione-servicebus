using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Sends typed control commands through command pipelines.</summary>
public static class CommandExtensions
{
    /// <summary>Sends a control command with a context-scoped clock and cancellation token.</summary>
    /// <typeparam name="TCommand">The command contract type.</typeparam>
    /// <param name="pipe">The command pipeline.</param>
    /// <param name="command">The command to send.</param>
    /// <param name="timeProvider">The clock used to timestamp the command context.</param>
    /// <param name="cancellationToken">The token that cancels command processing.</param>
    /// <returns>The asynchronous dispatch of the command through every configured command-pipeline stage.</returns>
    public static Task SendCommandAsync<TCommand>(this IPipe<CommandContext> pipe, TCommand command, TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        ArgumentNullException.ThrowIfNull(command);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var context = new SendCommandContext<TCommand>(command, timeProvider ?? TimeProvider.System, cancellationToken);

        return pipe.SendAsync(context);
    }

    private sealed class SendCommandContext<TCommand> :
        BasePipeContext,
        CommandContext<TCommand>
        where TCommand : class
    {
        public SendCommandContext(TCommand command, TimeProvider timeProvider, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Command = command;
            Timestamp = timeProvider.GetUtcNow();
            this.SetTimeProvider(timeProvider);
        }

        public DateTimeOffset Timestamp { get; }

        public TCommand Command { get; }
    }
}
