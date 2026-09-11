using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates a send endpoint for a normalized cache key.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <param name="key">The normalized endpoint key.</param>
/// <param name="cancellationToken">The cache-owned token that cancels endpoint creation when its owner is released.</param>
/// <returns>A task that produces the endpoint.</returns>
internal delegate Task<ISendEndpoint> SendEndpointFactory<in TKey>(TKey key, CancellationToken cancellationToken)
    where TKey : notnull;
