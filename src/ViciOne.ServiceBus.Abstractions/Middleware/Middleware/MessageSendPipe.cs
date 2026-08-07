// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    using System.Diagnostics;
    using System.Threading.Tasks;


    /// <summary>
    /// Converts an inbound context type to a pipe context type post-dispatch
    /// </summary>
    /// <typeparam name="TOutput">The subsequent pipe context type</typeparam>
    public class MessageSendPipe<TOutput> :
        IMessageSendPipe<TOutput>
        where TOutput : class
    {
        readonly IPipe<SendContext<TOutput>> _outputPipe;

        public MessageSendPipe(IPipe<SendContext<TOutput>> outputPipe)
        {
            _outputPipe = outputPipe;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("messageSendPipe");
            scope.Add("outputType", TypeCache<TOutput>.ShortName);

            _outputPipe.Probe(scope);
        }

        [DebuggerNonUserCode]
        Task IPipe<SendContext<TOutput>>.Send(SendContext<TOutput> context)
        {
            return _outputPipe.Send(context);
        }
    }
}
