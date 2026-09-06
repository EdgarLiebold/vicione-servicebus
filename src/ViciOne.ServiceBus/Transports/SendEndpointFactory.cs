using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Factory method for a send endpoint.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <param name="key">The key used to identify the requested entry.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task<ISendEndpoint> SendEndpointFactory<in TKey>(TKey key);
