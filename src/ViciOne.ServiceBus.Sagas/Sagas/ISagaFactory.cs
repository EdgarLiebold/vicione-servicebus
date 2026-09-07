using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Creates a saga instance when an existing saga instance is missing.</summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISagaFactory<out TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Create a new saga instance using the supplied consume context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    TSaga Create(ConsumeContext<TMessage> context);

    /// <summary>Send the context through the factory, with the proper decorations.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next);
}
