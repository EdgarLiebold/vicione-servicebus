using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts list property values.</summary>
/// <typeparam name="TElement">The element type.</typeparam>
public class ListPropertyConverter<TElement> :
    IPropertyConverter<List<TElement>, IEnumerable<TElement>>,
    IPropertyConverter<IList<TElement>, IEnumerable<TElement>>,
    IPropertyConverter<IReadOnlyList<TElement>, IEnumerable<TElement>>,
    IPropertyConverter<IEnumerable<TElement>, IEnumerable<TElement>>,
    IPropertyConverter<ICollection<TElement>, IEnumerable<TElement>>
{
    Task<ICollection<TElement>?> IPropertyConverter<ICollection<TElement>, IEnumerable<TElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TElement>? input, CancellationToken cancellationToken)
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<ICollection<TElement>>(cancellationToken: cancellationToken);
            case ICollection<TElement> list:
                return Task.FromResult<ICollection<TElement>?>(list);
            default:
                return Task.FromResult<ICollection<TElement>?>(input.ToList());
        }
    }

    Task<IEnumerable<TElement>?> IPropertyConverter<IEnumerable<TElement>, IEnumerable<TElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TElement>? input, CancellationToken cancellationToken)
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<IEnumerable<TElement>>(cancellationToken: cancellationToken);
            default:
                return Task.FromResult<IEnumerable<TElement>?>(input);
        }
    }

    Task<IList<TElement>?> IPropertyConverter<IList<TElement>, IEnumerable<TElement>>.ConvertAsync<T>(InitializeContext<T> context, IEnumerable<TElement>? input, CancellationToken cancellationToken)
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<IList<TElement>>(cancellationToken: cancellationToken);
            case IList<TElement> list:
                return Task.FromResult<IList<TElement>?>(list);
            default:
                return Task.FromResult<IList<TElement>?>(input.ToList());
        }
    }

    Task<IReadOnlyList<TElement>?> IPropertyConverter<IReadOnlyList<TElement>, IEnumerable<TElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TElement>? input, CancellationToken cancellationToken)
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<IReadOnlyList<TElement>>(cancellationToken: cancellationToken);
            case IReadOnlyList<TElement> list:
                return Task.FromResult<IReadOnlyList<TElement>?>(list);
            default:
                return Task.FromResult<IReadOnlyList<TElement>?>(input.ToList());
        }
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<List<TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TElement>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<List<TElement>>(cancellationToken: cancellationToken);
            case List<TElement> list:
                return Task.FromResult<List<TElement>?>(list);
            default:
                return Task.FromResult<List<TElement>?>(input.ToList());
        }
    }
}


/// <summary>Converts list property values.</summary>
/// <typeparam name="TElement">The element type.</typeparam>
/// <typeparam name="TInputElement">The input element type.</typeparam>
public class ListPropertyConverter<TElement, TInputElement> :
    IPropertyConverter<List<TElement>, IEnumerable<TInputElement>>,
    IPropertyConverter<IList<TElement>, IEnumerable<TInputElement>>,
    IPropertyConverter<IReadOnlyList<TElement>, IEnumerable<TInputElement>>,
    IPropertyConverter<IEnumerable<TElement>, IEnumerable<TInputElement>>,
    IPropertyConverter<ICollection<TElement>, IEnumerable<TInputElement>>
{
    readonly IPropertyConverter<TElement, TInputElement> _converter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    public ListPropertyConverter(IPropertyConverter<TElement, TInputElement> converter)
    {
        _converter = converter;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="elements">The elements.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<ICollection<TElement>?> ConvertAsync<T>(InitializeContext<T> context, IEnumerable<TInputElement>? elements, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.Collections.Generic.ICollection<TElement>?>(cancellationToken); Task<List<TElement>?> resultTask = ConvertSyncAsync(context, elements);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<ICollection<TElement>?>(resultTask.Result);

        async Task<ICollection<TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IEnumerable<TElement>?> IPropertyConverter<IEnumerable<TElement>, IEnumerable<TInputElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TInputElement>? elements, CancellationToken cancellationToken)
    {
        Task<List<TElement>?> resultTask = ConvertSyncAsync(context, elements);
        if (resultTask.IsCompleted)
            return Task.FromResult<IEnumerable<TElement>?>(resultTask.Result);

        async Task<IEnumerable<TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IList<TElement>?> IPropertyConverter<IList<TElement>, IEnumerable<TInputElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TInputElement>? elements, CancellationToken cancellationToken)
    {
        Task<List<TElement>?> resultTask = ConvertSyncAsync(context, elements);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IList<TElement>?>(resultTask.Result);

        async Task<IList<TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IReadOnlyList<TElement>?> IPropertyConverter<IReadOnlyList<TElement>, IEnumerable<TInputElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TInputElement>? elements, CancellationToken cancellationToken)
    {
        Task<List<TElement>?> resultTask = ConvertSyncAsync(context, elements);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IReadOnlyList<TElement>?>(resultTask.Result);

        async Task<IReadOnlyList<TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<List<TElement>?> IPropertyConverter<List<TElement>, IEnumerable<TInputElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TInputElement>? elements, CancellationToken cancellationToken)
    {
        return ConvertSyncAsync(context, elements);
    }

    Task<List<TElement>?> ConvertSyncAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TInputElement>? input)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<List<TElement>>();

        var capacity = 0;
        if (input is ICollection<TElement> collection)
        {
            capacity = collection.Count;
            if (capacity == 0)
                return Task.FromResult<List<TElement>?>(new List<TElement>());
        }

        var results = new List<TElement>(capacity);
        IEnumerator<TInputElement> enumerator = input.GetEnumerator();
        var disposeEnumerator = true;
        try
        {
            async Task<List<TElement>?> ConvertAsync(IEnumerator<TInputElement> asyncEnumerator, Task<TElement?> elementTask)
            {
                try
                {
                    var element = await elementTask.ConfigureAwait(false);

                    results.Add(element!);

                    while (asyncEnumerator.MoveNext())
                    {
                        var current = asyncEnumerator.Current;

                        elementTask = _converter.ConvertAsync(context, current);
                        if (elementTask.Status == TaskStatus.RanToCompletion)
                            results.Add(elementTask.Result!);
                        else
                        {
                            element = await elementTask.ConfigureAwait(false);

                            results.Add(element!);
                        }
                    }

                    return results;
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
                if (elementTask.Status == TaskStatus.RanToCompletion)
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

        return Task.FromResult<List<TElement>?>(results);
    }
}
