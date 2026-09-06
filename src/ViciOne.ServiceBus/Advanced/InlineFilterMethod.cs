using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents an inline asynchronous pipeline filter.</summary>
/// <typeparam name="T">The pipe-context type.</typeparam>
/// <param name="context">The context to filter.</param>
/// <param name="next">The next pipeline stage to invoke.</param>
/// <returns>A task that completes when the filter and downstream pipeline have completed.</returns>
public delegate Task InlineFilterMethod<T>(T context, IPipe<T> next)
    where T : class, PipeContext;
