using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds one bounded message journal within its owning bus registration.</summary>
public interface IMessageJournalConfigurator
{
    /// <summary>Selects the persistence store.</summary>
    /// <param name="store">The store.</param>
    /// <returns>The configured store.</returns>
    IMessageJournalConfigurator UseStore(IMessageJournalStore store);

    /// <summary>Selects the mandatory sanitization and inclusion policy.</summary>
    /// <param name="policy">The policy.</param>
    /// <returns>The message journal configurator produced by the operation.</returns>
    IMessageJournalConfigurator Policy(IMessageJournalPolicy policy);

    /// <summary>Selects finite runtime and timeout options.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The message journal configurator produced by the operation.</returns>
    IMessageJournalConfigurator Options(MessageJournalOptions options);
}
