using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>
/// Provides an array property converter implementation.
/// </summary>
/// <typeparam name="TElement">The t element type.</typeparam>
public class ArrayPropertyConverter<TElement> :
    IPropertyConverter<TElement[], IEnumerable<TElement>>
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TElement[]?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TElement>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<TElement[]>(cancellationToken: cancellationToken);
            case TElement[] array:
                return Task.FromResult<TElement[]?>(array);
            default:
                return Task.FromResult<TElement[]?>(input.ToArray());
        }
    }
}


/// <summary>
/// Provides an array property converter implementation.
/// </summary>
/// <typeparam name="TElement">The t element type.</typeparam>
/// <typeparam name="TInputElement">The t input element type.</typeparam>
public class ArrayPropertyConverter<TElement, TInputElement> :
    IPropertyConverter<TElement[], IEnumerable<TInputElement>>
{
    static readonly TElement[] _emptyArray = new TElement[0];
    readonly IPropertyConverter<TElement, TInputElement> _converter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="converter">The converter value.</param>
    public ArrayPropertyConverter(IPropertyConverter<TElement, TInputElement> converter)
    {
        _converter = converter;
    }

    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TElement[]?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TInputElement>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TElement[]?>(cancellationToken); Task<TElement[]?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.IsCompleted)
            return Task.FromResult<TElement[]?>(resultTask.Result);

        async Task<TElement[]?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<TElement[]?> ConvertSyncAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TInputElement>? input)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<TElement[]>();

        var capacity = 0;
        if (input is ICollection<TElement> collection)
        {
            capacity = collection.Count;
            if (capacity == 0)
                return Task.FromResult<TElement[]?>(_emptyArray);
        }

        var results = new List<TElement>(capacity);
        IEnumerator<TInputElement> enumerator = input.GetEnumerator();
        var disposeEnumerator = true;
        try
        {
            async Task<TElement[]?> ConvertAsync(IEnumerator<TInputElement> asyncEnumerator, Task<TElement?> elementTask)
            {
                try
                {
                    var element = await elementTask.ConfigureAwait(false);

                    results.Add(element!);

                    while (asyncEnumerator.MoveNext())
                    {
                        var current = asyncEnumerator.Current;

                        elementTask = _converter.ConvertAsync(context, current);
                        if (elementTask.IsCompleted)
                            results.Add(elementTask.Result!);
                        else
                        {
                            element = await elementTask.ConfigureAwait(false);

                            results.Add(element!);
                        }
                    }

                    return results.ToArray();
                }
                finally
                {
                    asyncEnumerator.Dispose();
                }
            }

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                Task<TElement?> elementTask = _converter.ConvertAsync(context, current);
                if (elementTask.IsCompleted)
                    results.Add(elementTask.Result!);
                else
                {
                    disposeEnumerator = false;
                    return ConvertAsync(enumerator, elementTask);
                }
            }
        }
        finally
        {
            if (disposeEnumerator)
                enumerator.Dispose();
        }

        return Task.FromResult<TElement[]?>(results.ToArray());
    }
}
