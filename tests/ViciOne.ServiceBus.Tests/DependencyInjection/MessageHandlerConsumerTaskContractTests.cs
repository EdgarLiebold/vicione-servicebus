using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class MessageHandlerConsumerTaskContractTests
{
    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, false, true)]
    [InlineData(0, true, false)]
    [InlineData(0, true, true)]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(1, true, false)]
    [InlineData(1, true, true)]
    [InlineData(2, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    [InlineData(3, false, false)]
    [InlineData(3, false, true)]
    [InlineData(3, true, false)]
    [InlineData(3, true, true)]
    [RequirementCoverage("REQ-VSB-DI-HANDLER", "t97-message-handler-preserves-pending-task-and-fault-across-all-signatures")]
    public async Task Consumer_ReturnsTheExactPendingHandlerTaskAndItsOutcomeAsync(
        int dependencies, bool messageOnly, bool fail)
    {
        var message = new Message(Guid.NewGuid());
        ConsumeContext<Message> context = Context(message);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("handler failed");
        int invocations = 0;
        IConsumer<Message> consumer = Consumer(dependencies, messageOnly, context, message,
            () => { invocations++; return completion.Task; });

        Task running = consumer.ConsumeAsync(context);

        Assert.Same(completion.Task, running);
        Assert.False(running.IsCompleted);
        Assert.Equal(1, invocations);

        if (fail)
        {
            completion.SetException(failure);
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => running));
        }
        else
        {
            completion.SetResult();
            await running;
            Assert.True(running.IsCompletedSuccessfully);
        }

        Assert.Equal(1, invocations);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [RequirementCoverage("REQ-VSB-DI-HANDLER", "t97-message-handler-synchronous-fault-propagates-without-repeat")]
    public void Consumer_PropagatesTheExactSynchronousHandlerFailureOnce(int dependencies, bool messageOnly)
    {
        var message = new Message(Guid.NewGuid());
        ConsumeContext<Message> context = Context(message);
        var failure = new InvalidOperationException("handler rejected message");
        int invocations = 0;
        IConsumer<Message> consumer = Consumer(dependencies, messageOnly, context, message, () =>
        {
            invocations++;
            throw failure;
        });

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => { _ = consumer.ConsumeAsync(context); }));
        Assert.Equal(1, invocations);
    }

    private static IConsumer<Message> Consumer(int count, bool messageOnly, ConsumeContext<Message> expectedContext,
        Message expectedMessage, Func<Task> execute)
    {
        var first = new Dependency("first");
        var second = new Dependency("second");
        var third = new Dependency("third");
        Task InvokeAsync(ConsumeContext<Message> context, params Dependency[] dependencies)
        {
            Assert.Same(expectedContext, context);
            AssertDependencies(count, first, second, third, dependencies);
            return execute();
        }
        Task InvokeMessageAsync(Message message, params Dependency[] dependencies)
        {
            Assert.Same(expectedMessage, message);
            AssertDependencies(count, first, second, third, dependencies);
            return execute();
        }

        return (count, messageOnly) switch
        {
            (0, false) => new MessageHandlerConsumer<Message>(
                new MessageHandlerMethod<Message>((ConsumeContext<Message> context) => InvokeAsync(context))),
            (0, true) => new MessageHandlerConsumer<Message>(
                new MessageHandlerMethod<Message>((Message message) => InvokeMessageAsync(message))),
            (1, false) => new MessageHandlerConsumer<Message, Dependency>(
                new MessageHandlerMethod<Message, Dependency>(
                    (ConsumeContext<Message> context, Dependency arg1) => InvokeAsync(context, arg1)), first),
            (1, true) => new MessageHandlerConsumer<Message, Dependency>(
                new MessageHandlerMethod<Message, Dependency>(
                    (Message message, Dependency arg1) => InvokeMessageAsync(message, arg1)), first),
            (2, false) => new MessageHandlerConsumer<Message, Dependency, Dependency>(
                new MessageHandlerMethod<Message, Dependency, Dependency>(
                    (ConsumeContext<Message> context, Dependency arg1, Dependency arg2) => InvokeAsync(context, arg1, arg2)), first, second),
            (2, true) => new MessageHandlerConsumer<Message, Dependency, Dependency>(
                new MessageHandlerMethod<Message, Dependency, Dependency>(
                    (Message message, Dependency arg1, Dependency arg2) => InvokeMessageAsync(message, arg1, arg2)), first, second),
            (3, false) => new MessageHandlerConsumer<Message, Dependency, Dependency, Dependency>(
                new MessageHandlerMethod<Message, Dependency, Dependency, Dependency>(
                    (ConsumeContext<Message> context, Dependency arg1, Dependency arg2, Dependency arg3) =>
                        InvokeAsync(context, arg1, arg2, arg3)), first, second, third),
            (3, true) => new MessageHandlerConsumer<Message, Dependency, Dependency, Dependency>(
                new MessageHandlerMethod<Message, Dependency, Dependency, Dependency>(
                    (Message message, Dependency arg1, Dependency arg2, Dependency arg3) =>
                        InvokeMessageAsync(message, arg1, arg2, arg3)), first, second, third),
            _ => throw new ArgumentOutOfRangeException(nameof(count)),
        };
    }

    private static void AssertDependencies(int count, Dependency first, Dependency second, Dependency third,
        Dependency[] actual)
    {
        Dependency[] expected = [first, second, third];
        Assert.Equal(count, actual.Length);
        for (int index = 0; index < count; index++)
            Assert.Same(expected[index], actual[index]);
    }

    private static ConsumeContext<Message> Context(Message message)
    {
        ConsumeContext<Message> context = DispatchProxy.Create<ConsumeContext<Message>, CallProxy>();
        ((CallProxy)(object)context).Handler = (method, args) =>
        {
            Assert.Equal("get_Message", method.Name);
            Assert.Empty(args);
            return message;
        };
        return context;
    }

    public class CallProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method ?? throw new InvalidOperationException("Missing method."), args ?? []);
    }

    public sealed record Message(Guid Id);
    public sealed record Dependency(string Name);
}
