using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Connects a handler to the inbound pipe of the receive endpoint.</summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public class ResponseHandlerConfigurator<TResponse> :
    IHandlerConfigurator<TResponse>
    where TResponse : class
{
    readonly TaskCompletionSource<ConsumeContext<TResponse>> _completed;
    readonly MessageHandler<TResponse>? _handler;
    readonly IBuildPipeConfigurator<ConsumeContext<TResponse>> _pipeConfigurator;
    readonly Task _requestTask;
    readonly TaskScheduler _taskScheduler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="taskScheduler">The task scheduler.</param>
    /// <param name="handler">The handler.</param>
    /// <param name="requestTask">The request task.</param>
    public ResponseHandlerConfigurator(TaskScheduler taskScheduler, MessageHandler<TResponse>? handler, Task requestTask)
    {
        _taskScheduler = taskScheduler;
        _handler = handler;
        _requestTask = requestTask;

        _pipeConfigurator = new PipeConfigurator<ConsumeContext<TResponse>>();
        _completed = TaskCompletionSources.Create<ConsumeContext<TResponse>>();
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TResponse>> specification)
    {
        _pipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Connects handler configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return new EmptyConnectHandle();
    }

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <param name="connector">The connector.</param>
    /// <param name="requestId">The request id.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public HandlerConnectHandle<TResponse> Connect(IRequestPipeConnector connector, Guid requestId)
    {
        MessageHandler<TResponse> messageHandler = _handler != null ? AsyncMessageHandlerAsync : MessageHandlerAsync;

        var connectHandle = connector.ConnectRequestHandler(requestId, messageHandler, _pipeConfigurator);

        return new ResponseHandlerConnectHandle<TResponse>(connectHandle, _completed, _requestTask);
    }

    async Task AsyncMessageHandlerAsync(ConsumeContext<TResponse> context)
    {
        try
        {
            await Task.Factory.StartNew(() => _handler!(context), context.CancellationToken, TaskCreationOptions.None, _taskScheduler)
                .Unwrap()
                .ConfigureAwait(false);

            _completed.TrySetResult(context);
        }
        catch (Exception ex)
        {
            _completed.TrySetException(ex);
        }
    }

    Task MessageHandlerAsync(ConsumeContext<TResponse> context)
    {
        try
        {
            _completed.TrySetResult(context);
        }
        catch (Exception ex)
        {
            _completed.TrySetException(ex);
        }

        return Task.CompletedTask;
    }
}
