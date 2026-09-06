namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Represents the method that handles load balancer factory.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="consumers">The consumers.</param>
/// <returns>The value produced by the operation.</returns>
public delegate IReceiverLoadBalancer<T> LoadBalancerFactory<T>(IMessageReceiver<T>[] consumers)
    where T : class;
