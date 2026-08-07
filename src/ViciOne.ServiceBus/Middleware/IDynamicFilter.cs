// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    public interface IDynamicFilter<TInput> :
        IFilter<TInput>,
        IPipeConnector,
        IFilterObserverConnector
        where TInput : class, PipeContext
    {
    }


    public interface IDynamicFilter<TInput, in TKey> :
        IFilter<TInput>,
        IPipeConnector,
        IKeyPipeConnector<TKey>,
        IFilterObserverConnector
        where TInput : class, PipeContext
    {
    }
}
