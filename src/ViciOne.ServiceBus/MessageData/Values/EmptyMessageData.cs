using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

public class EmptyMessageData<T> :
    MessageData<T>
{
    public static readonly MessageData<T> Instance = new EmptyMessageData<T>();

    EmptyMessageData()
    {
    }

    public Uri Address => throw new MessageDataException("The message data is empty");

    public bool HasValue => false;

    public Task<T?> Value => NoValueAsync();

    static Task<T?> NoValueAsync()
    {
        throw new MessageDataException("The message data is empty");
    }
}
