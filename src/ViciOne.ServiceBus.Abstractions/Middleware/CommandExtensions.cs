using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus;

public static class CommandExtensions
{
    public static Task SendCommand<T>(this IPipe<CommandContext> pipe, T command, TimeProvider? timeProvider = null)
        where T : class
    {
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var context = new SendCommandContext<T>(command, timeProvider ?? TimeProvider.System);

        return pipe.Send(context);
    }


    class SendCommandContext<T> :
        BasePipeContext,
        CommandContext<T>
        where T : class
    {
        public SendCommandContext(T command, TimeProvider timeProvider)
        {
            Command = command;
            Timestamp = timeProvider.GetUtcNow().UtcDateTime;
            this.SetTimeProvider(timeProvider);
        }

        public DateTime Timestamp { get; }

        public T Command { get; }
    }
}
