namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Represents the method that handles load balancer factory.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="consumers">The consumers value.</param>
/// <returns>The result of the operation.</returns>
public delegate IReceiverLoadBalancer<T> LoadBalancerFactory<T>(IMessageReceiver<T>[] consumers)
    where T : class;
