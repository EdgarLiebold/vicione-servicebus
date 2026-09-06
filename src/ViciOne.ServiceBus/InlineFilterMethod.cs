using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents the method that handles inline filter method.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <param name="next">The next pipeline stage to invoke.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task InlineFilterMethod<T>(T context, IPipe<T> next)
    where T : class, PipeContext;
