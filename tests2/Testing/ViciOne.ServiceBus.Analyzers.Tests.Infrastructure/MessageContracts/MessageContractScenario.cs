using System.Collections.Immutable;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;

/// <summary>A product behavior exercised in every source form the scenario supports.</summary>
public sealed class MessageContractScenario
{
    private readonly Func<MessageSourceForm, string> _sourceFactory;

    public MessageContractScenario(
        string key,
        bool supportsLocalVariable,
        Func<MessageSourceForm, string> sourceFactory,
        ExpectedMessageContractDiagnostic? expectedDiagnostic,
        IReadOnlyList<ExpectedInitializer>? expectedAddedInitializers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(sourceFactory);

        Key = key;
        SupportsLocalVariable = supportsLocalVariable;
        _sourceFactory = sourceFactory;
        ExpectedDiagnostic = expectedDiagnostic;
        ExpectedAddedInitializers = expectedAddedInitializers?.ToImmutableArray() ?? [];
    }

    public string Key { get; }

    public bool SupportsLocalVariable { get; }

    public ExpectedMessageContractDiagnostic? ExpectedDiagnostic { get; }

    public ImmutableArray<ExpectedInitializer> ExpectedAddedInitializers { get; }

    public bool HasCodeFix => ExpectedAddedInitializers.Length > 0;

    public string CreateSource(MessageSourceForm form)
    {
        if (form == MessageSourceForm.LocalVariable && !SupportsLocalVariable)
        {
            throw new ArgumentOutOfRangeException(nameof(form), form, $"Scenario '{Key}' has no local-variable form.");
        }

        var source = _sourceFactory(form);
        var markerCount = source.Split(MessageContractSourceFactory.TargetMarker, StringSplitOptions.None).Length - 1;

        if (markerCount != 1)
        {
            throw new InvalidOperationException(
                $"Scenario '{Key}' produced {markerCount} target markers; exactly one is required.");
        }

        return source;
    }

    public IEnumerable<MessageSourceForm> EnumerateForms()
    {
        yield return MessageSourceForm.DirectArgument;

        if (SupportsLocalVariable)
        {
            yield return MessageSourceForm.LocalVariable;
        }
    }
}
