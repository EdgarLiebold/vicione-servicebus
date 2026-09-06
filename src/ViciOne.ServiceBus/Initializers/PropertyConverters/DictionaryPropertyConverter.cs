using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts dictionary property values.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
public class DictionaryPropertyConverter<TKey, TElement> :
    IPropertyConverter<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TElement>>>,
    IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TElement>>>,
    IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TElement>>>,
    IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TKey, TElement>>>
    where TKey : notnull
{
    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<Dictionary<TKey, TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TKey, TElement>>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<Dictionary<TKey, TElement>>(cancellationToken: cancellationToken);
            case Dictionary<TKey, TElement> dictionary:
                return Task.FromResult<Dictionary<TKey, TElement>?>(dictionary);
            default:
                return Task.FromResult<Dictionary<TKey, TElement>?>(input.ToDictionary(x => x.Key, x => x.Value));
        }
    }

    Task<IDictionary<TKey, TElement>?> IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TElement>>>
        .ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TElement>>? input, CancellationToken cancellationToken)
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<IDictionary<TKey, TElement>>(cancellationToken: cancellationToken);
            case IDictionary<TKey, TElement> dictionary:
                return Task.FromResult<IDictionary<TKey, TElement>?>(dictionary);
            default:
                return Task.FromResult<IDictionary<TKey, TElement>?>(input.ToDictionary(x => x.Key, x => x.Value));
        }
    }

    Task<IEnumerable<KeyValuePair<TKey, TElement>>?>
        IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TKey, TElement>>>.ConvertAsync<TMessage>(
            InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TElement>>? input, CancellationToken cancellationToken)
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<IEnumerable<KeyValuePair<TKey, TElement>>>(cancellationToken: cancellationToken);
            default:
                return Task.FromResult<IEnumerable<KeyValuePair<TKey, TElement>>?>(input);
        }
    }

    Task<IReadOnlyDictionary<TKey, TElement>?> IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TElement>>>
        .ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TElement>>? input, CancellationToken cancellationToken)
    {
        switch (input)
        {
            case null:
                return TaskResults.DefaultAsync<IReadOnlyDictionary<TKey, TElement>>(cancellationToken: cancellationToken);
            case IReadOnlyDictionary<TKey, TElement> dictionary:
                return Task.FromResult<IReadOnlyDictionary<TKey, TElement>?>(dictionary);
            default:
                return Task.FromResult<IReadOnlyDictionary<TKey, TElement>?>(input.ToDictionary(x => x.Key, x => x.Value));
        }
    }
}


/// <summary>Converts dictionary property values.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
/// <typeparam name="TInputElement">The input element type.</typeparam>
public class DictionaryPropertyConverter<TKey, TElement, TInputElement> :
    IPropertyConverter<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>,
    IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>,
    IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>,
    IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TKey, TInputElement>>>
    where TKey : notnull
{
    readonly IPropertyConverter<TElement, TInputElement> _converter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    public DictionaryPropertyConverter(IPropertyConverter<TElement, TInputElement> converter)
    {
        _converter = converter;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<Dictionary<TKey, TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.Collections.Generic.Dictionary<TKey, TElement>?>(cancellationToken); return ConvertSyncAsync(context, input);
    }

    Task<IDictionary<TKey, TElement>?> IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IDictionary<TKey, TElement>?>(resultTask.Result);

        async Task<IDictionary<TKey, TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IEnumerable<KeyValuePair<TKey, TElement>>?>
        IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TKey, TInputElement>>>.ConvertAsync<TMessage>(
            InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IEnumerable<KeyValuePair<TKey, TElement>>?>(resultTask.Result);

        async Task<IEnumerable<KeyValuePair<TKey, TElement>>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IReadOnlyDictionary<TKey, TElement>?> IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IReadOnlyDictionary<TKey, TElement>?>(resultTask.Result);

        async Task<IReadOnlyDictionary<TKey, TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<Dictionary<TKey, TElement>?> ConvertSyncAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TKey, TInputElement>>? input)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<Dictionary<TKey, TElement>>();

        var capacity = 0;
        if (input is ICollection<TElement> collection)
        {
            capacity = collection.Count;
            if (capacity == 0)
                return Task.FromResult<Dictionary<TKey, TElement>?>(new Dictionary<TKey, TElement>());
        }

        var results = new Dictionary<TKey, TElement>(capacity);
        IEnumerator<KeyValuePair<TKey, TInputElement>> enumerator = input.GetEnumerator();
        var disposeEnumerator = true;
        try
        {
            async Task<Dictionary<TKey, TElement>?> ConvertAsync(IEnumerator<KeyValuePair<TKey, TInputElement>> asyncEnumerator,
                Task<TElement?> elementTask)
            {
                try
                {
                    var element = await elementTask.ConfigureAwait(false);

                    results.Add(asyncEnumerator.Current.Key, element!);

                    while (asyncEnumerator.MoveNext())
                    {
                        KeyValuePair<TKey, TInputElement> current = asyncEnumerator.Current;

                        elementTask = _converter.ConvertAsync(context, current.Value);
                        if (elementTask.Status == TaskStatus.RanToCompletion)
                            results.Add(current.Key, elementTask.Result!);
                        else
                        {
                            element = await elementTask.ConfigureAwait(false);

                            results.Add(asyncEnumerator.Current.Key, element!);
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
                KeyValuePair<TKey, TInputElement> current = enumerator.Current;

                Task<TElement?> elementTask = _converter.ConvertAsync(context, current.Value);
                if (elementTask.Status == TaskStatus.RanToCompletion)
                    results.Add(current.Key, elementTask.Result!);
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

        return Task.FromResult<Dictionary<TKey, TElement>?>(results);
    }
}


/// <summary>Converts dictionary property values.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
/// <typeparam name="TInputKey">The input key type.</typeparam>
/// <typeparam name="TInputElement">The input element type.</typeparam>
public class DictionaryPropertyConverter<TKey, TElement, TInputKey, TInputElement> :
    IPropertyConverter<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>,
    IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>,
    IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>,
    IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>
    where TKey : notnull
{
    readonly IPropertyConverter<TElement, TInputElement> _elementConverter;
    readonly IPropertyConverter<TKey, TInputKey> _keyConverter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="keyConverter">The key converter.</param>
    /// <param name="elementConverter">The element converter.</param>
    public DictionaryPropertyConverter(IPropertyConverter<TKey, TInputKey> keyConverter,
        IPropertyConverter<TElement, TInputElement> elementConverter)
    {
        _elementConverter = elementConverter;
        _keyConverter = keyConverter;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<Dictionary<TKey, TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.Collections.Generic.Dictionary<TKey, TElement>?>(cancellationToken); return ConvertSyncAsync(context, input);
    }

    Task<IDictionary<TKey, TElement>?> IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>
        .ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IDictionary<TKey, TElement>?>(resultTask.Result);

        async Task<IDictionary<TKey, TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IEnumerable<KeyValuePair<TKey, TElement>>?> IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>>>.ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IEnumerable<KeyValuePair<TKey, TElement>>?>(resultTask.Result);

        async Task<IEnumerable<KeyValuePair<TKey, TElement>>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IReadOnlyDictionary<TKey, TElement>?>
        IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IReadOnlyDictionary<TKey, TElement>?>(resultTask.Result);

        async Task<IReadOnlyDictionary<TKey, TElement>?> ConvertAsync()
        {
            return await resultTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<Dictionary<TKey, TElement>?> ConvertSyncAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<Dictionary<TKey, TElement>>();

        var capacity = 0;
        if (input is ICollection<TElement> collection)
        {
            capacity = collection.Count;
            if (capacity == 0)
                return Task.FromResult<Dictionary<TKey, TElement>?>(new Dictionary<TKey, TElement>());
        }

        var results = new Dictionary<TKey, TElement>(capacity);
        IEnumerator<KeyValuePair<TInputKey, TInputElement>> enumerator = input.GetEnumerator();
        var disposeEnumerator = true;
        try
        {
            async Task<Dictionary<TKey, TElement>?> ConvertAsync(IEnumerator<KeyValuePair<TInputKey, TInputElement>> asyncEnumerator,
                Task<TKey?> keyTask, Task<TElement?> elementTask)
            {
                try
                {
                    var key = keyTask.Status == TaskStatus.RanToCompletion ? keyTask.Result : await keyTask.ConfigureAwait(false);
                    var element = elementTask.Status == TaskStatus.RanToCompletion ? elementTask.Result : await elementTask.ConfigureAwait(false);

                    results.Add(RequireKey(key), element!);

                    while (asyncEnumerator.MoveNext())
                    {
                        KeyValuePair<TInputKey, TInputElement> current = asyncEnumerator.Current;

                        keyTask = _keyConverter.ConvertAsync(context, current.Key);
                        elementTask = _elementConverter.ConvertAsync(context, current.Value);

                        key = keyTask.IsCompleted ? keyTask.Result : await keyTask.ConfigureAwait(false);
                        element = elementTask.IsCompleted ? elementTask.Result : await elementTask.ConfigureAwait(false);

                        results.Add(RequireKey(key), element!);
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
                KeyValuePair<TInputKey, TInputElement> current = enumerator.Current;

                Task<TKey?> keyTask = _keyConverter.ConvertAsync(context, current.Key);
                Task<TElement?> elementTask = _elementConverter.ConvertAsync(context, current.Value);
                if (keyTask.Status == TaskStatus.RanToCompletion && elementTask.Status == TaskStatus.RanToCompletion)
                    results.Add(RequireKey(keyTask.Result), elementTask.Result!);
                else
                {
                    disposeEnumerator = false;
                    return ConvertAsync(enumerator, keyTask, elementTask);
                }
            }
        }
        finally
        {
            if (disposeEnumerator)
                enumerator.Dispose();
        }

        return Task.FromResult<Dictionary<TKey, TElement>?>(results);
    }

    static TKey RequireKey(TKey? key)
    {
        return key ?? throw new InvalidOperationException("A dictionary key converter returned null.");
    }
}
