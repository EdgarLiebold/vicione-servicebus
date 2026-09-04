using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Factory method for a send endpoint
/// </summary>
/// <param name="key"></param>
/// <typeparam name="TKey"></typeparam>
public delegate Task<ISendEndpoint> SendEndpointFactory<in TKey>(TKey key);
