using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for receive.</summary>
public class ReceivePipe :
    IReceivePipe
{
    readonly IConsumePipe _consumePipe;
    readonly IPipe<ReceiveContext> _receivePipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receivePipe">The receive pipe.</param>
    /// <param name="consumePipe">The consume pipe.</param>
    public ReceivePipe(IPipe<ReceiveContext> receivePipe, IConsumePipe consumePipe)
    {
        _receivePipe = receivePipe;
        _consumePipe = consumePipe;
    }

    /// <summary>Gets the connected.</summary>
    public Task Connected => _consumePipe.Connected;

    Task IPipe<ReceiveContext>.SendAsync(ReceiveContext context)
    {
        return _receivePipe.SendAsync(context);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _receivePipe.Probe(context);
    }

    ConnectHandle IConsumeMessageObserverConnector.ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
    {
        return _consumePipe.ConnectConsumeMessageObserver(observer);
    }

    ConnectHandle IConsumeObserverConnector.ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _consumePipe.ConnectConsumeObserver(observer);
    }

    ConnectHandle IConsumePipeConnector.ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
    {
        return _consumePipe.ConnectConsumePipe(pipe);
    }

    ConnectHandle IConsumePipeConnector.ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
    {
        return _consumePipe.ConnectConsumePipe(pipe, options);
    }

    ConnectHandle IRequestPipeConnector.ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
    {
        return _consumePipe.ConnectRequestPipe(requestId, pipe);
    }
}
