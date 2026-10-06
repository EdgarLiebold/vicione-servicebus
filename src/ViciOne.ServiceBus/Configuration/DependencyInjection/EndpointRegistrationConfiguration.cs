using System;
using System.Reflection;
using ViciOne.ServiceBus.Providers.Configuration;

namespace ViciOne.ServiceBus.Configuration;

internal static class EndpointRegistrationConfiguration
{
    internal static void RequireDefaultDispatch(object registration, Type contract, Type implementation,
        IRegistrationContext context, string feature)
    {
        InterfaceMapping mapping = registration.GetType().GetInterfaceMap(contract);
        for (int index = 0; index < mapping.InterfaceMethods.Length; index++)
        {
            if (mapping.InterfaceMethods[index].Name == "Configure"
                && mapping.TargetMethods[index].DeclaringType == implementation)
                return;
        }

        throw Unsupported(registration, context, feature);
    }

    internal static ConfigurationException Unsupported(object registration, IRegistrationContext context, string feature)
        => new(ConfigurationMessages.Create(feature,
            context is IBusRegistrationIdentity identity ? identity.BusKey : "unknown",
            $"Registration '{TypeCache.GetShortName(registration.GetType())}' cannot apply an endpoint-local callback safely",
            "Use the built-in registration for endpoint-local callbacks, or register shared configuration with AddConfigureAction and configure the endpoint without a local callback"));
}
