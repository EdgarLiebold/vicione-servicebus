using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by saga policy.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISagaPolicy<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>If true, changes should not be saved to the saga repository.</summary>
    bool IsReadOnly { get; }

    /// <summary>Produces a saga instance to attempt inserting before message processing when this policy supports pre-insertion; insertion and failure handling follow the repository provider's policy.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="instance">Receives the instance produced by the operation.</param>
    /// <returns>True if the instance should be inserted before invoking the message logic.</returns>
    bool PreInsertInstance(ConsumeContext<TMessage> context, [NotNullWhen(true)] out TSaga? instance);

    /// <summary>The method invoked when an existing saga instance is present.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A non-null task that represents the asynchronous operation.</returns>
    Task ExistingAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next);

    /// <summary>Invoked when there is not an existing saga instance available.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A non-null task that represents the asynchronous operation.</returns>
    Task MissingAsync(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next);
}
