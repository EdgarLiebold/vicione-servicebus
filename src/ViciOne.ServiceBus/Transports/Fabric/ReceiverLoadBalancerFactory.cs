namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Creates a load balancer for the currently connected receivers.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <param name="receivers">The connected receivers.</param>
/// <returns>A load balancer over <paramref name="receivers" />.</returns>
internal delegate IReceiverLoadBalancer<TMessage> ReceiverLoadBalancerFactory<TMessage>(IMessageReceiver<TMessage>[] receivers)
    where TMessage : class;
