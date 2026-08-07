// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    public interface IOutputPipeFilter<TInput, out TOutput> :
        IFilter<TInput>,
        IPipeConnector<TOutput>,
        IFilterObserverConnector<TOutput>
        where TInput : class, PipeContext
        where TOutput : class, PipeContext
    {
    }


    public interface IOutputPipeFilter<TInput, out TOutput, in TKey> :
        IOutputPipeFilter<TInput, TOutput>,
        IKeyPipeConnector<TKey>
        where TInput : class, PipeContext
        where TOutput : class, PipeContext
    {
    }
}
