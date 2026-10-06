using System.Buffers.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Serialization;

if (args.Length != 1)
    throw new ArgumentException("Use exactly one mode: s3-prefix, key-id, pre-auth.");

return args[0] switch
{
    "s3-prefix" => CheckS3Prefix(),
    "key-id" => await CheckKeyIdAsync(),
    "pre-auth" => await ObservePreAuthLookupAsync(),
    "key-boundaries" => await CheckKeyBoundariesAsync(),
    "contract-boundaries" => CheckContractBoundaries(),
    _ => throw new ArgumentException("Unknown review mode.")
};

static int CheckS3Prefix()
{
    const string validName = "vicione-review-control";
    var valid = new AmazonS3MessageDataRepositoryOptions(validName);
    Require(valid.BucketName == validName, "Valid bucket name was not preserved.");
    try
    {
        var invalid = new AmazonS3MessageDataRepositoryOptions("amzn-s3-demo-review");
        Emit(new { mode = "s3-prefix", valid_control = true, accepted_reserved_name = invalid.BucketName,
            contract = "FAIL", proof = "Actual public constructor execution" });
        return 2;
    }
    catch (ArgumentException exception) when (exception.ParamName == "bucketName")
    {
        Emit(new { mode = "s3-prefix", valid_control = true, reserved_prefix_rejected = true, contract = "PASS" });
        return 0;
    }
}

static async Task<int> CheckKeyIdAsync()
{
    byte[] expected = [0, 1, 127, 128, 254, 255];
    var validProvider = new StableKeyProvider(new EncryptionKey("key-Ω-😀", SyntheticKey()));
    var validRepository = new EncryptedMessageDataRepository(new InMemoryMessageDataRepository(), validProvider, 64);
    await using var validSource = new MemoryStream(expected, writable: false);
    Uri validAddress = await validRepository.PutAsync(validSource);
    await using Stream validResult = await validRepository.GetAsync(validAddress);
    Require((await ReadAsync(validResult)).SequenceEqual(expected), "Valid Unicode key-id roundtrip failed.");
    Require(validProvider.Lookups.SequenceEqual([validProvider.Current.KeyId]), "Valid key identifier changed.");

    const string malformed = "key-\uD800";
    EncryptionKey key;
    try { key = new EncryptionKey(malformed, SyntheticKey()); }
    catch (ArgumentException)
    {
        Emit(new { mode = "key-id", valid_control = true, malformed_rejected_before_storage = true, contract = "PASS" });
        return 0;
    }

    var provider = new StableKeyProvider(key);
    var repository = new EncryptedMessageDataRepository(new InMemoryMessageDataRepository(), provider, 64);
    await using var source = new MemoryStream(expected, writable: false);
    Uri address = await repository.PutAsync(source);
    try
    {
        await using Stream result = await repository.GetAsync(address);
        Require((await ReadAsync(result)).SequenceEqual(expected), "Accepted key-id did not preserve payload bytes.");
        Require(provider.Lookups.SequenceEqual([malformed]), "Accepted key identifier changed.");
        Emit(new { mode = "key-id", valid_control = true, accepted_identifier_preserved = true, contract = "PASS" });
        return 0;
    }
    catch (SerializationException exception)
    {
        Require(provider.Lookups.Count == 1, "Failure did not traverse the historical-key lookup boundary.");
        Emit(new { mode = "key-id", valid_control = true, write_completed = true,
            input_utf16 = CodeUnits(malformed), looked_up_utf16 = CodeUnits(provider.Lookups.Single()),
            read_failure_type = exception.GetType().FullName, contract = "FAIL" });
        return 2;
    }
}

static async Task<int> ObservePreAuthLookupAsync()
{
    var provider = new StableKeyProvider(new EncryptionKey("key-A", SyntheticKey()));
    IMessageDataRepository inner = new InMemoryMessageDataRepository();
    var repository = new EncryptedMessageDataRepository(inner, provider, 64);
    byte[] expected = [1, 2, 3];
    await using var source = new MemoryStream(expected, writable: false);
    Uri originalAddress = await repository.PutAsync(source);
    await using (Stream control = await repository.GetAsync(originalAddress))
        Require((await ReadAsync(control)).SequenceEqual(expected), "Untampered authenticated control failed.");
    provider.Lookups.Clear();
    await using Stream envelopeStream = await inner.GetAsync(originalAddress);
    byte[] envelope = await ReadAsync(envelopeStream);
    Require(envelope.AsSpan(0, 4).SequenceEqual("VOSB"u8), "Unexpected envelope format.");
    Require(envelope[4] == 1, "Unexpected envelope version.");
    int length = BinaryPrimitives.ReadUInt16BigEndian(envelope.AsSpan(5, 2));
    Require(length == 5, "Unexpected key identifier length.");
    "key-B"u8.CopyTo(envelope.AsSpan(7, length));
    provider.ResolveUnknownWithCurrent = true;
    await using var changedSource = new MemoryStream(envelope, writable: false);
    Uri changedAddress = await inner.PutAsync(changedSource);
    try
    {
        await using Stream unexpected = await repository.GetAsync(changedAddress);
        throw new InvalidOperationException("Manipulated associated data was accepted.");
    }
    catch (SerializationException exception) when (exception.InnerException is System.Security.Cryptography.AuthenticationTagMismatchException)
    {
        Require(provider.Lookups.SequenceEqual(["key-B"]), "Manipulated identifier was not positively observed before authentication failure.");
        Emit(new { mode = "pre-auth", valid_control = true, pre_auth_lookup = provider.Lookups.Single(),
            plaintext_not_returned = true, characterization = "PASS", trust_boundary = "Identifier is untrusted at lookup" });
        return 0;
    }
}

static byte[] SyntheticKey() => Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
static string[] CodeUnits(string value) => value.Select(character => ((int)character).ToString("X4")).ToArray();
static async Task<byte[]> ReadAsync(Stream source)
{
    using var destination = new MemoryStream();
    await source.CopyToAsync(destination);
    return destination.ToArray();
}
static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
static void Emit(object observation) => Console.WriteLine(JsonSerializer.Serialize(observation));

static async Task<int> CheckKeyBoundariesAsync()
{
    byte[] payload = [0, 1, 127, 128, 254, 255];
    string[] validNames = ["review-ascii", "review-Ω-😀", "review-\uFFFD", "\uD7FF", "\uE000",
        new string('a', ushort.MaxValue), new string('Ω', 32767) + "a",
        string.Concat(Enumerable.Repeat("😀", 16383)) + "abc"];
    int roundtrips = 0;
    foreach (string name in validNames)
    {
        byte[] bytes = SyntheticKey();
        var key = new EncryptionKey(name, bytes);
        Require(key.KeyId == name, "Valid key identifier was changed");
        var provider = new StableKeyProvider(key);
        IMessageDataRepository inner = new InMemoryMessageDataRepository();
        var repository = new EncryptedMessageDataRepository(inner, provider, 64);
        await using var source = new MemoryStream(payload, writable: false);
        Uri address = await repository.PutAsync(source);
        await using Stream envelopeStream = await inner.GetAsync(address);
        byte[] envelope = await ReadAsync(envelopeStream);
        int length = BinaryPrimitives.ReadUInt16BigEndian(envelope.AsSpan(5, 2));
        byte[] expectedId = new UTF8Encoding(false, true).GetBytes(name);
        Require(length == expectedId.Length && envelope.AsSpan(7, length).SequenceEqual(expectedId), "UTF8 envelope identifier was changed");
        await using Stream result = await repository.GetAsync(address);
        Require((await ReadAsync(result)).SequenceEqual(payload), "Valid key payload roundtrip failed");
        Require(provider.Lookups.SequenceEqual([name]), "Historical key lookup changed identifier");
        roundtrips++;
    }

    string[] malformed = ["key-\uD800", "key-\uDC00", "key-\uD800x", "key-\uDC00\uD800", "key-\uD800\uD800", "key-\uDC00x"];
    int rejected = 0;
    foreach (string name in malformed)
    {
        ArgumentException failure = Throws<ArgumentException>(() => new EncryptionKey(name, SyntheticKey()));
        Require(failure.ParamName == "keyId" && failure.InnerException is EncoderFallbackException, "Malformed key must fail at keyId with encoding cause");
        rejected++;
    }
    foreach (string name in new[] {new string('a', 65536), new string('Ω', 32768), string.Concat(Enumerable.Repeat("😀", 16384))})
    {
        ArgumentException failure = Throws<ArgumentException>(() => new EncryptionKey(name, SyntheticKey()));
        Require(failure.ParamName == "keyId", "Oversized UTF8 identifier must name keyId");
    }
    Require(Throws<ArgumentNullException>(() => new EncryptionKey(null!, SyntheticKey())).ParamName == "keyId", "Null identifier guard changed");
    Require(Throws<ArgumentException>(() => new EncryptionKey(" ", SyntheticKey())).ParamName == "keyId", "Blank identifier guard changed");

    foreach (int size in new[] {16, 24, 32})
    {
        byte[] callerMaterial = Enumerable.Range(0, size).Select(x => (byte)x).ToArray();
        byte[] originalMaterial = callerMaterial.ToArray();
        const string name = "defensive-copy-control";
        var key = new EncryptionKey(name, callerMaterial);
        Array.Fill(callerMaterial, (byte)255);
        IMessageDataRepository inner = new InMemoryMessageDataRepository();
        var writer = new EncryptedMessageDataRepository(inner, new StableKeyProvider(key), 64);
        var reader = new EncryptedMessageDataRepository(inner, new StableKeyProvider(new EncryptionKey(name, originalMaterial)), 64);
        await using var source = new MemoryStream(payload, writable: false);
        Uri address = await writer.PutAsync(source);
        await using Stream result = await reader.GetAsync(address);
        Require((await ReadAsync(result)).SequenceEqual(payload), "Caller mutation changed retained AES material");
    }
    foreach (int size in new[] {0, 15, 17, 23, 25, 31, 33})
        Require(Throws<ArgumentException>(() => new EncryptionKey("valid", new byte[size])).ParamName == "keyMaterial", "AES key-size guard changed");
    Emit(new {mode = "key-boundaries", valid_unicode_roundtrips = roundtrips, malformed_rejected = rejected,
        inclusive_utf8_bytes = ushort.MaxValue, retained_aes_sizes_and_defensive_copy = true, contract = "PASS"});
    return 0;
}

static int CheckContractBoundaries()
{
    string[] valid = ["review-ascii", "review-Übertragung-東京", "review-😀", "review-\uFFFD", "\uD7FF", "\uE000", new string('a', 256), string.Concat(Enumerable.Repeat("😀", 128))];
    foreach (string name in valid)
    {
        var identity = new MessageContractIdentity(name, ushort.MaxValue);
        Require(identity.Name == name && identity.MajorVersion == ushort.MaxValue, "Valid contract identity changed");
        string canonical = identity.ToString();
        Require(MessageContractIdentity.Parse(canonical) == identity, "Valid canonical parse changed identity");
        Require(MessageContractIdentity.TryParse(canonical, out var parsed) && parsed == identity, "Valid TryParse rejected identity");
        var attribute = new MessageContractAttribute(name, ushort.MaxValue);
        Require(attribute.Name == name && attribute.MajorVersion == ushort.MaxValue, "Valid contract attribute changed");
        var catalog = new MessageContractCatalogBuilder().Register<BoundaryMessage>(name, ushort.MaxValue).Build();
        Require(catalog.GetIdentity(typeof(BoundaryMessage)) == identity && catalog.GetMessageType(identity) == typeof(BoundaryMessage), "Valid bidirectional catalog mapping changed");
    }
    string[] malformed = ["review-\uD800", "review-\uDC00", "review-\uD800x", "review-\uDC00\uD800", "review-\uD800\uD800", "review-\uDC00x"];
    foreach (string name in malformed)
    {
        Require(Throws<ArgumentException>(() => new MessageContractIdentity(name, 1)).ParamName == "name", "Malformed constructor must name name");
        Require(Throws<ArgumentException>(() => MessageContractIdentity.Parse(name + ";v=1")).ParamName == "name", "Malformed parse must name name");
        Require(!MessageContractIdentity.TryParse(name + ";v=1", out var identity) && identity == default, "Malformed TryParse must return false and default");
        Require(Throws<ArgumentException>(() => new MessageContractAttribute(name)).ParamName == "name", "Malformed attribute must reject name");
        Require(Throws<ArgumentException>(() => new MessageContractCatalogBuilder().Register<BoundaryMessage>(name)).ParamName == "name", "Malformed catalog registration must reject name");
    }
    foreach (string name in new[] {new string('a', 257), string.Concat(Enumerable.Repeat("😀", 128)) + "a"})
        Require(Throws<ArgumentOutOfRangeException>(() => new MessageContractIdentity(name, 1)).ParamName == "name", "Existing UTF16 length limit changed");
    foreach (int version in new[] {-1, 0, 65536})
        Require(Throws<ArgumentOutOfRangeException>(() => new MessageContractIdentity("valid", version)).ParamName == "majorVersion", "Version boundary changed");
    foreach (string text in new[] {"valid;v=01", "valid;v=0", "valid;v=65536", "valid;v=-1", "valid;v=1;v=1"})
        Require(!MessageContractIdentity.TryParse(text, out _), "Invalid canonical form was accepted");
    Emit(new {mode = "contract-boundaries", valid_unicode_route_sets = valid.Length, malformed_rejected_route_sets = malformed.Length,
        inclusive_utf16_characters = 256, retained_version_bounds = true, contract = "PASS"});
    return 0;
}

static T Throws<T>(Action operation) where T : Exception
{
    try { operation(); }
    catch (T exception) when (exception.GetType() == typeof(T)) { return exception; }
    throw new InvalidOperationException("Expected exact " + typeof(T).Name + " was not thrown");
}
public sealed record BoundaryMessage(string Value);

sealed class StableKeyProvider(EncryptionKey current) : IEncryptionKeyProvider
{
    public EncryptionKey Current { get; } = current;
    public List<string> Lookups { get; } = [];
    public bool ResolveUnknownWithCurrent { get; set; }
    public EncryptionKey GetCurrentKey() => Current;
    public bool TryGetKey(string keyId, out EncryptionKey? key)
    {
        Lookups.Add(keyId);
        key = StringComparer.Ordinal.Equals(keyId, Current.KeyId) || ResolveUnknownWithCurrent ? Current : null;
        return key is not null;
    }
}


