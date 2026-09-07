using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates a saga instance using the default factory method.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class FactoryMethodSagaFactory<TSaga, TMessage> :
    ISagaFactory<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly SagaFactoryMethod<TSaga, TMessage> _factoryMethod;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public FactoryMethodSagaFactory(SagaFactoryMethod<TSaga, TMessage> factoryMethod)
    {
        _factoryMethod = factoryMethod;
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    public TSaga Create(ConsumeContext<TMessage> context)
    {
        if (!context.CorrelationId.HasValue)
            throw new SagaException("The correlationId was not present and the saga could not be created", typeof(TSaga), typeof(TMessage));

        return _factoryMethod(context);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        if (!context.CorrelationId.HasValue)
            throw new SagaException("The correlationId was not present and the saga could not be created", typeof(TSaga), typeof(TMessage));

        var instance = _factoryMethod(context);

        var proxy = new DefaultSagaConsumeContext<TSaga, TMessage>(context, instance);

        proxy.LogCreated();

        return next.SendAsync(proxy);
    }
}
