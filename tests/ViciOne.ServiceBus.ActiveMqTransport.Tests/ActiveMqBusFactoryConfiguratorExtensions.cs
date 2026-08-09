// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    internal static class ActiveMqBusFactoryConfiguratorExtensions
    {
        public static void ConfigureHost(this IActiveMqBusFactoryConfigurator configurator, string testFlavor)
        {
            if (testFlavor == ArtemisFlavor)
            {
                // Artemis is a separate broker and is not part of the pinned ViciOne fixture. Its
                // endpoint stays run-scoped as well, so nothing here falls back to a fixed port.
                configurator.Host(ArtemisBroker.Address, cfgHost =>
                {
                    cfgHost.Username(ArtemisBroker.User);
                    cfgHost.Password(ArtemisBroker.Pass);
                });
                configurator.EnableArtemisCompatibility();
            }
            else if (testFlavor == ActiveMqHostAddress.AmqpScheme)
            {
                configurator.Host(RunScopedBroker.AddressFor(ActiveMqHostAddress.AmqpScheme), cfgHost =>
                {
                    cfgHost.Username(RunScopedBroker.User);
                    cfgHost.Password(RunScopedBroker.Pass);
                });
            }
            else if (testFlavor == ActiveMqHostAddress.ActiveMqScheme)
            {
                // Without this branch the configurator keeps its built-in default and the spec
                // silently connects to localhost:61616, which is not the fixture the runner started.
                configurator.Host(RunScopedBroker.AddressFor(ActiveMqHostAddress.ActiveMqScheme), cfgHost =>
                {
                    cfgHost.Username(RunScopedBroker.User);
                    cfgHost.Password(RunScopedBroker.Pass);
                });
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(testFlavor), testFlavor,
                    "Unknown broker flavor. A spec may not fall through to a default endpoint.");
            }
        }

        /// <summary>Name of the Artemis flavor used by the parameterized specs.</summary>
        public const string ArtemisFlavor = "artemis";
    }
}
