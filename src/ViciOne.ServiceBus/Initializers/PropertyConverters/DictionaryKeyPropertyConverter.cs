using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts dictionary keys while preserving their associated values.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TInputKey">The input key type.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
internal sealed class DictionaryKeyPropertyConverter<TKey, TInputKey, TElement> :
    IPropertyConverter<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TElement>>>,
    IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TElement>>>,
    IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TElement>>>,
    IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TInputKey, TElement>>>
    where TKey : notnull
{
    readonly IPropertyConverter<TKey, TInputKey> _converter;

    /// <summary>Initializes the converter with the key conversion strategy.</summary>
    /// <param name="converter">The converter used for every source key.</param>
    public DictionaryKeyPropertyConverter(IPropertyConverter<TKey, TInputKey> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <inheritdoc />
    public Task<Dictionary<TKey, TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TElement>>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Dictionary<TKey, TElement>?>(cancellationToken);

        return ConvertCoreAsync(context, input, cancellationToken);
    }

    Task<IDictionary<TKey, TElement>?> IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TElement>>? input,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IDictionary<TKey, TElement>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IDictionary<TKey, TElement>?>(resultTask.GetAwaiter().GetResult());

        return AwaitResultAsync(resultTask, cancellationToken);

        static async Task<IDictionary<TKey, TElement>?> AwaitResultAsync(Task<Dictionary<TKey, TElement>?> task,
            CancellationToken token) => await task.WaitAsync(token).ConfigureAwait(false);
    }

    Task<IEnumerable<KeyValuePair<TKey, TElement>>?>
        IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TInputKey, TElement>>>.ConvertAsync<TMessage>(
            InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TElement>>? input,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IEnumerable<KeyValuePair<TKey, TElement>>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IEnumerable<KeyValuePair<TKey, TElement>>?>(resultTask.GetAwaiter().GetResult());

        return AwaitResultAsync(resultTask, cancellationToken);

        static async Task<IEnumerable<KeyValuePair<TKey, TElement>>?> AwaitResultAsync(Task<Dictionary<TKey, TElement>?> task,
            CancellationToken token) => await task.WaitAsync(token).ConfigureAwait(false);
    }

    Task<IReadOnlyDictionary<TKey, TElement>?> IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TElement>>? input,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IReadOnlyDictionary<TKey, TElement>?>(cancellationToken);

        Task<Dictionary<TKey, TElement>?> resultTask = ConvertCoreAsync(context, input, cancellationToken);
        if (resultTask.IsCompletedSuccessfully)
            return Task.FromResult<IReadOnlyDictionary<TKey, TElement>?>(resultTask.GetAwaiter().GetResult());

        return AwaitResultAsync(resultTask, cancellationToken);

        static async Task<IReadOnlyDictionary<TKey, TElement>?> AwaitResultAsync(Task<Dictionary<TKey, TElement>?> task,
            CancellationToken token) => await task.WaitAsync(token).ConfigureAwait(false);
    }

    Task<Dictionary<TKey, TElement>?> ConvertCoreAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TElement>>? input, CancellationToken cancellationToken)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<Dictionary<TKey, TElement>>();

        var capacity = input is ICollection<KeyValuePair<TInputKey, TElement>> collection ? collection.Count : 0;
        if (capacity == 0 && input is ICollection<KeyValuePair<TInputKey, TElement>>)
            return Task.FromResult<Dictionary<TKey, TElement>?>([]);

        var results = new Dictionary<TKey, TElement>(capacity);
        IEnumerator<KeyValuePair<TInputKey, TElement>> enumerator = input.GetEnumerator();
        var disposeEnumerator = true;
        try
        {
            while (enumerator.MoveNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                KeyValuePair<TInputKey, TElement> current = enumerator.Current;
                Task<TKey?> keyTask = ConvertKeyAsync(context, current.Key, cancellationToken);
                if (keyTask.IsCompletedSuccessfully)
                    results.Add(RequireKey(keyTask.GetAwaiter().GetResult()), current.Value);
                else
                {
                    disposeEnumerator = false;
                    return CompleteAsync(enumerator, keyTask, results, context, cancellationToken);
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

    async Task<Dictionary<TKey, TElement>?> CompleteAsync<TMessage>(
        IEnumerator<KeyValuePair<TInputKey, TElement>> enumerator,
        Task<TKey?> keyTask,
        Dictionary<TKey, TElement> results,
        InitializeContext<TMessage> context,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        try
        {
            results.Add(RequireKey(await keyTask.WaitAsync(cancellationToken).ConfigureAwait(false)), enumerator.Current.Value);
            while (enumerator.MoveNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                KeyValuePair<TInputKey, TElement> current = enumerator.Current;
                keyTask = ConvertKeyAsync(context, current.Key, cancellationToken);
                results.Add(RequireKey(await keyTask.WaitAsync(cancellationToken).ConfigureAwait(false)), current.Value);
            }

            return results;
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    static TKey RequireKey(TKey? key) =>
        key ?? throw new InvalidOperationException("A dictionary key converter returned null.");

    Task<TKey?> ConvertKeyAsync<TMessage>(InitializeContext<TMessage> context, TInputKey input,
        CancellationToken cancellationToken)
        where TMessage : class => _converter.ConvertAsync(context, input, cancellationToken)
            ?? throw new InvalidOperationException("The dictionary key converter returned a null task.");
}
