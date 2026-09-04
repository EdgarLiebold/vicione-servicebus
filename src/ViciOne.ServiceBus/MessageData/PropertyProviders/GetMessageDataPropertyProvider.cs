using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

public class GetMessageDataPropertyProvider<TInput, TValue> :
    IPropertyProvider<TInput, MessageData<TValue>>
    where TInput : class
{
    readonly IPropertyProvider<TInput, MessageData<TValue>> _inputProvider;
    readonly IMessageDataReader<TValue> _reader;
    readonly IMessageDataRepository? _repository;

    public GetMessageDataPropertyProvider(IPropertyProvider<TInput, MessageData<TValue>> inputProvider, IMessageDataRepository? repository = default)
    {
        _repository = repository;
        _inputProvider = inputProvider;

        _reader = MessageDataReaderFactory.CreateReader<TValue>();
    }

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
