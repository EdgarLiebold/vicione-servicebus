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
    public static Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string value,
        CancellationToken cancellationToken = default)
    {
        return PutStringAsync(repository, value, default, MessageDataPolicy.Default, cancellationToken);
    }

    public static Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string value, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutStringAsync(repository, value, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    public static async Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string value, TimeSpan? timeToLive,
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

        var address = await repository.PutAsync(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytesCount < policy.Threshold)
            return new StringInlineMessageData(value, address);

        return new StoredMessageData<string>(address, value);
    }

    public static Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        return PutBytesAsync(repository, bytes, default, MessageDataPolicy.Default, cancellationToken);
    }

    public static Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[] bytes, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutBytesAsync(repository, bytes, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    public static async Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[] bytes, TimeSpan? timeToLive,
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

        var address = await repository.PutAsync(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytes.Length < policy.Threshold)
            return new BytesInlineMessageData(bytes, address);

        return new StoredMessageData<byte[]>(address, bytes);
    }

    public static Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object value, Type objectType,
        CancellationToken cancellationToken =
            default)
    {
        return PutObjectAsync(repository, value, objectType, default, MessageDataPolicy.Default, cancellationToken);
    }

    public static Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object value, Type objectType, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutObjectAsync(repository, value, objectType, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    public static async Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object value, Type objectType, TimeSpan? timeToLive,
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

        var address = await repository.PutAsync(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytes.Length < policy.Threshold)
            return new BytesInlineMessageData(bytes, address);

        return new StoredMessageData<byte[]>(address, bytes);
    }

    public static Task<MessageData<Stream>> PutStreamAsync(this IMessageDataRepository repository, Stream stream,
        CancellationToken cancellationToken = default)
    {
        return PutStreamAsync(repository, stream, default, cancellationToken);
    }

    public static async Task<MessageData<Stream>> PutStreamAsync(this IMessageDataRepository repository, Stream stream, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (stream == null)
            return EmptyMessageData<Stream>.Instance;

        var address = await repository.PutAsync(stream, timeToLive, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<Stream>(address, stream);
    }

    public static async Task<MessageData<string>> GetStringAsync(this IMessageDataRepository repository, Uri address,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        using var ms = new MemoryStream();

        using var stream = await repository.GetAsync(address, cancellationToken).ConfigureAwait(false);

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<string>(address, Encoding.UTF8.GetString(ms.ToArray()));
    }

    public static async Task<MessageData<byte[]>> GetBytesAsync(this IMessageDataRepository repository, Uri address,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        using var ms = new MemoryStream();

        using var stream = await repository.GetAsync(address, cancellationToken).ConfigureAwait(false);

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<byte[]>(address, ms.ToArray());
    }
}
