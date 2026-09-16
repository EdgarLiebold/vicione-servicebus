using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Adapts an element sequence to the requested list-shaped contract without changing its elements.</summary>
/// <typeparam name="TElement">The collection element type.</typeparam>
internal sealed class ListPropertyConverter<TElement> :
    IPropertyConverter<List<TElement>, IEnumerable<TElement>>,
    IPropertyConverter<IList<TElement>, IEnumerable<TElement>>,
    IPropertyConverter<IReadOnlyList<TElement>, IEnumerable<TElement>>,
    IPropertyConverter<IEnumerable<TElement>, IEnumerable<TElement>>,
    IPropertyConverter<ICollection<TElement>, IEnumerable<TElement>>
{
    Task<ICollection<TElement>?> IPropertyConverter<ICollection<TElement>, IEnumerable<TElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TElement>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ICollection<TElement>?>(cancellationToken);

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
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IEnumerable<TElement>?>(cancellationToken);

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
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IList<TElement>?>(cancellationToken);

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
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IReadOnlyList<TElement>?>(cancellationToken);

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

    /// <inheritdoc />
    public Task<List<TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TElement>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<List<TElement>?>(cancellationToken);

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


/// <summary>Converts each source element and exposes the results through list-shaped collection contracts.</summary>
/// <typeparam name="TElement">The result element type.</typeparam>
/// <typeparam name="TInputElement">The source element type.</typeparam>
/// <remarks>Each accepted element conversion is observed before traversal or a shape adapter completes.</remarks>
internal sealed class ListPropertyConverter<TElement, TInputElement> :
    IPropertyConverter<List<TElement>, IEnumerable<TInputElement>>,
    IPropertyConverter<IList<TElement>, IEnumerable<TInputElement>>,
    IPropertyConverter<IReadOnlyList<TElement>, IEnumerable<TInputElement>>,
    IPropertyConverter<IEnumerable<TElement>, IEnumerable<TInputElement>>,
    IPropertyConverter<ICollection<TElement>, IEnumerable<TInputElement>>
{
    readonly IPropertyConverter<TElement, TInputElement> _converter;

    /// <summary>Creates a list converter that applies <paramref name="converter" /> to every element.</summary>
    /// <param name="converter">The element conversion.</param>
    public ListPropertyConverter(IPropertyConverter<TElement, TInputElement> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <inheritdoc />
    public Task<ICollection<TElement>?> ConvertAsync<T>(InitializeContext<T> context, IEnumerable<TInputElement>? elements, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ICollection<TElement>?>(cancellationToken);

        Task<List<TElement>?> resultTask = ConvertCoreAsync(context, elements, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<ICollection<TElement>?>(resultTask.GetAwaiter().GetResult());

        async Task<ICollection<TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IEnumerable<TElement>?> IPropertyConverter<IEnumerable<TElement>, IEnumerable<TInputElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TInputElement>? elements, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IEnumerable<TElement>?>(cancellationToken);

        Task<List<TElement>?> resultTask = ConvertCoreAsync(context, elements, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IEnumerable<TElement>?>(resultTask.GetAwaiter().GetResult());

        async Task<IEnumerable<TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IList<TElement>?> IPropertyConverter<IList<TElement>, IEnumerable<TInputElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TInputElement>? elements, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IList<TElement>?>(cancellationToken);

        Task<List<TElement>?> resultTask = ConvertCoreAsync(context, elements, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IList<TElement>?>(resultTask.GetAwaiter().GetResult());

        async Task<IList<TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IReadOnlyList<TElement>?> IPropertyConverter<IReadOnlyList<TElement>, IEnumerable<TInputElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TInputElement>? elements, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IReadOnlyList<TElement>?>(cancellationToken);

        Task<List<TElement>?> resultTask = ConvertCoreAsync(context, elements, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IReadOnlyList<TElement>?>(resultTask.GetAwaiter().GetResult());

        async Task<IReadOnlyList<TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<List<TElement>?> IPropertyConverter<List<TElement>, IEnumerable<TInputElement>>.ConvertAsync<T>(InitializeContext<T> context,
        IEnumerable<TInputElement>? elements, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<List<TElement>?>(cancellationToken);

        return ConvertCoreAsync(context, elements, cancellationToken);
    }

    Task<List<TElement>?> ConvertCoreAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TInputElement>? input,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<List<TElement>>();

        var capacity = 0;
        if (input is ICollection<TInputElement> collection)
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
                        cancellationToken.ThrowIfCancellationRequested();
                        var current = asyncEnumerator.Current;

                        elementTask = ConvertElementAsync(context, current, cancellationToken);
                        if (elementTask.IsCompletedSuccessfully)
                            results.Add(elementTask.GetAwaiter().GetResult()!);
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
                cancellationToken.ThrowIfCancellationRequested();
                var current = enumerator.Current;

                Task<TElement?> elementTask = ConvertElementAsync(context, current, cancellationToken);
                if (elementTask.IsCompletedSuccessfully)
                    results.Add(elementTask.GetAwaiter().GetResult()!);
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

    Task<TElement?> ConvertElementAsync<TMessage>(InitializeContext<TMessage> context, TInputElement input,
        CancellationToken cancellationToken)
        where TMessage : class => _converter.ConvertAsync(context, input, cancellationToken)
            ?? throw new InvalidOperationException("The list element converter returned a null task.");
}
