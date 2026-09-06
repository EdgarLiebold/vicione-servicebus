using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts dictionary keys while preserving their associated values.</summary>
/// <typeparam name="TKey">The target dictionary key type.</typeparam>
/// <typeparam name="TInputKey">The source dictionary key type.</typeparam>
/// <typeparam name="TElement">The dictionary value type.</typeparam>
public class DictionaryKeyPropertyConverter<TKey, TInputKey, TElement> :
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
        _converter = converter;
    }

    /// <summary>Converts the keys in the input dictionary sequence.</summary>
    /// <typeparam name="TMessage">The message contract being initialized.</typeparam>
    /// <param name="context">The active message-initialization context.</param>
    /// <param name="input">The source key/value sequence.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The converted dictionary, or <see langword="null" /> when the input is null.</returns>
    public Task<Dictionary<TKey, TElement>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TElement>>? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Dictionary<TKey, TElement>?>(cancellationToken);

        return ConvertSyncAsync(context, input);
    }

    Task<IDictionary<TKey, TElement>?> IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TElement>>? input,
            CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.IsCompleted)
            return Task.FromResult<IDictionary<TKey, TElement>?>(resultTask.Result);

        return AwaitResultAsync(resultTask);

        static async Task<IDictionary<TKey, TElement>?> AwaitResultAsync(Task<Dictionary<TKey, TElement>?> task) =>
            await task.ConfigureAwait(false);
    }

    Task<IEnumerable<KeyValuePair<TKey, TElement>>?>
        IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TInputKey, TElement>>>.ConvertAsync<TMessage>(
            InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TElement>>? input,
            CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IEnumerable<KeyValuePair<TKey, TElement>>?>(resultTask.Result);

        return AwaitResultAsync(resultTask);

        static async Task<IEnumerable<KeyValuePair<TKey, TElement>>?> AwaitResultAsync(Task<Dictionary<TKey, TElement>?> task) =>
            await task.ConfigureAwait(false);
    }

    Task<IReadOnlyDictionary<TKey, TElement>?> IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TElement>>>.
        ConvertAsync<TMessage>(InitializeContext<TMessage> context, IEnumerable<KeyValuePair<TInputKey, TElement>>? input,
            CancellationToken cancellationToken)
    {
        Task<Dictionary<TKey, TElement>?> resultTask = ConvertSyncAsync(context, input);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<IReadOnlyDictionary<TKey, TElement>?>(resultTask.Result);

        return AwaitResultAsync(resultTask);

        static async Task<IReadOnlyDictionary<TKey, TElement>?> AwaitResultAsync(Task<Dictionary<TKey, TElement>?> task) =>
            await task.ConfigureAwait(false);
    }

    Task<Dictionary<TKey, TElement>?> ConvertSyncAsync<TMessage>(InitializeContext<TMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TElement>>? input)
        where TMessage : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<Dictionary<TKey, TElement>>();

        var capacity = input is ICollection<TElement> collection ? collection.Count : 0;
        if (capacity == 0 && input is ICollection<TElement>)
            return Task.FromResult<Dictionary<TKey, TElement>?>([]);

        var results = new Dictionary<TKey, TElement>(capacity);
        IEnumerator<KeyValuePair<TInputKey, TElement>> enumerator = input.GetEnumerator();
        var disposeEnumerator = true;
        try
        {
            while (enumerator.MoveNext())
            {
                KeyValuePair<TInputKey, TElement> current = enumerator.Current;
                Task<TKey?> keyTask = _converter.ConvertAsync(context, current.Key);
                if (keyTask.Status == TaskStatus.RanToCompletion)
                    results.Add(RequireKey(keyTask.Result), current.Value);
                else
                {
                    disposeEnumerator = false;
                    return CompleteAsync(enumerator, keyTask, results, context);
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
        InitializeContext<TMessage> context)
        where TMessage : class
    {
        try
        {
            results.Add(RequireKey(await keyTask.ConfigureAwait(false)), enumerator.Current.Value);
            while (enumerator.MoveNext())
            {
                KeyValuePair<TInputKey, TElement> current = enumerator.Current;
                keyTask = _converter.ConvertAsync(context, current.Key);
                results.Add(RequireKey(await keyTask.ConfigureAwait(false)), current.Value);
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
}
