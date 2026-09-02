namespace ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;

/// <summary>Where the anonymous message value appears relative to the producer call.</summary>
public enum MessageSourceForm
{
    DirectArgument,
    LocalVariable,
}
