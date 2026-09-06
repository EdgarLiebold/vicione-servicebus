using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Provides an endpoint for mediator publish send.</summary>
public class MediatorPublishSendEndpoint :
    SendEndpointProxy,
    IPublishObserverConnector
{
    readonly PublishObservable _observers;
    readonly IPublishPipe _publishPipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    public MediatorPublishSendEndpoint(ISendEndpoint endpoint, IPublishPipe publishPipe)
        : base(endpoint)
    {
        _publishPipe = publishPipe;

        _observers = new PublishObservable();
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Gets pipe proxy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>The pipe proxy.</returns>
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new PublishPipeAdapter<T>(_publishPipe, pipe);
    }


    class PublishPipeAdapter<T> :
        IPipe<SendContext<T>>
        where T : class
    {
        readonly IPipe<SendContext<T>>? _pipe;
        readonly IPublishPipe _publishPipe;

        public PublishPipeAdapter(IPublishPipe publishPipe, IPipe<SendContext<T>>? pipe)
        {
            _publishPipe = publishPipe;
            _pipe = pipe;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            _pipe?.Probe(context);
        }

        public async Task SendAsync(SendContext<T> context)
        {
            var publishContext = context.GetPayload<MessageSendContext<T>>();

            publishContext.IsPublish = true;

            if (_pipe is ISendContextPipe sendContextPipe)
                await sendContextPipe.SendAsync(context).ConfigureAwait(false);

            await _publishPipe.SendAsync(publishContext).ConfigureAwait(false);

            if (_pipe != null && _pipe.IsNotEmpty())
                await _pipe.SendAsync(context).ConfigureAwait(false);
        }
    }
}
