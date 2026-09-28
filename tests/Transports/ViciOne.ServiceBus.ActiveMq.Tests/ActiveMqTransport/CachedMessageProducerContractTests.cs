using Apache.NMS;
using System.Linq.Expressions;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class CachedMessageProducerContractTests
{
    [Theory]
    [InlineData(FactoryKind.Message, false)]
    [InlineData(FactoryKind.Message, true)]
    [InlineData(FactoryKind.EmptyText, false)]
    [InlineData(FactoryKind.EmptyText, true)]
    [InlineData(FactoryKind.Text, false)]
    [InlineData(FactoryKind.Text, true)]
    [InlineData(FactoryKind.Map, false)]
    [InlineData(FactoryKind.Map, true)]
    [InlineData(FactoryKind.Object, false)]
    [InlineData(FactoryKind.Object, true)]
    [InlineData(FactoryKind.EmptyBytes, false)]
    [InlineData(FactoryKind.EmptyBytes, true)]
    [InlineData(FactoryKind.Bytes, false)]
    [InlineData(FactoryKind.Bytes, true)]
    [InlineData(FactoryKind.Stream, false)]
    [InlineData(FactoryKind.Stream, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "native-message-factory-shapes-preserve-arguments-result-and-usage")]
    public async Task NativeMessageFactory_PreservesEachShapeResultAndUsageOrderAsync(FactoryKind kind, bool asynchronous)
    {
        string text = new('x', 7);
        object body = new();
        byte[] bytes = [1, 3, 5, 7];
        object result = kind switch
        {
            FactoryKind.Message => Native<IMessage>(),
            FactoryKind.EmptyText or FactoryKind.Text => Native<ITextMessage>(),
            FactoryKind.Map => Native<IMapMessage>(),
            FactoryKind.Object => Native<IObjectMessage>(),
            FactoryKind.EmptyBytes or FactoryKind.Bytes => Native<IBytesMessage>(),
            FactoryKind.Stream => Native<IStreamMessage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        string expectedName = $"Create{kind switch
        {
            FactoryKind.Message => string.Empty,
            FactoryKind.EmptyText or FactoryKind.Text => "Text",
            FactoryKind.EmptyBytes or FactoryKind.Bytes => "Bytes",
            _ => kind.ToString(),
        }}Message{(asynchronous ? "Async" : string.Empty)}";
        object?[] expectedArgs = kind switch
        {
            FactoryKind.Text => [text],
            FactoryKind.Object => [body],
            FactoryKind.Bytes => [bytes],
            _ => [],
        };
        int nativeCalls = 0;
        int usage = 0;
        PendingFactory? pending = asynchronous ? PendingFor(kind, result) : null;
        IMessageProducer native = InterfaceProxy<IMessageProducer>.Create((method, args) =>
        {
            Assert.Equal(expectedName, method.Name);
            AssertArguments(expectedArgs, args);
            Assert.Equal(1, usage);
            nativeCalls++;
            return asynchronous ? pending!.Task : result;
        });
        var cached = new CachedMessageProducer(Native<IDestination>(), native);
        ((IResourceUsageSource)cached).Used += () => usage++;

        object actual;
        if (asynchronous)
        {
            Task forwarded = CreateAsync(cached, kind, text, body, bytes);
            Assert.Same(pending!.Task, forwarded);
            Assert.False(forwarded.IsCompleted);
            Assert.Equal(1, nativeCalls);
            Assert.Equal(1, usage);
            pending.Complete();
            await forwarded;
            actual = FactoryResult(kind, forwarded);
        }
        else
        {
            actual = Create(cached, kind, text, body, bytes);
        }

        Assert.Same(result, actual);
        Assert.Equal(1, nativeCalls);
        Assert.Equal(1, usage);
    }

    [Theory]
    [InlineData(SendShape.Default, false)]
    [InlineData(SendShape.Default, true)]
    [InlineData(SendShape.Settings, false)]
    [InlineData(SendShape.Settings, true)]
    [InlineData(SendShape.Destination, false)]
    [InlineData(SendShape.Destination, true)]
    [InlineData(SendShape.DestinationAndSettings, false)]
    [InlineData(SendShape.DestinationAndSettings, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "native-send-shapes-preserve-delivery-options-and-async-completion")]
    public async Task NativeSend_PreservesEveryDestinationAndDeliverySettingsShapeAsync(SendShape shape, bool asynchronous)
    {
        IDestination owningDestination = Native<IDestination>();
        IDestination explicitDestination = Native<IDestination>();
        IMessage message = Native<IMessage>();
        const MsgDeliveryMode mode = MsgDeliveryMode.NonPersistent;
        const MsgPriority priority = MsgPriority.Highest;
        TimeSpan lifetime = TimeSpan.FromSeconds(47);
        object?[] expectedArgs = shape switch
        {
            SendShape.Default => [message],
            SendShape.Settings => [message, mode, priority, lifetime],
            SendShape.Destination => [explicitDestination, message],
            SendShape.DestinationAndSettings => [explicitDestination, message, mode, priority, lifetime],
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int nativeCalls = 0;
        int usage = 0;
        IMessageProducer native = InterfaceProxy<IMessageProducer>.Create((method, args) =>
        {
            Assert.Equal(asynchronous ? nameof(IMessageProducer.SendAsync) : nameof(IMessageProducer.Send), method.Name);
            AssertArguments(expectedArgs, args);
            Assert.Equal(1, usage);
            nativeCalls++;
            return asynchronous ? completion.Task : null;
        });
        var cached = new CachedMessageProducer(owningDestination, native);
        Assert.Same(owningDestination, cached.Destination);
        ((IResourceUsageSource)cached).Used += () => usage++;

        if (asynchronous)
        {
            Task pending = shape switch
            {
                SendShape.Default => cached.SendAsync(message),
                SendShape.Settings => cached.SendAsync(message, mode, priority, lifetime),
                SendShape.Destination => cached.SendAsync(explicitDestination, message),
                SendShape.DestinationAndSettings => cached.SendAsync(explicitDestination, message, mode, priority, lifetime),
                _ => throw new ArgumentOutOfRangeException(nameof(shape)),
            };
            Assert.Same(completion.Task, pending);
            Assert.False(pending.IsCompleted);
            Assert.Equal(1, nativeCalls);
            Assert.Equal(1, usage);
            completion.SetResult();
            await pending;
        }
        else
        {
            switch (shape)
            {
                case SendShape.Default:
                    cached.Send(message);
                    break;
                case SendShape.Settings:
                    cached.Send(message, mode, priority, lifetime);
                    break;
                case SendShape.Destination:
                    cached.Send(explicitDestination, message);
                    break;
                case SendShape.DestinationAndSettings:
                    cached.Send(explicitDestination, message, mode, priority, lifetime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }
        }

        Assert.Equal(1, nativeCalls);
        Assert.Equal(1, usage);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "native-close-and-dispose-preserve-lifecycle-boundaries")]
    public async Task NativeCloseAndDispose_ForwardExactlyOnceWithOnlyAsyncCloseReportingUsageAsync()
    {
        var closeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<string>();
        int usage = 0;
        IMessageProducer native = InterfaceProxy<IMessageProducer>.Create((method, args) =>
        {
            AssertArguments([], args);
            calls.Add(method.Name);
            Assert.Equal(method.Name == nameof(IMessageProducer.Close) ? 0 : 1, usage);
            return method.Name == nameof(IMessageProducer.CloseAsync) ? closeCompletion.Task : null;
        });
        var cached = new CachedMessageProducer(Native<IDestination>(), native);
        ((IResourceUsageSource)cached).Used += () => usage++;

        cached.Close();
        Task pending = cached.CloseAsync();
        Assert.Same(closeCompletion.Task, pending);
        Assert.False(pending.IsCompleted);
        closeCompletion.SetResult();
        await pending;
        cached.Dispose();

        Assert.Equal([nameof(IMessageProducer.Close), nameof(IMessageProducer.CloseAsync), nameof(IDisposable.Dispose)], calls);
        Assert.Equal(1, usage);
    }

    [Theory]
    [InlineData(nameof(IMessageProducer.ProducerTransformer))]
    [InlineData(nameof(IMessageProducer.DeliveryMode))]
    [InlineData(nameof(IMessageProducer.TimeToLive))]
    [InlineData(nameof(IMessageProducer.RequestTimeout))]
    [InlineData(nameof(IMessageProducer.Priority))]
    [InlineData(nameof(IMessageProducer.DisableMessageID))]
    [InlineData(nameof(IMessageProducer.DisableMessageTimestamp))]
    [InlineData(nameof(IMessageProducer.DeliveryDelay))]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "native-producer-properties-forward-both-directions-without-usage")]
    public void NativeProperties_ForwardGetterAndSetterWithoutReportingUsage(string name)
    {
        (object? oldValue, object? newValue) = PropertyValues(name);
        object? nativeValue = oldValue;
        var calls = new List<string>();
        int usage = 0;
        IMessageProducer native = InterfaceProxy<IMessageProducer>.Create((method, args) =>
        {
            calls.Add(method.Name);
            Assert.Equal(0, usage);
            if (method.Name == $"get_{name}")
            {
                AssertArguments([], args);
                return nativeValue;
            }

            Assert.Equal($"set_{name}", method.Name);
            AssertArguments([newValue], args);
            nativeValue = args![0];
            return null;
        });
        var cached = new CachedMessageProducer(Native<IDestination>(), native);
        ((IResourceUsageSource)cached).Used += () => usage++;
        var property = typeof(CachedMessageProducer).GetProperty(name)!;

        AssertPropertyValue(oldValue, property.GetValue(cached));
        property.SetValue(cached, newValue);
        AssertPropertyValue(newValue, property.GetValue(cached));

        Assert.Equal([$"get_{name}", $"set_{name}", $"get_{name}"], calls);
        Assert.Equal(0, usage);
    }

    private static (object? OldValue, object? NewValue) PropertyValues(string name) =>
        name switch
        {
            nameof(IMessageProducer.ProducerTransformer) => (NewTransformer(), NewTransformer()),
            nameof(IMessageProducer.DeliveryMode) => (MsgDeliveryMode.Persistent, MsgDeliveryMode.NonPersistent),
            nameof(IMessageProducer.TimeToLive) => (TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(29)),
            nameof(IMessageProducer.RequestTimeout) => (TimeSpan.FromSeconds(31), TimeSpan.FromSeconds(47)),
            nameof(IMessageProducer.Priority) => (MsgPriority.Lowest, MsgPriority.Highest),
            nameof(IMessageProducer.DisableMessageID) => (false, true),
            nameof(IMessageProducer.DisableMessageTimestamp) => (false, true),
            nameof(IMessageProducer.DeliveryDelay) => (TimeSpan.FromSeconds(53), TimeSpan.FromSeconds(71)),
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };

    private static void AssertPropertyValue(object? expected, object? actual)
    {
        if (expected is ProducerTransformerDelegate)
            Assert.Same(expected, actual);
        else
            Assert.Equal(expected, actual);
    }

    private static T Native<T>() where T : class =>
        InterfaceProxy<T>.Create((method, _) => method.ReturnType.IsValueType
            ? Activator.CreateInstance(method.ReturnType)
            : null);

    private static void AssertArguments(object?[] expected, object?[]? actual)
    {
        object?[] supplied = actual ?? [];
        Assert.Equal(expected.Length, supplied.Length);
        for (int index = 0; index < expected.Length; index++)
        {
            if (expected[index] is string text)
                Assert.Equal(text, supplied[index]);
            else if (expected[index] is ValueType)
                Assert.Equal(expected[index], supplied[index]);
            else
                Assert.Same(expected[index], supplied[index]);
        }
    }

    private static object Create(CachedMessageProducer producer, FactoryKind kind, string text, object body, byte[] bytes) =>
        kind switch
        {
            FactoryKind.Message => producer.CreateMessage(),
            FactoryKind.EmptyText => producer.CreateTextMessage(),
            FactoryKind.Text => producer.CreateTextMessage(text),
            FactoryKind.Map => producer.CreateMapMessage(),
            FactoryKind.Object => producer.CreateObjectMessage(body),
            FactoryKind.EmptyBytes => producer.CreateBytesMessage(),
            FactoryKind.Bytes => producer.CreateBytesMessage(bytes),
            FactoryKind.Stream => producer.CreateStreamMessage(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private static Task CreateAsync(CachedMessageProducer producer, FactoryKind kind, string text, object body, byte[] bytes) =>
        kind switch
        {
            FactoryKind.Message => producer.CreateMessageAsync(),
            FactoryKind.EmptyText => producer.CreateTextMessageAsync(),
            FactoryKind.Text => producer.CreateTextMessageAsync(text),
            FactoryKind.Map => producer.CreateMapMessageAsync(),
            FactoryKind.Object => producer.CreateObjectMessageAsync(body),
            FactoryKind.EmptyBytes => producer.CreateBytesMessageAsync(),
            FactoryKind.Bytes => producer.CreateBytesMessageAsync(bytes),
            FactoryKind.Stream => producer.CreateStreamMessageAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private static object FactoryResult(FactoryKind kind, Task task) => kind switch
    {
        FactoryKind.Message => ((Task<IMessage>)task).Result,
        FactoryKind.EmptyText or FactoryKind.Text => ((Task<ITextMessage>)task).Result,
        FactoryKind.Map => ((Task<IMapMessage>)task).Result,
        FactoryKind.Object => ((Task<IObjectMessage>)task).Result,
        FactoryKind.EmptyBytes or FactoryKind.Bytes => ((Task<IBytesMessage>)task).Result,
        FactoryKind.Stream => ((Task<IStreamMessage>)task).Result,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static PendingFactory PendingFor(FactoryKind kind, object result) => kind switch
    {
        FactoryKind.Message => Pending((IMessage)result),
        FactoryKind.EmptyText or FactoryKind.Text => Pending((ITextMessage)result),
        FactoryKind.Map => Pending((IMapMessage)result),
        FactoryKind.Object => Pending((IObjectMessage)result),
        FactoryKind.EmptyBytes or FactoryKind.Bytes => Pending((IBytesMessage)result),
        FactoryKind.Stream => Pending((IStreamMessage)result),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static PendingFactory Pending<T>(T result) where T : class
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        return new PendingFactory(completion.Task, () => completion.SetResult(result));
    }

    private static ProducerTransformerDelegate NewTransformer()
    {
        var signature = typeof(ProducerTransformerDelegate).GetMethod("Invoke")!;
        var parameters = signature.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        return (ProducerTransformerDelegate)Expression.Lambda(
            typeof(ProducerTransformerDelegate), Expression.Default(signature.ReturnType), parameters).Compile();
    }

    private sealed record PendingFactory(Task Task, Action Complete);

    public enum FactoryKind { Message, EmptyText, Text, Map, Object, EmptyBytes, Bytes, Stream }

    public enum SendShape { Default, Settings, Destination, DestinationAndSettings }
}
