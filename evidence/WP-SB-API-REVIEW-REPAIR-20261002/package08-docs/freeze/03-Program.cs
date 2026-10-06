using System.Buffers.Binary;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Serialization;

return await DocumentationContracts.RunAsync(args.Single());

public static class DocumentationContracts
{
    public static async Task<int> RunAsync(string mode)
    {
        try
        {
            switch (mode)
            {
                case "empty-address": await EmptyAddressAsync(); break;
                case "pre-auth": await PreAuthenticationSelectorAsync(); break;
                case "ef-single": case "ef-separate": case "ef-same": EntityFrameworkOwnership(mode); break;
                default: throw new ArgumentException("Unknown documentation contract mode.");
            }
            Console.WriteLine(JsonSerializer.Serialize(new { mode, contract = "PASS", public_package_only = true, bus_started = false, database_opened = false }));
            return 0;
        }
        catch (ContractAssertionException error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { mode, contract = "FAIL", assertion = error.Message }));
            return 2;
        }
    }

    static async Task EmptyAddressAsync()
    {
        IMessageDataRepository repository = new InMemoryMessageDataRepository();
        MessageData<string> inline = await repository.PutStringAsync("inline-control", null,
            new MessageDataPolicy(alwaysWriteToRepository: false));
        Require(inline.HasValue && inline.Address is null && await inline.Value == "inline-control", "Populated inline control changed.");
        MessageData<string> deferred = MessageData.FromValue("deferred-control");
        Require(deferred.HasValue && deferred.Address is null && await deferred.Value == "deferred-control", "Deferred populated control changed.");
        MessageData<string> stored = await repository.PutStringAsync("stored-control");
        Require(stored.HasValue && stored.Address is not null && await stored.Value == "stored-control", "Stored positive control changed.");
        foreach (IMessageData empty in new IMessageData[] { MessageData.Empty<string>(), await repository.PutStringAsync(null) })
        {
            Require(!empty.HasValue, "Empty HasValue guard changed.");
            bool rejected = false;
            try { _ = empty.Address; }
            catch (MessageDataException error) { rejected = error.GetType() == typeof(MessageDataException); }
            Require(rejected, "Empty address no longer throws the exact documented exception.");
        }
        XElement member = Member("ViciOne.ServiceBus.Abstractions", "P:ViciOne.ServiceBus.Advanced.Serialization.IMessageData.Address");
        Require(member.Elements("exception").Any(element => (string?)element.Attribute("cref") == "T:ViciOne.ServiceBus.MessageDataException"), "Packed empty-address exception documentation missing.");
        Require(member.Element("remarks")!.Descendants("see").Any(element => ((string?)element.Attribute("cref"))?.EndsWith(".HasValue", StringComparison.Ordinal) == true), "Packed HasValue guard documentation missing.");
        Require(member.Element("summary")!.Value.Contains("populated", StringComparison.Ordinal), "Packed null-address documentation must distinguish populated from empty handles.");
    }

    static async Task PreAuthenticationSelectorAsync()
    {
        var provider = new RecordingKeyProvider(new EncryptionKey("key-A", Enumerable.Range(0, 32).Select(value => (byte)value).ToArray()));
        IMessageDataRepository inner = new InMemoryMessageDataRepository();
        var repository = new EncryptedMessageDataRepository(inner, provider, 64);
        byte[] expected = [1, 2, 3];
        await using var source = new MemoryStream(expected, writable: false);
        Uri address = await repository.PutAsync(source);
        await using (Stream control = await repository.GetAsync(address))
            Require((await ReadAsync(control)).SequenceEqual(expected), "Authenticated positive control changed.");
        Require(provider.Lookups.SequenceEqual(new[] { "key-A" }), "Positive historical key lookup changed.");
        provider.Lookups.Clear();
        await using Stream original = await inner.GetAsync(address);
        byte[] envelope = await ReadAsync(original);
        Require(envelope.AsSpan(0, 4).SequenceEqual("VOSB"u8) && envelope[4] == 1, "Unexpected envelope format.");
        int length = BinaryPrimitives.ReadUInt16BigEndian(envelope.AsSpan(5, 2));
        Require(length == 5, "Unexpected selector length.");
        "key-B"u8.CopyTo(envelope.AsSpan(7, length));
        provider.ResolveUnknown = true;
        await using var changed = new MemoryStream(envelope, writable: false);
        Uri changedAddress = await inner.PutAsync(changed);
        bool authenticationFailed = false;
        try
        {
            await using Stream unexpected = await repository.GetAsync(changedAddress);
        }
        catch (SerializationException error) when (error.InnerException?.GetType() == typeof(AuthenticationTagMismatchException))
        {
            authenticationFailed = true;
        }
        Require(authenticationFailed && provider.Lookups.SequenceEqual(new[] { "key-B" }), "Untrusted selector must be observed before exact authentication failure; plaintext must not be returned.");
        XElement member = Member("ViciOne.ServiceBus", "M:ViciOne.ServiceBus.Serialization.IEncryptionKeyProvider.TryGetKey(System.String,ViciOne.ServiceBus.Serialization.EncryptionKey@)");
        string selector = member.Elements("param").Single(element => (string?)element.Attribute("name") == "keyId").Value;
        Require(selector.Contains("untrusted", StringComparison.Ordinal) && selector.Contains("before authentication", StringComparison.Ordinal), "Packed callback selector falsely claims prior authentication.");
        Require(member.Element("remarks")?.Value.Contains("bounded lookup", StringComparison.Ordinal) == true, "Packed provider lookup bound missing.");
    }

    static void EntityFrameworkOwnership(string mode)
    {
        var services = new ServiceCollection();
        services.AddDbContext<FirstDb>();
        if (mode == "ef-separate") services.AddDbContext<SecondDb>();
        services.AddViciOneServiceBus<IFirstBus>("owned-first", bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.ConfigureEntityFrameworkTransactionalStore<IFirstBus, FirstDb>(ConfigureStore);
            bus.UsingInMemory();
        });
        bool accepted = mode == "ef-single";
        ConfigurationException? failure = null;
        try
        {
            if (mode != "ef-single") services.AddViciOneServiceBus<ISecondBus>("owned-second", bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                if (mode == "ef-same") bus.ConfigureEntityFrameworkTransactionalStore<ISecondBus, FirstDb>(ConfigureStore);
                else bus.ConfigureEntityFrameworkTransactionalStore<ISecondBus, SecondDb>(ConfigureStore);
                bus.UsingInMemory();
            });
            accepted = true;
        }
        catch (ConfigurationException error) { failure = error; }
        if (mode != "ef-same") Require(accepted, "Positive EF owner registration rejected: " + failure?.Message);
        else
        {
            Require(!accepted && failure?.Message.Contains("already owned by another bus", StringComparison.Ordinal) == true, "Shared DbContext type did not reject the precise foreign bus owner.");
            XElement member = Member("ViciOne.ServiceBus.EntityFrameworkCore", "M:ViciOne.ServiceBus.EntityFrameworkCore.EntityFrameworkOutboxConfigurationExtensions.ConfigureEntityFrameworkTransactionalStore``2(", prefix: true);
            string summary = string.Join(" ", member.Element("summary")!.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            Require(summary.Contains("Each DbContext type belongs to one bus owner", StringComparison.Ordinal), "Packed multi-bus XML does not state the actual DbContext-type owner constraint.");
            Require(summary.Contains("separate DbContext type", StringComparison.Ordinal) && !summary.Contains("allowing the same DbContext", StringComparison.Ordinal), "Packed multi-bus guidance retains the false sharing claim.");
        }
    }

    static void ConfigureStore(IEntityFrameworkOutboxConfigurator store)
    {
        store.UseSqlite();
        store.DisableInboxCleanupService();
        store.EnableTransactionalOutbox(delivery => delivery.DisableDeliveryService());
    }
    static XElement Member(string package, string id, bool prefix = false) => XDocument.Load(Path.Combine(
        Directory.GetCurrentDirectory(), "restored-packages", package.ToLowerInvariant(), "1.0.0", "lib", "net10.0", package + ".xml"))
        .Descendants("member").Single(element => prefix ? ((string?)element.Attribute("name"))?.StartsWith(id, StringComparison.Ordinal) == true : (string?)element.Attribute("name") == id);
    static async Task<byte[]> ReadAsync(Stream source) { using var target = new MemoryStream(); await source.CopyToAsync(target); return target.ToArray(); }
    static void Require(bool condition, string message) { if (!condition) throw new ContractAssertionException(message); }
    sealed class ContractAssertionException(string message) : Exception(message);
    sealed class RecordingKeyProvider(EncryptionKey key) : IEncryptionKeyProvider
    {
        public List<string> Lookups { get; } = [];
        public bool ResolveUnknown { get; set; }
        public EncryptionKey GetCurrentKey() => key;
        public bool TryGetKey(string keyId, out EncryptionKey? selected) { Lookups.Add(keyId); selected = keyId == key.KeyId || ResolveUnknown ? key : null; return selected is not null; }
    }
    public interface IFirstBus : IBus;
    public interface ISecondBus : IBus;
    public sealed class FirstDb(DbContextOptions<FirstDb> options) : DbContext(options);
    public sealed class SecondDb(DbContextOptions<SecondDb> options) : DbContext(options);
}
