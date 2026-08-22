namespace ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;

/// <summary>One leaf initializer a code fix must add to the marked anonymous message.</summary>
public sealed record ExpectedInitializer(string Path, string Expression);
