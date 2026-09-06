using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Contributes one category of registered message handlers to endpoint materialization.</summary>
public interface IConsumerKind
{
    /// <summary>Gets a value indicating whether the category is used only when no capability-specific category claims a registration.</summary>
    bool IsFallback { get; }

    /// <summary>Gets the stable category name used to select the capability.</summary>
    string Name { get; }

    /// <summary>Gets the order in which this category contributes registrations.</summary>
    int Order { get; }

    /// <summary>Selects the endpoint contributions for the current bus registration.</summary>
    /// <param name="context">The bus-scoped registration view.</param>
    /// <returns>The endpoint contributions owned by this category.</returns>
    IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context);

    /// <summary>Contributes the registrations owned by this category to a container test harness.</summary>
    /// <param name="context">The pre-container test-harness registration view.</param>
    void ConfigureTestHarness(IConsumerKindTestHarnessContext context);
}
