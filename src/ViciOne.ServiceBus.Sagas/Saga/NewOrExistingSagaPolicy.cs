using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Dispatches to existing saga instances or creates missing instances through a saga factory.</summary>
/// <typeparam name="TSaga">The saga state supplied by existing instances or the factory.</typeparam>
/// <typeparam name="TMessage">The message contract used for saga dispatch and creation.</typeparam>
public class NewOrExistingSagaPolicy<TSaga, TMessage> :
    ISagaPolicy<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly bool _insertOnInitial;
    readonly ISagaFactory<TSaga, TMessage> _sagaFactory;

    /// <summary>Configures factory-based saga creation and optional repository pre-insertion.</summary>
    /// <param name="sagaFactory">The factory creating and dispatching missing saga instances.</param>
    /// <param name="insertOnInitial">Whether the repository's pre-insertion check creates an instance.</param>
    public NewOrExistingSagaPolicy(ISagaFactory<TSaga, TMessage> sagaFactory, bool insertOnInitial)
    {
        _sagaFactory = sagaFactory;
        _insertOnInitial = insertOnInitial;
    }

    /// <summary>Gets false because this policy permits saga creation and mutation.</summary>
    public bool IsReadOnly => false;

    /// <summary>Creates an instance through the factory when repository pre-insertion is enabled.</summary>
    /// <param name="context">The message context passed to the saga factory.</param>
    /// <param name="instance">Receives the factory-created instance, or null when pre-insertion is disabled.</param>
    /// <returns>True when pre-insertion is enabled; otherwise false.</returns>
    public bool PreInsertInstance(ConsumeContext<TMessage> context, [NotNullWhen(true)] out TSaga? instance)
    {
        if (_insertOnInitial)
        {
            instance = _sagaFactory.Create(context);
            return true;
        }

        instance = null;
        return false;
    }

    Task ISagaPolicy<TSaga, TMessage>.ExistingAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        return next.SendAsync(context);
    }

    Task ISagaPolicy<TSaga, TMessage>.MissingAsync(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        return _sagaFactory.SendAsync(context, next);
    }
}
