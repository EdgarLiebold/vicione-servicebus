using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for merge.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TSplit">The split type.</typeparam>
public class MergePipe<TInput, TSplit> :
    IPipe<TSplit>
    where TSplit : class, PipeContext
    where TInput : class, PipeContext
{
    readonly MergeFilterContextProvider<TInput, TSplit> _contextProvider;
    readonly TInput _input;
    readonly IPipe<TInput> _next;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="input">The input.</param>
    /// <param name="contextProvider">The context provider.</param>
    public MergePipe(IPipe<TInput> next, TInput input, MergeFilterContextProvider<TInput, TSplit> contextProvider)
    {
        _next = next;
        _input = input;
        _contextProvider = contextProvider;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("merge");
        scope.Set(new { InputType = TypeCache<TInput>.ShortName });

        _next.Probe(scope);
    }

    /// <summary>Invokes the context projection with the retained input and split context, then forwards its result to the continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(TSplit context)
    {
        var inputContext = _contextProvider(_input, context);

        return _next.SendAsync(inputContext);
    }
}
