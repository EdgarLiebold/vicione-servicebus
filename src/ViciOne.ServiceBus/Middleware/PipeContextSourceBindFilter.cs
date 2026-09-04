using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Binds a context to the pipe using a <see cref="IPipeContextSource{TSource}" />.
/// </summary>
/// <typeparam name="TLeft"></typeparam>
/// <typeparam name="TRight"></typeparam>
public class PipeContextSourceBindFilter<TLeft, TRight> :
    IFilter<TLeft>
    where TLeft : class, PipeContext
    where TRight : class, PipeContext
{
    readonly IPipe<BindContext<TLeft, TRight>> _output;
    readonly IPipeContextSource<TRight, TLeft> _source;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="output">The output value.</param>
    /// <param name="source">The source value.</param>
    public PipeContextSourceBindFilter(IPipe<BindContext<TLeft, TRight>> output, IPipeContextSource<TRight, TLeft> source)
    {
        _output = output;
        _source = source;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(TLeft context, IPipe<TLeft> next)
    {
        var bindPipe = new BindPipe(context, _output);

        var sourceTask = _source.SendAsync(context, bindPipe);
        if (sourceTask.Status == TaskStatus.RanToCompletion)
            return next.SendAsync(context);

        async Task SendAsync()
        {
            await sourceTask.ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
        }

        return SendAsync();
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("bind");
        _output.Probe(scope);
        _source.Probe(scope);
    }


    class BindPipe :
        IPipe<TRight>
    {
        readonly TLeft _context;
        readonly IPipe<BindContext<TLeft, TRight>> _output;

        public BindPipe(TLeft context, IPipe<BindContext<TLeft, TRight>> output)
        {
            _context = context;
            _output = output;
        }

        public Task SendAsync(TRight context)
        {
            var bindContext = new BindContextProxy<TLeft, TRight>(_context, context);

            return _output.SendAsync(bindContext);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
