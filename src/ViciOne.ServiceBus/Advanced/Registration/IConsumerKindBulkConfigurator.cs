using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Configures every registration owned by a consumer kind on an existing endpoint.</summary>
public interface IConsumerKindBulkConfigurator
{
    /// <summary>Configures registrations that have not already been claimed by another consumer kind.</summary>
    /// <param name="endpointConfigurator">The endpoint to configure.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <param name="excludedRegistrationTypes">Registration types that were already configured.</param>
    /// <returns>The registration types configured by this invocation.</returns>
    IReadOnlyCollection<Type> ConfigureAll(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, IReadOnlySet<Type> excludedRegistrationTypes);
}
