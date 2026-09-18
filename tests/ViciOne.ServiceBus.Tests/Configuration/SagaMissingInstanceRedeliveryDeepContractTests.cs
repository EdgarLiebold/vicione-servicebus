using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaMissingInstanceRedeliveryDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MISSING-INSTANCE-REDELIVERY", "request-configurator-exact-generic-apply-surface")]
    public void RequestConfigurator_HasOneExactGenericApplyContract()
    {
        Type type = typeof(IRequestStateMachineMissingInstanceConfigurator);
        Assert.True(type.IsPublic);
        Assert.True(type.IsInterface);
        Assert.Empty(type.GetInterfaces());
        Assert.Empty(type.GetProperties());

        MethodInfo apply = Assert.Single(type.GetMethods());
        Assert.Equal("Apply", apply.Name);
        Type[] arguments = apply.GetGenericArguments();
        Assert.Equal(2, arguments.Length);
        Assert.Equal([typeof(ISagaStateMachineInstance)], arguments[0].GetGenericParameterConstraints());
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            arguments[1].GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        ParameterInfo parameter = Assert.Single(apply.GetParameters());
        Assert.Equal("configurator", parameter.Name);
        Assert.Equal(typeof(IMissingInstanceConfigurator<,>).MakeGenericType(arguments), parameter.ParameterType);
        Assert.Equal(typeof(IPipe<>).MakeGenericType(typeof(ConsumeContext<>).MakeGenericType(arguments[1])), apply.ReturnType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MISSING-INSTANCE-REDELIVERY", "request-wrapper-constructor-and-apply-guard-ownership")]
    public void RequestWrapper_RejectsMissingCallbackAndConfiguratorAtItsOwnBoundary()
    {
        ArgumentNullException callback = Assert.Throws<ArgumentNullException>(() =>
            new RedeliverRequestStateMachineSpecification(null!));
        Assert.Equal("configure", callback.ParamName);

        var specification = new RedeliverRequestStateMachineSpecification(static _ => { });
        ArgumentNullException configurator = Assert.Throws<ArgumentNullException>(() =>
            specification.Apply<RedeliverySaga, RedeliveryMessage>(null!));
        Assert.Equal("configurator", configurator.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MISSING-INSTANCE-REDELIVERY", "request-wrapper-default-fault-before-user-configuration")]
    public void RequestWrapper_ConfiguresDefaultFaultBeforeTheUserCallbackAndBuildsThePipe()
    {
        var missing = new RecordingMissingConfigurator();
        var callbackCalls = 0;
        var specification = new RedeliverRequestStateMachineSpecification(configurator =>
        {
            callbackCalls++;
            Assert.Equal(1, missing.FaultCalls);
            configurator.None();
        });

        IPipe<ConsumeContext<RedeliveryMessage>> pipe = specification.Apply<RedeliverySaga, RedeliveryMessage>(missing);

        Assert.NotNull(pipe);
        Assert.Equal("MissingInstanceRedeliveryPipe`2", pipe.GetType().Name);
        Assert.Equal(1, missing.DiscardCalls);
        Assert.Equal(1, missing.FaultCalls);
        Assert.Equal(1, callbackCalls);
        Assert.Same(missing.FaultPipe, ReadField(pipe, "_finalPipe"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MISSING-INSTANCE-REDELIVERY", "internal-default-discard-flags-and-policy-validation")]
    public void Configurator_StartsWithDiscardAndBothOptionsAndRequiresAPolicy()
    {
        var missing = new RecordingMissingConfigurator();
        object owner = CreateConfigurator(missing);
        var configurator = Assert.IsAssignableFrom<IMissingInstanceRedeliveryConfigurator<RedeliverySaga, RedeliveryMessage>>(owner);
        var specification = Assert.IsAssignableFrom<ISpecification>(owner);

        Assert.Equal(1, missing.DiscardCalls);
        Assert.Same(missing.DiscardPipe, ReadField(owner, "_finalPipe"));
        Assert.True(Assert.IsType<bool>(owner.GetType().GetProperty("ReplaceMessageId")!.GetValue(owner)));
        Assert.True(Assert.IsType<bool>(owner.GetType().GetProperty("ConfigureMessageScheduler")!.GetValue(owner)));
        ValidationResult failure = Assert.Single(specification.Validate());
        Assert.Equal("RetryPolicy", failure.Key);
        Assert.Equal("must not be null", failure.Message);

        configurator.SetRetryPolicy(_ => Retry.None);
        Assert.Empty(specification.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MISSING-INSTANCE-REDELIVERY", "policy-observer-and-terminal-callback-guard-ownership")]
    public void Configurator_OwnsPolicyObserverAndTerminalCallbackBoundaries()
    {
        object owner = CreateConfigurator(new RecordingMissingConfigurator());
        var configurator = Assert.IsAssignableFrom<IMissingInstanceRedeliveryConfigurator<RedeliverySaga, RedeliveryMessage>>(owner);

        AssertArgument("factory", () => configurator.SetRetryPolicy(null!));
        AssertArgument("observer", () => configurator.ConnectRetryObserver(null!));
        AssertArgument("configure", () => configurator.OnRedeliveryLimitReached(null!));

        ConfigurationException result = Assert.Throws<ConfigurationException>(() =>
            configurator.OnRedeliveryLimitReached(static _ => null!));
        Assert.Contains("terminal pipe", result.Message, StringComparison.OrdinalIgnoreCase);

        IRetryObserver observer = DispatchProxy.Create<IRetryObserver, EmptyProxy>();
        using ConnectHandle handle = configurator.ConnectRetryObserver(observer);
        Assert.NotNull(handle);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MISSING-INSTANCE-REDELIVERY", "terminal-pipe-policy-identity-and-options-matrix")]
    public void Build_PreservesPolicyAndTerminalPipeAndComposesTheExactOptionsMatrix()
    {
        foreach ((bool replace, bool scheduler, RedeliveryOptions expected) in new[]
        {
            (false, false, RedeliveryOptions.None),
            (true, false, RedeliveryOptions.ReplaceMessageId),
            (false, true, RedeliveryOptions.ConfigureMessageScheduler),
            (true, true, RedeliveryOptions.ReplaceMessageId | RedeliveryOptions.ConfigureMessageScheduler)
        })
        {
            var missing = new RecordingMissingConfigurator();
            object owner = CreateConfigurator(missing);
            var configurator = Assert.IsAssignableFrom<IMissingInstanceRedeliveryConfigurator<RedeliverySaga, RedeliveryMessage>>(owner);
            configurator.ReplaceMessageId = replace;
            configurator.ConfigureMessageScheduler = scheduler;
            IRetryPolicy policy = Retry.None;
            configurator.SetRetryPolicy(_ => policy);
            configurator.OnRedeliveryLimitReached(_ => missing.FaultPipe);

            var pipe = Assert.IsAssignableFrom<IPipe<ConsumeContext<RedeliveryMessage>>>(
                owner.GetType().GetMethod("Build")!.Invoke(owner, null));

            Assert.Same(policy, ReadField(pipe, "_retryPolicy"));
            Assert.Same(missing.FaultPipe, ReadField(pipe, "_finalPipe"));
            Assert.Equal(expected, Assert.IsType<RedeliveryOptions>(ReadField(pipe, "_options")));
        }
    }

    private static object? ReadField(object owner, string name)
    {
        FieldInfo? field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(owner);
    }

    private static object CreateConfigurator(IMissingInstanceConfigurator<RedeliverySaga, RedeliveryMessage> missing)
    {
        Type? openType = typeof(RedeliverRequestStateMachineSpecification).Assembly.GetType(
            "ViciOne.ServiceBus.Configuration.MissingInstanceRedeliveryConfigurator`2");
        Assert.NotNull(openType);
        Type type = openType.MakeGenericType(typeof(RedeliverySaga), typeof(RedeliveryMessage));
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        return Assert.IsAssignableFrom<object>(constructor.Invoke([missing]));
    }

    private static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private sealed class RecordingMissingConfigurator : IMissingInstanceConfigurator<RedeliverySaga, RedeliveryMessage>
    {
        public IPipe<ConsumeContext<RedeliveryMessage>> DiscardPipe { get; } = Pipe.Empty<ConsumeContext<RedeliveryMessage>>();
        public IPipe<ConsumeContext<RedeliveryMessage>> FaultPipe { get; } = Pipe.Empty<ConsumeContext<RedeliveryMessage>>();
        public int DiscardCalls { get; private set; }
        public int FaultCalls { get; private set; }

        public IPipe<ConsumeContext<RedeliveryMessage>> Discard()
        {
            DiscardCalls++;
            return DiscardPipe;
        }

        public IPipe<ConsumeContext<RedeliveryMessage>> Fault()
        {
            FaultCalls++;
            return FaultPipe;
        }

        public IPipe<ConsumeContext<RedeliveryMessage>> ExecuteAwaited(Func<ConsumeContext<RedeliveryMessage>, Task> callback) =>
            throw new NotSupportedException();

        public IPipe<ConsumeContext<RedeliveryMessage>> Execute(Action<ConsumeContext<RedeliveryMessage>> callback) =>
            throw new NotSupportedException();
    }

    private class EmptyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true ? Activator.CreateInstance(targetMethod.ReturnType) : null;
    }

    private sealed class RedeliverySaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class RedeliveryMessage;
}
