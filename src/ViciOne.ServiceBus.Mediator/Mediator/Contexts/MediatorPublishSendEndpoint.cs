using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Marks outgoing mediator contexts as publications and applies the configured publish pipe.</summary>
internal sealed class MediatorPublishSendEndpoint :
    SendEndpointProxy,
    IPublishObserverConnector
{
    readonly PublishObservable _observers;
    readonly IPublishPipe _publishPipe;

    /// <summary>Creates a publishing view over the mediator send endpoint.</summary>
    /// <param name="endpoint">The mediator endpoint that performs the dispatch.</param>
    /// <param name="publishPipe">The pipe that applies publication metadata.</param>
    /// <param name="observers">The observers notified only for mediator publications.</param>
    public MediatorPublishSendEndpoint(ISendEndpoint endpoint, IPublishPipe publishPipe, PublishObservable observers)
        : base(endpoint)
    {
        _publishPipe = publishPipe ?? throw new ArgumentNullException(nameof(publishPipe));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }

    /// <inheritdoc />
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _observers.Connect(observer);
    }

    /// <inheritdoc />
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new PublishPipeAdapter<T>(_publishPipe, pipe);
    }
    sealed class PublishPipeAdapter<T> :
        IPipe<SendContext<T>>
        where T : class
    {
        readonly IPipe<SendContext<T>>? _pipe;
        readonly IPublishPipe _publishPipe;

        public PublishPipeAdapter(IPublishPipe publishPipe, IPipe<SendContext<T>>? pipe)
        {
            _publishPipe = publishPipe ?? throw new ArgumentNullException(nameof(publishPipe));
            _pipe = pipe;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            _pipe?.Probe(context);
        }

        public async Task SendAsync(SendContext<T> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.CancellationToken.ThrowIfCancellationRequested();
            var publishContext = context.GetPayload<MessageSendContext<T>>();

            publishContext.IsPublish = true;

            if (_pipe is ISendContextPipe sendContextPipe)
            {
                Task generalConfiguration = sendContextPipe.SendAsync(context, context.CancellationToken)
                    ?? throw new InvalidOperationException("The general publish send-context pipe returned no configuration task.");
                await generalConfiguration.ConfigureAwait(false);
            }

            Task publishConfiguration = _publishPipe.SendAsync(publishContext, context.CancellationToken)
                ?? throw new InvalidOperationException("The mediator publish pipe returned no configuration task.");
            await publishConfiguration.ConfigureAwait(false);

            if (_pipe != null && _pipe.IsNotEmpty())
            {
                Task additionalConfiguration = _pipe.SendAsync(context)
                    ?? throw new InvalidOperationException("The additional publish send pipe returned no configuration task.");
                await additionalConfiguration.ConfigureAwait(false);
            }
        }
    }
}
