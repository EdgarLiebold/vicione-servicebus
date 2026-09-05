using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Tests.MessageData;

internal sealed class TestEncryptionKeyProvider : IEncryptionKeyProvider
{
    private readonly Dictionary<string, EncryptionKey> _keys = new(StringComparer.Ordinal);
    private EncryptionKey _current;

    public TestEncryptionKeyProvider(string keyId = "default", byte seed = 1)
    {
        _current = CreateKey(keyId, seed);
        _keys.Add(keyId, _current);
    }

    public EncryptionKey GetCurrentKey() => _current;

    public bool TryGetKey(string keyId, out EncryptionKey? key) => _keys.TryGetValue(keyId, out key);

    public void Remove(string keyId) => _keys.Remove(keyId);

    public void Rotate(string keyId, byte seed, bool retainPrevious = true)
    {
        if (!retainPrevious)
            _keys.Clear();

        _current = CreateKey(keyId, seed);
        _keys[keyId] = _current;
    }

    private static EncryptionKey CreateKey(string keyId, byte seed) =>
        new(keyId, Enumerable.Range(seed, 32).Select(static value => checked((byte)value)).ToArray());
}
