using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class MessageConsumeContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "construction-requires-context-and-message")]
    public void Construction_RejectsMissingCollaborators()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, TypeLookupConsumeContextProxy>();

        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new MessageConsumeContext<ProbeMessage>(null!, new ProbeMessage())).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new MessageConsumeContext<ProbeMessage>(source, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "projected-message-types-are-visible-without-source-support")]
    public void ProjectedMessage_IsVisibleThroughExactAndAssignableTypedLookups()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, TypeLookupConsumeContextProxy>();
        var sourceProxy = (TypeLookupConsumeContextProxy)(object)source;
        var message = new ProbeMessage();
        var context = new MessageConsumeContext<ProbeMessage>(source, message);

        Assert.True(context.HasMessageType(typeof(ProbeMessage)));
        Assert.True(context.HasMessageType(typeof(IProbeMessage)));
        Assert.True(context.TryGetMessage(out ConsumeContext<ProbeMessage>? exact));
        Assert.Same(context, exact);
        Assert.True(context.TryGetMessage(out ConsumeContext<IProbeMessage>? assignable));
        Assert.Same(message, assignable.Message);
        Assert.Equal(2, sourceProxy.HasMessageTypeInvocationCount);
        Assert.Equal(2, sourceProxy.TryGetMessageInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "unsupported-message-types-delegate-to-source")]
    public void UnsupportedMessageType_DelegatesToTheSourceContext()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, TypeLookupConsumeContextProxy>();
        var sourceProxy = (TypeLookupConsumeContextProxy)(object)source;
        var context = new MessageConsumeContext<ProbeMessage>(source, new ProbeMessage());

        Assert.False(context.HasMessageType(typeof(UnrelatedMessage)));
        Assert.False(context.TryGetMessage(out ConsumeContext<UnrelatedMessage>? unrelated));
        Assert.Null(unrelated);
        Assert.Equal(1, sourceProxy.HasMessageTypeInvocationCount);
        Assert.Equal(1, sourceProxy.TryGetMessageInvocationCount);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() => context.HasMessageType(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "source-contract-remains-authoritative-when-already-available")]
    public void ExistingSourceContract_RemainsAuthoritativeForRepeatedMaterialization()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, TypeLookupConsumeContextProxy>();
        ConsumeContext<ProbeMessage> sourceMessageContext =
            DispatchProxy.Create<ConsumeContext<ProbeMessage>, UnexpectedInvocationProxy>();
        var sourceProxy = (TypeLookupConsumeContextProxy)(object)source;
        sourceProxy.ReturnedContext = sourceMessageContext;
        sourceProxy.HasMessageTypeResult = true;
        var context = new MessageConsumeContext<ProbeMessage>(source, new ProbeMessage());

        Assert.True(context.HasMessageType(typeof(ProbeMessage)));
        Assert.True(context.TryGetMessage(out ConsumeContext<ProbeMessage>? selected));
        Assert.Same(sourceMessageContext, selected);
        Assert.Equal(1, sourceProxy.HasMessageTypeInvocationCount);
        Assert.Equal(1, sourceProxy.TryGetMessageInvocationCount);
    }

    private interface IProbeMessage
    {
    }

    private sealed class ProbeMessage : IProbeMessage
    {
    }

    private sealed class UnrelatedMessage
    {
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The typed source context unexpectedly invoked {targetMethod?.Name}.");
    }

    private class TypeLookupConsumeContextProxy : DispatchProxy
    {
        public bool HasMessageTypeResult { get; set; }

        public int HasMessageTypeInvocationCount { get; private set; }

        public object? ReturnedContext { get; set; }

        public int TryGetMessageInvocationCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ConsumeContext.HasMessageType))
            {
                HasMessageTypeInvocationCount++;
                return HasMessageTypeResult;
            }

            if (targetMethod?.Name == nameof(ConsumeContext.TryGetMessage))
            {
                TryGetMessageInvocationCount++;
                args![0] = ReturnedContext;
                return ReturnedContext is not null;
            }

            throw new InvalidOperationException($"The typed message lookup unexpectedly invoked {targetMethod?.Name}.");
        }
    }
}
