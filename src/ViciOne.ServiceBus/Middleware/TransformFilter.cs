using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Transformation;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Applies a transform to the message.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class TransformFilter<T> :
    IFilter<ConsumeContext<T>>,
    IFilter<ExecuteContext<T>>,
    IFilter<CompensateContext<T>>,
    IFilter<SendContext<T>>
    where T : class
{
    readonly IMessageInitializer<T> _initializer;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="initializer">The initializer.</param>
    public TransformFilter(IMessageInitializer<T> initializer)
    {
        _initializer = initializer;
    }

    Task IFilter<CompensateContext<T>>.SendAsync(CompensateContext<T> context, IPipe<CompensateContext<T>> next)
    {
        var transformContext = new ConsumeTransformContext<T>(context, context.Log);

        Task<InitializeContext<T>> initializeTask = _initializer.InitializeAsync(_initializer.Create(transformContext), context.Log);
        if (initializeTask.Status == TaskStatus.RanToCompletion)
        {
            var log = initializeTask.Result.Message;

            return next.SendAsync(ReferenceEquals(log, context.Log)
                ? context
                : new CompensateContextProxy<T>(context, log));
        }

        async Task SendAsync()
        {
            InitializeContext<T> initializeContext = await initializeTask.ConfigureAwait(false);

            await next.SendAsync(ReferenceEquals(initializeContext.Message, context.Log)
                ? context
                : new CompensateContextProxy<T>(context, initializeContext.Message)).ConfigureAwait(false);
        }

        return SendAsync();
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("transform");
    }

    Task IFilter<ConsumeContext<T>>.SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var transformContext = new ConsumeTransformContext<T>(context.Advanced(), context.Message);

        Task<InitializeContext<T>> initializeTask = _initializer.InitializeAsync(_initializer.Create(transformContext), context.Message);
        if (initializeTask.Status == TaskStatus.RanToCompletion)
        {
            var message = initializeTask.Result.Message;

            return next.SendAsync(ReferenceEquals(message, context.Message)
                ? context
                : new MessageConsumeContext<T>(context.Advanced(), message));
        }

        async Task SendAsync()
        {
            InitializeContext<T> initializeContext = await initializeTask.ConfigureAwait(false);

            await next.SendAsync(ReferenceEquals(initializeContext.Message, context.Message)
                ? context
                : new MessageConsumeContext<T>(context.Advanced(), initializeContext.Message)).ConfigureAwait(false);
        }

        return SendAsync();
    }

    Task IFilter<ExecuteContext<T>>.SendAsync(ExecuteContext<T> context, IPipe<ExecuteContext<T>> next)
    {
        var transformContext = new ConsumeTransformContext<T>(context, context.Arguments);

        Task<InitializeContext<T>> initializeTask = _initializer.InitializeAsync(_initializer.Create(transformContext), context.Arguments);
        if (initializeTask.Status == TaskStatus.RanToCompletion)
        {
            var arguments = initializeTask.Result.Message;

            return next.SendAsync(ReferenceEquals(arguments, context.Arguments)
                ? context
                : new ExecuteContextProxy<T>(context, arguments));
        }

        async Task SendAsync()
        {
            InitializeContext<T> initializeContext = await initializeTask.ConfigureAwait(false);

            await next.SendAsync(ReferenceEquals(initializeContext.Message, context.Arguments)
                ? context
                : new ExecuteContextProxy<T>(context, initializeContext.Message)).ConfigureAwait(false);
        }

        return SendAsync();
    }

    Task IFilter<SendContext<T>>.SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        var transformContext = new SendTransformContext<T>(context);

        Task<InitializeContext<T>> initializeTask = _initializer.InitializeAsync(_initializer.Create(transformContext), context.Message);
        if (initializeTask.Status == TaskStatus.RanToCompletion)
        {
            var message = initializeTask.Result.Message;

            return next.SendAsync(ReferenceEquals(message, context.Message)
                ? context
                : context.CreateProxy(message));
        }

        async Task SendAsync()
        {
            InitializeContext<T> initializeContext = await initializeTask.ConfigureAwait(false);

            await next.SendAsync(ReferenceEquals(initializeContext.Message, context.Message)
                ? context
                : context.CreateProxy(initializeContext.Message)).ConfigureAwait(false);
        }

        return SendAsync();
    }
}
