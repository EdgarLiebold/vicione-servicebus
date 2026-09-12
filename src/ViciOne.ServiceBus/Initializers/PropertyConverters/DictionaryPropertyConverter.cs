using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Adapts a key/value sequence to the requested dictionary-shaped contract without changing entries.</summary>
/// <typeparam name="TKey">The dictionary key type.</typeparam>
/// <typeparam name="TElement">The dictionary value type.</typeparam>
internal sealed class DictionaryPropertyConverter<TKey, TElement> :
    IPropertyConverter<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TElement>>>,
    IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TElement>>>,
    IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TElement>>>,
    IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TKey, TElement>>>
    where TKey : notnull
{
    /// <inheritdoc />
    public Task<Dictionary<TKey, TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TKey, TElement>>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Dictionary<TKey, TElement>?>(cancellationToken);

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
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IDictionary<TKey, TElement>?>(cancellationToken);

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
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IEnumerable<KeyValuePair<TKey, TElement>>?>(cancellationToken);

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
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IReadOnlyDictionary<TKey, TElement>?>(cancellationToken);

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


/// <summary>Converts each source value while preserving dictionary keys.</summary>
/// <typeparam name="TKey">The dictionary key type.</typeparam>
/// <typeparam name="TElement">The result value type.</typeparam>
/// <typeparam name="TInputElement">The source value type.</typeparam>
internal sealed class DictionaryPropertyConverter<TKey, TElement, TInputElement> :
    IPropertyConverter<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>,
    IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>,
    IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>,
    IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TKey, TInputElement>>>
    where TKey : notnull
{
    readonly IPropertyConverter<TElement, TInputElement> _converter;

    /// <summary>Creates a dictionary converter that applies <paramref name="converter" /> to every value.</summary>
    /// <param name="converter">The value conversion.</param>
    public DictionaryPropertyConverter(IPropertyConverter<TElement, TInputElement> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <inheritdoc />
    public Task<Dictionary<TKey, TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Dictionary<TKey, TElement>?>(cancellationToken);

        return ConvertCoreAsync(context, input, cancellationToken);
    }

    Task<IDictionary<TKey, TElement>?> IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IDictionary<TKey, TElement>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IDictionary<TKey, TElement>?>(resultTask.GetAwaiter().GetResult());

        async Task<IDictionary<TKey, TElement>?> ConvertAsync()
        {
            return await resultTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IEnumerable<KeyValuePair<TKey, TElement>>?>
        IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TKey, TInputElement>>>.ConvertAsync<TMessage>(
            InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IEnumerable<KeyValuePair<TKey, TElement>>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IEnumerable<KeyValuePair<TKey, TElement>>?>(resultTask.GetAwaiter().GetResult());

        async Task<IEnumerable<KeyValuePair<TKey, TElement>>?> ConvertAsync()
        {
            return await resultTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IReadOnlyDictionary<TKey, TElement>?> IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TKey, TInputElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IReadOnlyDictionary<TKey, TElement>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IReadOnlyDictionary<TKey, TElement>?>(resultTask.GetAwaiter().GetResult());

        async Task<IReadOnlyDictionary<TKey, TElement>?> ConvertAsync()
        {
            return await resultTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<Dictionary<TKey, TElement>?> ConvertCoreAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TKey, TInputElement>>? input, CancellationToken cancellationToken)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<Dictionary<TKey, TElement>>();

        var capacity = 0;
        if (input is ICollection<KeyValuePair<TKey, TInputElement>> collection)
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
                    var element = await elementTask.WaitAsync(cancellationToken).ConfigureAwait(false);

                    results.Add(asyncEnumerator.Current.Key, element!);

                    while (asyncEnumerator.MoveNext())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        KeyValuePair<TKey, TInputElement> current = asyncEnumerator.Current;

                        elementTask = ConvertElementAsync(context, current.Value, cancellationToken);
                        if (elementTask.IsCompletedSuccessfully)
                            results.Add(current.Key, elementTask.GetAwaiter().GetResult()!);
                        else
                        {
                            element = await elementTask.WaitAsync(cancellationToken).ConfigureAwait(false);

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
                cancellationToken.ThrowIfCancellationRequested();
                KeyValuePair<TKey, TInputElement> current = enumerator.Current;

                Task<TElement?> elementTask = ConvertElementAsync(context, current.Value, cancellationToken);
                if (elementTask.IsCompletedSuccessfully)
                    results.Add(current.Key, elementTask.GetAwaiter().GetResult()!);
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

    Task<TElement?> ConvertElementAsync<TMessage>(InitializeContext<TMessage> context, TInputElement input,
        CancellationToken cancellationToken)
        where TMessage : class => _converter.ConvertAsync(context, input, cancellationToken)
            ?? throw new InvalidOperationException("The dictionary element converter returned a null task.");
}


/// <summary>Converts every key and value in a source key/value sequence.</summary>
/// <typeparam name="TKey">The result key type.</typeparam>
/// <typeparam name="TElement">The result value type.</typeparam>
/// <typeparam name="TInputKey">The source key type.</typeparam>
/// <typeparam name="TInputElement">The source value type.</typeparam>
internal sealed class DictionaryPropertyConverter<TKey, TElement, TInputKey, TInputElement> :
    IPropertyConverter<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>,
    IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>,
    IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>,
    IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>
    where TKey : notnull
{
    readonly IPropertyConverter<TElement, TInputElement> _elementConverter;
    readonly IPropertyConverter<TKey, TInputKey> _keyConverter;

    /// <summary>Creates a dictionary converter with independent key and value conversions.</summary>
    /// <param name="keyConverter">The key conversion.</param>
    /// <param name="elementConverter">The value conversion.</param>
    public DictionaryPropertyConverter(IPropertyConverter<TKey, TInputKey> keyConverter,
        IPropertyConverter<TElement, TInputElement> elementConverter)
    {
        _elementConverter = elementConverter ?? throw new ArgumentNullException(nameof(elementConverter));
        _keyConverter = keyConverter ?? throw new ArgumentNullException(nameof(keyConverter));
    }

    /// <inheritdoc />
    public Task<Dictionary<TKey, TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Dictionary<TKey, TElement>?>(cancellationToken);

        return ConvertCoreAsync(context, input, cancellationToken);
    }

    Task<IDictionary<TKey, TElement>?> IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>
        .ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IDictionary<TKey, TElement>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IDictionary<TKey, TElement>?>(resultTask.GetAwaiter().GetResult());

        async Task<IDictionary<TKey, TElement>?> ConvertAsync()
        {
            return await resultTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IEnumerable<KeyValuePair<TKey, TElement>>?> IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>>>.ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IEnumerable<KeyValuePair<TKey, TElement>>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IEnumerable<KeyValuePair<TKey, TElement>>?>(resultTask.GetAwaiter().GetResult());

        async Task<IEnumerable<KeyValuePair<TKey, TElement>>?> ConvertAsync()
        {
            return await resultTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<IReadOnlyDictionary<TKey, TElement>?>
        IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IReadOnlyDictionary<TKey, TElement>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IReadOnlyDictionary<TKey, TElement>?>(resultTask.GetAwaiter().GetResult());

        async Task<IReadOnlyDictionary<TKey, TElement>?> ConvertAsync()
        {
            return await resultTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return ConvertAsync();
    }

    Task<Dictionary<TKey, TElement>?> ConvertCoreAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>>? input, CancellationToken cancellationToken)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<Dictionary<TKey, TElement>>();

        var capacity = 0;
        if (input is ICollection<KeyValuePair<TInputKey, TInputElement>> collection)
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
                    var key = keyTask.IsCompletedSuccessfully
                        ? keyTask.GetAwaiter().GetResult()
                        : await keyTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                    var element = elementTask.IsCompletedSuccessfully
                        ? elementTask.GetAwaiter().GetResult()
                        : await elementTask.WaitAsync(cancellationToken).ConfigureAwait(false);

                    results.Add(RequireKey(key), element!);

                    while (asyncEnumerator.MoveNext())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        KeyValuePair<TInputKey, TInputElement> current = asyncEnumerator.Current;

                        keyTask = ConvertKeyAsync(context, current.Key, cancellationToken);
                        elementTask = ConvertElementAsync(context, current.Value, cancellationToken);

                        key = keyTask.IsCompletedSuccessfully
                            ? keyTask.GetAwaiter().GetResult()
                            : await keyTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                        element = elementTask.IsCompletedSuccessfully
                            ? elementTask.GetAwaiter().GetResult()
                            : await elementTask.WaitAsync(cancellationToken).ConfigureAwait(false);

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
                cancellationToken.ThrowIfCancellationRequested();
                KeyValuePair<TInputKey, TInputElement> current = enumerator.Current;

                Task<TKey?> keyTask = ConvertKeyAsync(context, current.Key, cancellationToken);
                Task<TElement?> elementTask = ConvertElementAsync(context, current.Value, cancellationToken);
                if (keyTask.IsCompletedSuccessfully && elementTask.IsCompletedSuccessfully)
                    results.Add(RequireKey(keyTask.GetAwaiter().GetResult()), elementTask.GetAwaiter().GetResult()!);
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

    Task<TKey?> ConvertKeyAsync<TMessage>(InitializeContext<TMessage> context, TInputKey input,
        CancellationToken cancellationToken)
        where TMessage : class => _keyConverter.ConvertAsync(context, input, cancellationToken)
            ?? throw new InvalidOperationException("The dictionary key converter returned a null task.");

    Task<TElement?> ConvertElementAsync<TMessage>(InitializeContext<TMessage> context, TInputElement input,
        CancellationToken cancellationToken)
        where TMessage : class => _elementConverter.ConvertAsync(context, input, cancellationToken)
            ?? throw new InvalidOperationException("The dictionary element converter returned a null task.");
}
