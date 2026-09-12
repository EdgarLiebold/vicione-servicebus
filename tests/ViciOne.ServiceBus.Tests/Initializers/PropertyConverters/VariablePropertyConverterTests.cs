using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Initializers.Variables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class VariablePropertyConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-VARIABLES", "explicit-values")]
    public void ExplicitVariables_PreserveTheirSuppliedValues()
    {
        Guid expectedId = Guid.NewGuid();
        var expectedTimestamp = new DateTimeOffset(2026, 9, 12, 12, 34, 56, TimeSpan.Zero);

        Assert.Equal(expectedId, (Guid)new IdVariable(expectedId));
        Assert.Equal(expectedTimestamp, (DateTimeOffset)new TimestampVariable(expectedTimestamp));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-VARIABLES", "shared-id-and-captured-timestamp")]
    public async Task InitializerVariables_ShareTheContextIdAndPreserveTheCapturedTimestampAsync()
    {
        IdVariable correlationId = InVar.CorrelationId;
        IdVariable id = InVar.Id;
        IdVariable stringId = InVar.Id;
        TimestampVariable timestamp = InVar.Timestamp;
        DateTimeOffset expectedTimestamp = timestamp;

        InitializeContext<VariableIntermediate> intermediate = await MessageInitializerCache<VariableIntermediate>.InitializeAsync(
            new
            {
                CorrelationId = correlationId,
                Id = id,
                StringId = stringId,
                Timestamp = timestamp,
            },
            TestContext.Current.CancellationToken);
        InitializeContext<VariableMessage> result = await MessageInitializerCache<VariableMessage>.InitializeAsync(
            intermediate.Message,
            TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.Message.Id);
        Assert.Equal(result.Message.Id, result.Message.CorrelationId);
        Assert.Equal(result.Message.Id, result.Message.StringId);
        Assert.Equal(expectedTimestamp, result.Message.Timestamp);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-VARIABLES", "variables-reject-missing-context-and-conversion-source")]
    public async Task InitializerVariables_RejectMissingContextsAndNullConversionSourcesAsync()
    {
        var id = new IdVariable(Guid.NewGuid());
        var timestamp = new TimestampVariable(DateTimeOffset.UtcNow);

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((IInitializerVariable<Guid>)id).GetValueAsync<VariableMessage>(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((IInitializerVariable<DateTimeOffset>)timestamp).GetValueAsync<VariableMessage>(null!, TestContext.Current.CancellationToken))).ParamName);

        IdVariable? missingId = null;
        TimestampVariable? missingTimestamp = null;
        Assert.Equal("variable", Assert.Throws<ArgumentNullException>(() => _ = (Guid)missingId!).ParamName);
        Assert.Equal("variable", Assert.Throws<ArgumentNullException>(() => _ = (DateTimeOffset)missingTimestamp!).ParamName);

        InitializeContext<IdMessage> context = await MessageInitializerCache<IdMessage>.InitializeAsync(
            new { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        var directConverter = new VariablePropertyConverter<Guid, IdVariable>();
        var convertedConverter = new VariablePropertyConverter<string, IdVariable, Guid>(
            new TypePropertyConverter<string, Guid>(new GuidTypeConverter()));
        Assert.Equal(Guid.Empty, await directConverter.ConvertAsync(context, null, TestContext.Current.CancellationToken));
        Assert.Null(await convertedConverter.ConvertAsync(context, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-VARIABLES", "cross-initialization-isolation")]
    public async Task GeneratedIdVariables_AreIndependentAcrossInitializationsAsync()
    {
        InitializeContext<IdMessage> first = await MessageInitializerCache<IdMessage>.InitializeAsync(
            new { Id = InVar.Id }, TestContext.Current.CancellationToken);
        InitializeContext<IdMessage> second = await MessageInitializerCache<IdMessage>.InitializeAsync(
            new { Id = InVar.Id }, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, first.Message.Id);
        Assert.NotEqual(Guid.Empty, second.Message.Id);
        Assert.NotEqual(first.Message.Id, second.Message.Id);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-VARIABLES", "caller-cancellation")]
    public async Task InitializerVariables_ObserveCallerCancellationBeforeContextAccessAsync()
    {
        InitializeContext<IdMessage> context = await MessageInitializerCache<IdMessage>.InitializeAsync(
            new { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        IInitializerVariable<Guid> id = new IdVariable();
        IInitializerVariable<DateTimeOffset> timestamp = new TimestampVariable();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException idCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            id.GetValueAsync(context, cancellation.Token));
        OperationCanceledException timestampCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            timestamp.GetValueAsync(context, cancellation.Token));

        Assert.Equal(cancellation.Token, idCancellation.CancellationToken);
        Assert.Equal(cancellation.Token, timestampCancellation.CancellationToken);
    }

    public interface IdMessage
    {
        Guid Id { get; }
    }

    public interface VariableIntermediate
    {
        Guid CorrelationId { get; }

        Guid Id { get; }

        string StringId { get; }

        DateTimeOffset Timestamp { get; }
    }

    public interface VariableMessage
    {
        Guid CorrelationId { get; }

        Guid Id { get; }

        Guid StringId { get; }

        DateTimeOffset? Timestamp { get; }
    }
}
