using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus;

public static class MessageDataExtensions
{
    public static Task<MessageData<string>> PutString(this IMessageDataRepository repository, string value,
        CancellationToken cancellationToken = default)
    {
        return PutString(repository, value, default, MessageDataPolicy.Default, cancellationToken);
    }

    public static Task<MessageData<string>> PutString(this IMessageDataRepository repository, string value, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutString(repository, value, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    public static async Task<MessageData<string>> PutString(this IMessageDataRepository repository, string value, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));
        if (value == null)
            return EmptyMessageData<string>.Instance;

        var bytesCount = Encoding.UTF8.GetByteCount(value);
        if (bytesCount < policy.Threshold && !policy.AlwaysWriteToRepository)
            return new StringInlineMessageData(value);

        var bytes = Encoding.UTF8.GetBytes(value);

        using var ms = new MemoryStream(bytes, false);

        var address = await repository.Put(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytesCount < policy.Threshold)
            return new StringInlineMessageData(value, address);

        return new StoredMessageData<string>(address, value);
    }

    public static Task<MessageData<byte[]>> PutBytes(this IMessageDataRepository repository, byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        return PutBytes(repository, bytes, default, MessageDataPolicy.Default, cancellationToken);
    }

    public static Task<MessageData<byte[]>> PutBytes(this IMessageDataRepository repository, byte[] bytes, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutBytes(repository, bytes, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    public static async Task<MessageData<byte[]>> PutBytes(this IMessageDataRepository repository, byte[] bytes, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));
        if (bytes == null)
            return EmptyMessageData<byte[]>.Instance;

        if (bytes.Length < policy.Threshold && !policy.AlwaysWriteToRepository)
            return new BytesInlineMessageData(bytes);

        using var ms = new MemoryStream(bytes, false);

        var address = await repository.Put(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytes.Length < policy.Threshold)
            return new BytesInlineMessageData(bytes, address);

        return new StoredMessageData<byte[]>(address, bytes);
    }

    public static Task<IMessageData> PutObject(this IMessageDataRepository repository, object value, Type objectType,
        CancellationToken cancellationToken =
            default)
    {
        return PutObject(repository, value, objectType, default, MessageDataPolicy.Default, cancellationToken);
    }

    public static Task<IMessageData> PutObject(this IMessageDataRepository repository, object value, Type objectType, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutObject(repository, value, objectType, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    public static async Task<IMessageData> PutObject(this IMessageDataRepository repository, object value, Type objectType, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));
        if (value == null)
            return EmptyMessageData<byte[]>.Instance;

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, objectType, ServiceBusMetadataJson.Options);

        if (bytes.Length < policy.Threshold && !policy.AlwaysWriteToRepository)
            return new BytesInlineMessageData(bytes);

        using var ms = new MemoryStream(bytes, false);

        var address = await repository.Put(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytes.Length < policy.Threshold)
            return new BytesInlineMessageData(bytes, address);

        return new StoredMessageData<byte[]>(address, bytes);
    }

    public static Task<MessageData<Stream>> PutStream(this IMessageDataRepository repository, Stream stream,
        CancellationToken cancellationToken = default)
    {
        return PutStream(repository, stream, default, cancellationToken);
    }

    public static async Task<MessageData<Stream>> PutStream(this IMessageDataRepository repository, Stream stream, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (stream == null)
            return EmptyMessageData<Stream>.Instance;

        var address = await repository.Put(stream, timeToLive, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<Stream>(address, stream);
    }

    public static async Task<MessageData<string>> GetString(this IMessageDataRepository repository, Uri address,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        using var ms = new MemoryStream();

        using var stream = await repository.Get(address, cancellationToken).ConfigureAwait(false);

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<string>(address, Encoding.UTF8.GetString(ms.ToArray()));
    }

    public static async Task<MessageData<byte[]>> GetBytes(this IMessageDataRepository repository, Uri address,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        using var ms = new MemoryStream();

        using var stream = await repository.Get(address, cancellationToken).ConfigureAwait(false);

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<byte[]>(address, ms.ToArray());
    }
}
