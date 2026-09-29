using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.InternalAccess.Rescue;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RescueContextProjectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "consume-projections-preserve-message-and-failure")]
    public void ConsumeProjections_PreserveTheMessageAndCacheTheFailureSnapshot()
    {
        var message = new object();
        ConsumeContext<object> typedSource = InMemoryOutboxTestContextFactory.Create(
            message, TestContext.Current.CancellationToken);
        var failure = new InvalidOperationException("consume failed");

        ExceptionConsumeContext<object> typed = RescueContextTestFactory.Create(typedSource, failure);
        ExceptionConsumeContext untyped = RescueContextTestFactory.Create((ConsumeContext)typedSource, failure);

        Assert.Same(message, typed.Message);
        Assert.Same(failure, typed.Exception);
        Assert.Same(failure, untyped.Exception);
        Assert.Same(typed.ExceptionInfo, typed.ExceptionInfo);
        Assert.Same(untyped.ExceptionInfo, untyped.ExceptionInfo);
        Assert.Equal(failure.Message, typed.ExceptionInfo.Message);
        Assert.Equal(failure.GetType().FullName, untyped.ExceptionInfo.ExceptionType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "consumer-projection-preserves-consumer-and-failure")]
    public void ConsumerProjection_PreservesTheConsumerAndCachesTheFailureSnapshot()
    {
        ConsumeContext<object> source = InMemoryOutboxTestContextFactory.Create(
            new object(), TestContext.Current.CancellationToken);
        var consumer = new RescueConsumer();
        ConsumerConsumeContext<RescueConsumer> consumerContext =
            RescueContextTestFactory.CreateConsumerContext(source, consumer);
        var failure = new InvalidOperationException("consumer failed");

        ExceptionConsumerConsumeContext<RescueConsumer> rescue =
            RescueContextTestFactory.Create(consumerContext, failure);

        Assert.Same(consumer, rescue.Consumer);
        Assert.Same(failure, rescue.Exception);
        Assert.Same(rescue.ExceptionInfo, rescue.ExceptionInfo);
        Assert.Equal(failure.Message, rescue.ExceptionInfo.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "receive-projection-captures-clock-and-fault-headers")]
    public void ReceiveProjection_CapturesTheConfiguredClockAndFaultHeaders()
    {
        DateTimeOffset now = new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);
        ReceiveContext source = CreateReceiveContext(new FakeTimeProvider(now));
        var failure = new InvalidOperationException("receive failed");

        ExceptionReceiveContext rescue = RescueContextTestFactory.Create(source, failure);

        Assert.Same(failure, rescue.Exception);
        Assert.Equal(now, rescue.ExceptionTimestamp);
        Assert.Same(rescue.ExceptionInfo, rescue.ExceptionInfo);
        Assert.Equal("fault", rescue.ExceptionHeaders.Get<string>(MessageHeaders.Reason));
        Assert.Equal(failure.Message, rescue.ExceptionHeaders.Get<string>(MessageHeaders.FaultMessage));
        Assert.Equal(now.ToString("O"), rescue.ExceptionHeaders.Get<string>(MessageHeaders.FaultTimestamp));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RESCUE", "unsafe-base-exception-cannot-block-fault-headers")]
    public void ReceiveProjection_UsesOriginalFailureWhenBaseExceptionOverrideIsUnsafe(bool throws)
    {
        ReceiveContext source = CreateReceiveContext(TimeProvider.System);
        var failure = new UnsafeBaseException(throws);

        ExceptionReceiveContext rescue = RescueContextTestFactory.Create(source, failure);
        var dictionary = new Dictionary<string, object>();
        var adapter = new DictionaryTransportSetHeaderAdapter(new StringHeaderValueConverter());
        adapter.SetExceptionHeaders(dictionary, rescue);

        Assert.Same(failure, rescue.Exception);
        Assert.Equal(failure.Message, rescue.ExceptionHeaders.Get<string>(MessageHeaders.FaultMessage));
        Assert.Equal(TypeCache.GetShortName(failure.GetType()), rescue.ExceptionHeaders.Get<string>(MessageHeaders.FaultExceptionType));
        Assert.Equal(failure.Message, dictionary[MessageHeaders.FaultMessage]);
        Assert.Equal(TypeCache.GetShortName(failure.GetType()), dictionary[MessageHeaders.FaultExceptionType]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "projection-constructors-reject-null-inputs")]
    public void ProjectionFactories_RejectNullContextsAndFailures()
    {
        ConsumeContext<object> typedSource = InMemoryOutboxTestContextFactory.Create(
            new object(), TestContext.Current.CancellationToken);
        ConsumeContext untypedSource = (ConsumeContext)typedSource;
        ReceiveContext receiveSource = CreateReceiveContext(TimeProvider.System);
        var consumer = new RescueConsumer();
        ConsumerConsumeContext<RescueConsumer> consumerSource =
            RescueContextTestFactory.CreateConsumerContext(typedSource, consumer);
        var failure = new InvalidOperationException("failed");

        AssertContextNull(() => RescueContextTestFactory.Create((ConsumeContext<object>)null!, failure));
        AssertExceptionNull(() => RescueContextTestFactory.Create(typedSource, null!));
        AssertContextNull(() => RescueContextTestFactory.Create((ConsumeContext)null!, failure));
        AssertExceptionNull(() => RescueContextTestFactory.Create(untypedSource, null!));
        AssertContextNull(() => RescueContextTestFactory.Create((ConsumerConsumeContext<RescueConsumer>)null!, failure));
        AssertExceptionNull(() => RescueContextTestFactory.Create(consumerSource, null!));
        AssertContextNull(() => RescueContextTestFactory.Create((ReceiveContext)null!, failure));
        AssertExceptionNull(() => RescueContextTestFactory.Create(receiveSource, null!));
    }

    private static void AssertContextNull(Action action) =>
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(action).ParamName);

    private static void AssertExceptionNull(Action action) =>
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(action).ParamName);

    private static ReceiveContext CreateReceiveContext(TimeProvider timeProvider)
    {
        ReceiveContext context = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)context).TimeProvider = timeProvider;
        return context;
    }

    public class ReceiveContextProxy : DispatchProxy
    {
        public TimeProvider? TimeProvider { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "get_InputAddress":
                    return new Uri("loopback://localhost/rescue-projection-tests");
                case "TryGetPayload":
                    bool found = targetMethod.GetGenericArguments()[0] == typeof(TimeProvider);
                    args![0] = found ? TimeProvider : null;
                    return found;
                default:
                    throw new NotSupportedException($"Unexpected receive-context member: {targetMethod.Name}");
            }
        }
    }

    private sealed class RescueConsumer;

    private sealed class UnsafeBaseException(bool throws) : Exception("original failure")
    {
        public override Exception GetBaseException() => throws
            ? throw new ApplicationException("unsafe base exception override")
            : null!;
    }
}
