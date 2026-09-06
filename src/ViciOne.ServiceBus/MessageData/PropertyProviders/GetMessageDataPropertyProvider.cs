using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Provides get message data property services.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class GetMessageDataPropertyProvider<TInput, TValue> :
    IPropertyProvider<TInput, MessageData<TValue>>
    where TInput : class
{
    readonly IPropertyProvider<TInput, MessageData<TValue>> _inputProvider;
    readonly IMessageDataReader<TValue> _reader;
    readonly IMessageDataRepository? _repository;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="inputProvider">The input provider.</param>
    /// <param name="repository">The repository.</param>
    public GetMessageDataPropertyProvider(IPropertyProvider<TInput, MessageData<TValue>> inputProvider, IMessageDataRepository? repository = default)
    {
        _repository = repository;
        _inputProvider = inputProvider;

        _reader = MessageDataReaderFactory.CreateReader<TValue>();
    }

    /// <summary>Gets property.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<MessageData<TValue>?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!context.HasInput)
            return TaskResults.DefaultAsync<MessageData<TValue>>(cancellationToken: cancellationToken);

        Task<MessageData<TValue>?> inputTask = _inputProvider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (inputTask.IsCompleted)
        {
            MessageData<TValue>? messageData = inputTask.Result;
            if (messageData == null)
                return TaskResults.DefaultAsync<MessageData<TValue>>(cancellationToken: cancellationToken);

            if (messageData is IInlineMessageData)
                return Task.FromResult<MessageData<TValue>?>(messageData);

            if (messageData is { HasValue: true } && messageData.Address != null)
            {
                var repository = _repository;
                if (repository != null || context.TryGetPayload(out repository))
                    return Task.FromResult<MessageData<TValue>?>(_reader.GetMessageData(repository, messageData.Address, context.CancellationToken));
            }

            return Task.FromResult<MessageData<TValue>?>(EmptyMessageData<TValue>.Instance);
        }

        async Task<MessageData<TValue>?> GetPropertyAsync()
        {
            MessageData<TValue>? messageData = await inputTask.ConfigureAwait(false);
            if (messageData == null)
                return null;

            if (messageData is IInlineMessageData)
                return messageData;

            if (messageData?.Address != null)
            {
                var repository = _repository;
                if (repository != null || context.TryGetPayload(out repository))
                    return _reader.GetMessageData(repository, messageData.Address, context.CancellationToken);
            }

            return EmptyMessageData<TValue>.Instance;
        }

        return GetPropertyAsync();
    }
}
