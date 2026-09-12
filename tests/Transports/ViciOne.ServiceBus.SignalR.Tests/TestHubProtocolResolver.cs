using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed class TestHubProtocolResolver : IHubProtocolResolver
{
    private readonly IReadOnlyDictionary<string, IHubProtocol> _protocols;

    public TestHubProtocolResolver(IEnumerable<IHubProtocol> protocols)
    {
        ArgumentNullException.ThrowIfNull(protocols);

        _protocols = protocols.ToDictionary(protocol => protocol.Name, StringComparer.OrdinalIgnoreCase);
        AllProtocols = _protocols.Values.ToArray();

        if (AllProtocols.Count == 0)
            throw new ArgumentException("At least one SignalR protocol is required.", nameof(protocols));
    }

    public IReadOnlyList<IHubProtocol> AllProtocols { get; }

    public IHubProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols)
    {
        ArgumentNullException.ThrowIfNull(protocolName);

        return _protocols.TryGetValue(protocolName, out IHubProtocol? protocol) &&
               (supportedProtocols is null || supportedProtocols.Contains(protocolName, StringComparer.OrdinalIgnoreCase))
            ? protocol
            : null;
    }
}
