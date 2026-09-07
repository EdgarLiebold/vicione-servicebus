using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Combines routing-slip activity state with the underlying consume context.</summary>
public interface CourierContext :
    ActivityContext,
    ConsumeContext<RoutingSlip>;
