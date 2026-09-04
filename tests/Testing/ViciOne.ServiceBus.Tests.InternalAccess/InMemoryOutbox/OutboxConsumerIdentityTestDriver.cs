using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;

public static class OutboxConsumerIdentityTestDriver
{
    public static OutboxConsumerIdentitySnapshot CreateSnapshot()
    {
        var endpoint = new Uri("loopback://localhost/shared-endpoint");
        var otherEndpoint = new Uri("loopback://localhost/other-endpoint");
        string defaultKey = BusRegistrationIdentity.GetKey(typeof(IBus));
        string namedKey = BusRegistrationIdentity.GetKey(typeof(INamedBus));

        Guid defaultId = OutboxConsumerIdentity.Create<TestConsumer, TestMessage>(defaultKey, endpoint);
        Guid namedId = OutboxConsumerIdentity.Create<TestConsumer, TestMessage>(namedKey, endpoint);

        return new OutboxConsumerIdentitySnapshot(
            defaultKey,
            namedKey,
            defaultId,
            namedId,
            OutboxConsumerIdentity.Create<TestConsumer, TestMessage>(namedKey, endpoint),
            OutboxConsumerIdentity.Create<TestConsumer, TestMessage>(namedKey, otherEndpoint));
    }

    interface INamedBus : IBus;
    sealed class TestConsumer;
    sealed class TestMessage;
}

public sealed record OutboxConsumerIdentitySnapshot(
    string DefaultBusKey,
    string NamedBusKey,
    Guid DefaultId,
    Guid NamedId,
    Guid RepeatedNamedId,
    Guid OtherEndpointId);
