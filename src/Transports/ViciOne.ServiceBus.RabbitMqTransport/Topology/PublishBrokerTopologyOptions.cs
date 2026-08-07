// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    [Flags]
    public enum PublishBrokerTopologyOptions
    {
        FlattenHierarchy = 0,
        MaintainHierarchy = 1
    }
}
