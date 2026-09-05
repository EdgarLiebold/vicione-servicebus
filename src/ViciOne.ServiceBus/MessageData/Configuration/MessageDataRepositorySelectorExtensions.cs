using System;
using System.IO;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>
/// Provides extension methods for message data repository selector.
/// </summary>
public static class MessageDataRepositorySelectorExtensions
{
    /// <summary>
    /// Performs the in memory operation.
    /// </summary>
    /// <param name="selector">The selector value.</param>
    /// <returns>The result of the operation.</returns>
    public static IMessageDataRepository InMemory(this IMessageDataRepositorySelector selector)
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        return new InMemoryMessageDataRepository();
    }

    /// <summary>
    /// Performs the file system operation.
    /// </summary>
    /// <param name="selector">The selector value.</param>
    /// <param name="path">The path value.</param>
    /// <returns>The result of the operation.</returns>
    public static IMessageDataRepository FileSystem(this IMessageDataRepositorySelector selector, string path)
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        var dataDirectory = new DirectoryInfo(path);

        return new FileSystemMessageDataRepository(dataDirectory);
    }

    /// <summary>
    /// Performs the encrypted operation.
    /// </summary>
    /// <param name="selector">The selector value.</param>
    /// <param name="keyProvider">The provider that selects current and historical encryption keys.</param>
    /// <param name="maximumObjectBytes">The hard upper bound for one plaintext message-data object.</param>
    /// <param name="innerSelector">The inner selector value.</param>
    /// <returns>The result of the operation.</returns>
    public static IMessageDataRepository Encrypted(
        this IMessageDataRepositorySelector selector,
        IEncryptionKeyProvider keyProvider,
        int maximumObjectBytes,
        Func<IMessageDataRepositorySelector, IMessageDataRepository> innerSelector)
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));
        if (keyProvider is null)
            throw new ArgumentNullException(nameof(keyProvider));
        if (maximumObjectBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumObjectBytes));
        if (innerSelector == null)
            throw new ArgumentNullException(nameof(innerSelector));

        var innerRepository = innerSelector(selector);
        if (innerRepository is EncryptedMessageDataRepository)
            throw new ArgumentException("Nesting encrypted repositories is not supported", nameof(innerSelector));

        if (innerRepository is IBusObserver observer)
            selector.Configurator.ConnectBusObserver(observer);

        return new EncryptedMessageDataRepository(innerRepository, keyProvider, maximumObjectBytes);
    }
}
