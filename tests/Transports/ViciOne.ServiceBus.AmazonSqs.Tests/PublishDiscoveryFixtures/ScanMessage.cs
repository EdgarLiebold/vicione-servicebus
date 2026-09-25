namespace ViciOne.ServiceBus.AmazonSqs.Tests.PublishDiscoveryFixtures;

public interface IScanMessage;
public sealed record ScanMessage;
public sealed record ScanGenericMessage<T>;
public sealed record ScanExcludedMessage;
