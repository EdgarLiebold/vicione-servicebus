using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// A message property converter, which is async, and has access to the context
/// </summary>
/// <typeparam name="TResult"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public interface IPropertyConverter<TResult, in TProperty>
{
    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TProperty? input, CancellationToken cancellationToken = default)
        where T : class;
}
