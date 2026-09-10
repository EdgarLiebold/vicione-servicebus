using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryScheduledPublishTests
{
    private static readonly OpCode[] SingleByteOpCodes = CreateOpCodeTable(multiByte: false);
    private static readonly OpCode[] MultiByteOpCodes = CreateOpCodeTable(multiByte: true);

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-SCHEDULED-PUBLISH", "manual-advance")]
    public async Task ScheduledPublish_IsDeliveredAtTheExactAdvancedDeadlineAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan scheduleDelay = TimeSpan.FromHours(3);
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
                configuration.AddConsumer<ScheduledMessageConsumer>())
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(operationTimeout, cancellationToken);

        try
        {
            IMessageScheduler scheduler = harness.Scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
            IInMemoryDelayProvider delayProvider = harness.Scope.ServiceProvider.GetRequiredService<IInMemoryDelayProvider>();
            Task<IConsumedMessage<ScheduledMessage>> consumed = harness.Consumed
                .SelectAsync<ScheduledMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await scheduler.SchedulePublishAsync(
                    scheduleDelay,
                    new ScheduledMessage("scheduled"),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            Assert.False(consumed.IsCompleted);
            delayProvider.Advance(scheduleDelay - TimeSpan.FromTicks(1));
            Assert.False(consumed.IsCompleted);

            delayProvider.Advance(TimeSpan.FromTicks(1));
            ConsumeContext<ScheduledMessage> context = (await consumed.WaitAsync(
                operationTimeout,
                cancellationToken)).Context;

            Assert.Equal("scheduled", context.Message.Value);
            Assert.Single(harness.Consumed.Snapshot<ScheduledMessage>());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None)
                .WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-SCHEDULED-PUBLISH", "inline-delay-registration")]
    public void DelayedDelivery_RegistersBeforeReturningWithoutAThreadPoolDispatch()
    {
        MethodInfo deliver = typeof(MessageQueue<,>).GetMethod(nameof(MessageQueue<object, object>.DeliverAsync))
            ?? throw new InvalidOperationException("MessageQueue.Deliver was not found.");
        Type stateMachineDefinition = deliver.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
            ?? throw new InvalidOperationException("MessageQueue.Deliver is not an async state machine.");
        Type stateMachine = stateMachineDefinition.IsGenericTypeDefinition
            ? stateMachineDefinition.MakeGenericType(typeof(object), typeof(object))
            : stateMachineDefinition;
        MethodInfo moveNext = stateMachine.GetMethod(
            nameof(IAsyncStateMachine.MoveNext),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("MessageQueue.Deliver.MoveNext was not found.");

        MethodBase[] calls = ReadCalledMethods(moveNext).ToArray();

        Assert.Contains(calls, method =>
            method.Name == "DeliverWithDelayAsync"
            && method.DeclaringType?.IsGenericType == true
            && method.DeclaringType.GetGenericTypeDefinition() == typeof(MessageQueue<,>));
        Assert.DoesNotContain(calls, method =>
            method.Name == nameof(Task.Run)
            && method.DeclaringType == typeof(Task));
    }


    private static IEnumerable<MethodBase> ReadCalledMethods(MethodInfo method)
    {
        byte[] il = method.GetMethodBody()?.GetILAsByteArray()
            ?? throw new InvalidOperationException($"{method} has no IL body.");
        Type[] typeArguments = method.DeclaringType?.GetGenericArguments() ?? [];
        Type[] methodArguments = method.GetGenericArguments();

        for (var offset = 0; offset < il.Length;)
        {
            OpCode operation = ReadOpCode(il, ref offset);
            if (operation.OperandType == OperandType.InlineMethod)
            {
                int token = BitConverter.ToInt32(il, offset);
                MethodBase? called = method.Module.ResolveMethod(token, typeArguments, methodArguments);
                if (called is not null)
                    yield return called;
            }

            offset += OperandSize(operation.OperandType, il, offset);
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int offset)
    {
        byte first = il[offset++];
        return first == 0xfe
            ? MultiByteOpCodes[il[offset++]]
            : SingleByteOpCodes[first];
    }

    private static int OperandSize(OperandType operandType, byte[] il, int offset) => operandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or OperandType.InlineMethod
            or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType
            or OperandType.ShortInlineR => 4,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => sizeof(int) + (BitConverter.ToInt32(il, offset) * sizeof(int)),
        _ => throw new InvalidOperationException($"Unsupported IL operand type: {operandType}."),
    };

    private static OpCode[] CreateOpCodeTable(bool multiByte)
    {
        var table = new OpCode[256];
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode operation)
                continue;

            ushort value = unchecked((ushort)operation.Value);
            bool isMultiByte = (value & 0xff00) == 0xfe00;
            if (isMultiByte == multiByte)
                table[value & 0xff] = operation;
        }

        return table;
    }

    public sealed record ScheduledMessage(string Value);

    private sealed class ScheduledMessageConsumer : IConsumer<ScheduledMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ScheduledMessage> context) => Task.CompletedTask;
    }
}
