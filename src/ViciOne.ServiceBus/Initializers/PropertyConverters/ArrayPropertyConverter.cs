using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Materializes an element sequence as an array without changing its elements.</summary>
/// <typeparam name="TElement">The array element type.</typeparam>
internal sealed class ArrayPropertyConverter<TElement> :
    IPropertyConverter<TElement[], IEnumerable<TElement>>
{
    /// <inheritdoc />
    public Task<TElement[]?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TElement>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TElement[]?>(cancellationToken);

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


/// <summary>Converts each source element and materializes the results as an array.</summary>
/// <typeparam name="TElement">The result element type.</typeparam>
/// <typeparam name="TInputElement">The source element type.</typeparam>
/// <remarks>Each accepted element conversion is observed before traversal completes or releases its enumerator.</remarks>
internal sealed class ArrayPropertyConverter<TElement, TInputElement> :
    IPropertyConverter<TElement[], IEnumerable<TInputElement>>
{
    static readonly TElement[] _emptyArray = [];
    readonly IPropertyConverter<TElement, TInputElement> _converter;

    /// <summary>Creates an array converter that applies <paramref name="converter" /> to every element.</summary>
    /// <param name="converter">The element conversion.</param>
    public ArrayPropertyConverter(IPropertyConverter<TElement, TInputElement> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <inheritdoc />
    public Task<TElement[]?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TInputElement>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TElement[]?>(cancellationToken);

        return ConvertCoreAsync(context, input, cancellationToken);
    }

    Task<TElement[]?> ConvertCoreAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<TInputElement>? input,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<TElement[]>();

        var capacity = 0;
        if (input is ICollection<TInputElement> collection)
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

                    return results.ToArray();
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

        return Task.FromResult<TElement[]?>(results.ToArray());
    }

    Task<TElement?> ConvertElementAsync<TMessage>(InitializeContext<TMessage> context, TInputElement input,
        CancellationToken cancellationToken)
        where TMessage : class => _converter.ConvertAsync(context, input, cancellationToken)
            ?? throw new InvalidOperationException("The array element converter returned a null task.");
}
