using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for courier operations.</summary>
public interface CourierContext :
    ActivityContext,
    ConsumeContext<RoutingSlip>;
