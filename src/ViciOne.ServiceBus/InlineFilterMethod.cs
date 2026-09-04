using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Represents the method that handles inline filter method.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <param name="next">The next value.</param>
/// <returns>The result of the operation.</returns>
public delegate Task InlineFilterMethod<T>(T context, IPipe<T> next)
    where T : class, PipeContext;
