using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus;

public static class CommandExtensions
{
    public static Task SendCommandAsync<T>(this IPipe<CommandContext> pipe, T command, TimeProvider? timeProvider = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var context = new SendCommandContext<T>(command, timeProvider ?? TimeProvider.System);

        return pipe.SendAsync(context);
    }


    class SendCommandContext<T> :
        BasePipeContext,
        CommandContext<T>
        where T : class
    {
        public SendCommandContext(T command, TimeProvider timeProvider)
        {
            Command = command;
            Timestamp = timeProvider.GetUtcNow();
            this.SetTimeProvider(timeProvider);
        }

        public DateTimeOffset Timestamp { get; }

        public T Command { get; }
    }
}
