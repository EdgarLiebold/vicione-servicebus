namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Controls how much historical test-context data is retained for later assertions.
/// Live observers and inactivity tracking remain active regardless of this setting.
/// </summary>
public enum TestContextSaveMode
{
    /// <summary>Retain every observed context for the lifetime of the harness.</summary>
    All,

    /// <summary>Retain only the most recent <see cref="TestHarnessOptions.MaximumSavedContexts"/> contexts per list.</summary>
    Bounded,

    /// <summary>Do not retain historical contexts. Use when only activity/completion observation is required.</summary>
    None
}
