// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;
    using Transports;


    public class ScopedSendPipeAdapter<TMessage> :
        SendContextPipeAdapter<TMessage>
        where TMessage : class
    {
        readonly IServiceProvider _provider;

        public ScopedSendPipeAdapter(IServiceProvider provider, IPipe<SendContext<TMessage>> pipe)
            : base(pipe)
        {
            _provider = provider;
        }

        protected override void Send<T>(SendContext<T> context)
        {
            context.GetOrAddPayload(() => _provider);
        }

        protected override void Send(SendContext<TMessage> context)
        {
        }
    }
}
