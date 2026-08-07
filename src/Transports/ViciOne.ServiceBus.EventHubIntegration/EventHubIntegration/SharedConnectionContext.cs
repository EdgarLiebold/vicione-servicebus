// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using System.Threading;
    using Azure.Messaging.EventHubs.Producer;
    using ViciOne.ServiceBus.Middleware;


    public class SharedConnectionContext :
        ProxyPipeContext,
        ConnectionContext
    {
        readonly ConnectionContext _context;

        public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
            : base(context)
        {
            _context = context;
            CancellationToken = cancellationToken;
        }

        public override CancellationToken CancellationToken { get; }

        public EventHubProducerClient CreateEventHubClient(string eventHubName)
        {
            return _context.CreateEventHubClient(eventHubName);
        }
    }
}
