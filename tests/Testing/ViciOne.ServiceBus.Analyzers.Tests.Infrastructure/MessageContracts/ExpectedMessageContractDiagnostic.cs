using Microsoft.CodeAnalysis;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;

/// <summary>The exact product diagnostic expected from one message-contract scenario.</summary>
public sealed record ExpectedMessageContractDiagnostic(
    string Id,
    DiagnosticSeverity Severity,
    string Message);
