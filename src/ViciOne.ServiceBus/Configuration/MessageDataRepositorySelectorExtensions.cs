using System;
using System.IO;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects built-in repository implementations for message-data storage.</summary>
public static class MessageDataRepositorySelectorExtensions
{
    /// <summary>Selects a process-local repository whose entries support optional expiration.</summary>
    /// <param name="selector">The repository selector for the owning bus.</param>
    /// <returns>A new in-memory repository.</returns>
    public static IMessageDataRepository UseInMemory(this IMessageDataRepositorySelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new InMemoryMessageDataRepository();
    }

    /// <summary>Selects a file-system repository constrained to one root directory.</summary>
    /// <param name="selector">The repository selector for the owning bus.</param>
    /// <param name="path">The non-empty repository root path.</param>
    /// <returns>A new file-system repository.</returns>
    public static IMessageDataRepository UseFileSystem(this IMessageDataRepositorySelector selector, string path)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new FileSystemMessageDataRepository(new DirectoryInfo(path));
    }

    /// <summary>Wraps one selected repository with authenticated message-data encryption.</summary>
    /// <param name="selector">The repository selector for the owning bus.</param>
    /// <param name="keyProvider">The provider that selects current and historical encryption keys.</param>
    /// <param name="maximumObjectBytes">The hard upper bound for one plaintext message-data object.</param>
    /// <param name="innerSelector">The callback that selects the repository used for encrypted bytes.</param>
    /// <returns>An encrypted repository backed by the selected inner repository.</returns>
    public static IMessageDataRepository UseEncryption(
        this IMessageDataRepositorySelector selector,
        IEncryptionKeyProvider keyProvider,
        int maximumObjectBytes,
        Func<IMessageDataRepositorySelector, IMessageDataRepository> innerSelector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumObjectBytes, 1);
        ArgumentNullException.ThrowIfNull(innerSelector);

        var innerRepository = innerSelector(selector)
            ?? throw new InvalidOperationException("The inner message-data repository selector returned null.");
        if (innerRepository is EncryptedMessageDataRepository)
            throw new ArgumentException("An encrypted message-data repository cannot wrap another encrypted repository.", nameof(innerSelector));

        if (innerRepository is IBusObserver observer)
            selector.Configurator.ConnectBusObserver(observer);

        return new EncryptedMessageDataRepository(innerRepository, keyProvider, maximumObjectBytes);
    }
}
