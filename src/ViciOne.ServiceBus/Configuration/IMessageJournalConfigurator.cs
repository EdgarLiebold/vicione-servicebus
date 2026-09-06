using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds one bounded message journal within its owning bus registration.</summary>
public interface IMessageJournalConfigurator
{
    /// <summary>Selects the persistence store.</summary>
    IMessageJournalConfigurator UseStore(IMessageJournalStore store);

    /// <summary>Selects the mandatory sanitization and inclusion policy.</summary>
    IMessageJournalConfigurator Policy(IMessageJournalPolicy policy);

    /// <summary>Selects finite runtime and timeout options.</summary>
    IMessageJournalConfigurator Options(MessageJournalOptions options);
}
