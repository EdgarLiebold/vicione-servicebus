using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for courier context.
/// </summary>
public interface CourierContext :
    ActivityContext,
    ConsumeContext<RoutingSlip>;
