using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineCompositeEventStatusAccessorDeepContractTests
{
    private const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-public-surface")]
    public void PublicSurface_PreservesTheExactAccessorContracts()
    {
        Type contract = typeof(ICompositeEventStatusAccessor<>);
        Type sagaParameter = Assert.Single(contract.GetGenericArguments());

        Assert.True(contract.IsInterface);
        Assert.Equal("TSaga", sagaParameter.Name);
        Assert.Equal(GenericParameterAttributes.Contravariant, sagaParameter.GenericParameterAttributes);
        Assert.Equal([typeof(IProbeSite)], contract.GetInterfaces());

        MethodInfo[] methods = contract.GetMethods(DeclaredPublicInstance);
        Assert.Equal(2, methods.Length);
        AssertMethod(
            methods,
            nameof(ICompositeEventStatusAccessor<object>.Get),
            typeof(CompositeEventStatus),
            [sagaParameter],
            ["instance"]);
        AssertMethod(
            methods,
            nameof(ICompositeEventStatusAccessor<object>.Set),
            typeof(void),
            [sagaParameter, typeof(CompositeEventStatus)],
            ["instance", "status"]);

        var nullability = new NullabilityInfoContext();
        Assert.All(methods, method =>
        {
            NullabilityInfo instanceNullability = nullability.Create(method.GetParameters()[0]);
            Assert.Equal(NullabilityState.Nullable, instanceNullability.ReadState);
            Assert.Equal(NullabilityState.NotNull, instanceNullability.WriteState);
        });

        AssertAccessorClass(typeof(IntCompositeEventStatusAccessor<>), nullability);
        AssertAccessorClass(typeof(StructCompositeEventStatusAccessor<>), nullability);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-constructor-boundaries")]
    public void Constructors_RejectInvalidDescriptorsWithThePropertyInfoParameter()
    {
        PropertyInfo intProperty = GetProperty<AccessorSaga>(nameof(AccessorSaga.IntStatus));
        PropertyInfo structProperty = GetProperty<AccessorSaga>(nameof(AccessorSaga.StructStatus));

        AssertConstructorFailure<ArgumentNullException>(
            () => new IntCompositeEventStatusAccessor<AccessorSaga>(null!),
            "propertyInfo");
        AssertConstructorFailure<ArgumentNullException>(
            () => new StructCompositeEventStatusAccessor<AccessorSaga>(null!),
            "propertyInfo");

        AssertConstructorFailure<ArgumentException>(
            () => new IntCompositeEventStatusAccessor<AccessorSaga>(structProperty),
            "propertyInfo",
            $"Property type {typeof(CompositeEventStatus)} does not match {typeof(int)}.");
        AssertConstructorFailure<ArgumentException>(
            () => new StructCompositeEventStatusAccessor<AccessorSaga>(intProperty),
            "propertyInfo",
            $"Property type {typeof(int)} does not match {typeof(CompositeEventStatus)}.");

        AssertConstructorFailure<ArgumentException>(
            () => new IntCompositeEventStatusAccessor<AccessorSaga>(GetProperty<ForeignSaga>(nameof(ForeignSaga.IntStatus))),
            "propertyInfo");
        AssertConstructorFailure<ArgumentException>(
            () => new StructCompositeEventStatusAccessor<AccessorSaga>(GetProperty<ForeignSaga>(nameof(ForeignSaga.StructStatus))),
            "propertyInfo");
        AssertConstructorFailure<ArgumentException>(
            () => new IntCompositeEventStatusAccessor<BaseStatusSaga>(
                GetProperty<HiddenStatusSaga>(nameof(HiddenStatusSaga.IntStatus))),
            "propertyInfo");
        AssertConstructorFailure<ArgumentException>(
            () => new StructCompositeEventStatusAccessor<BaseStatusSaga>(
                GetProperty<HiddenStatusSaga>(nameof(HiddenStatusSaga.StructStatus))),
            "propertyInfo");

        AssertConstructorFailure<ArgumentException>(
            () => new IntCompositeEventStatusAccessor<InvalidDescriptorSaga>(
                GetProperty<InvalidDescriptorSaga>("Item", [typeof(int)])),
            "propertyInfo",
            "Indexed property Item is not supported.");
        AssertConstructorFailure<ArgumentException>(
            () => new StructCompositeEventStatusAccessor<InvalidDescriptorSaga>(
                GetProperty<InvalidDescriptorSaga>("Item", [typeof(string)])),
            "propertyInfo",
            "Indexed property Item is not supported.");

        AssertConstructorFailure<ArgumentException>(
            () => new IntCompositeEventStatusAccessor<InvalidDescriptorSaga>(
                GetProperty<InvalidDescriptorSaga>(nameof(InvalidDescriptorSaga.StaticIntStatus))),
            "propertyInfo",
            "Static property StaticIntStatus is not supported.");
        AssertConstructorFailure<ArgumentException>(
            () => new StructCompositeEventStatusAccessor<InvalidDescriptorSaga>(
                GetProperty<InvalidDescriptorSaga>(nameof(InvalidDescriptorSaga.StaticStructStatus))),
            "propertyInfo",
            "Static property StaticStructStatus is not supported.");

        AssertConstructorFailure<ArgumentException>(
            () => new IntCompositeEventStatusAccessor<InvalidDescriptorSaga>(
                GetProperty<InvalidDescriptorSaga>(nameof(InvalidDescriptorSaga.ReadOnlyIntStatus))),
            "propertyInfo",
            "The property does not have a setter: ReadOnlyIntStatus");
        AssertConstructorFailure<ArgumentException>(
            () => new StructCompositeEventStatusAccessor<InvalidDescriptorSaga>(
                GetProperty<InvalidDescriptorSaga>(nameof(InvalidDescriptorSaga.ReadOnlyStructStatus))),
            "propertyInfo",
            "The property does not have a setter: ReadOnlyStructStatus");

        AssertConstructorFailure<ArgumentException>(
            () => new IntCompositeEventStatusAccessor<InvalidDescriptorSaga>(
                GetProperty<InvalidDescriptorSaga>(nameof(InvalidDescriptorSaga.WriteOnlyIntStatus))),
            "propertyInfo",
            "The property does not have a getter: WriteOnlyIntStatus");
        AssertConstructorFailure<ArgumentException>(
            () => new StructCompositeEventStatusAccessor<InvalidDescriptorSaga>(
                GetProperty<InvalidDescriptorSaga>(nameof(InvalidDescriptorSaga.WriteOnlyStructStatus))),
            "propertyInfo",
            "The property does not have a getter: WriteOnlyStructStatus");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-entry-null-boundaries")]
    public void PublicEntries_RejectNullAtTheirExactOwningBoundaries()
    {
        ICompositeEventStatusAccessor<AccessorSaga>[] accessors =
        [
            new IntCompositeEventStatusAccessor<AccessorSaga>(GetProperty<AccessorSaga>(nameof(AccessorSaga.IntStatus))),
            new StructCompositeEventStatusAccessor<AccessorSaga>(GetProperty<AccessorSaga>(nameof(AccessorSaga.StructStatus))),
        ];

        foreach (ICompositeEventStatusAccessor<AccessorSaga> accessor in accessors)
        {
            ArgumentNullException getException = Assert.Throws<ArgumentNullException>(() => accessor.Get(null!));
            Assert.Equal("instance", getException.ParamName);

            ArgumentNullException setException = Assert.Throws<ArgumentNullException>(
                () => accessor.Set(null!, new CompositeEventStatus(1)));
            Assert.Equal("instance", setException.ParamName);

            ArgumentNullException probeException = Assert.Throws<ArgumentNullException>(() => accessor.Probe(null!));
            Assert.Equal("context", probeException.ParamName);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-roundtrip-and-probe")]
    public void Accessors_RoundTripEveryBitAndReportExactProbeMetadata()
    {
        var instance = new AccessorSaga();
        var intAccessor = new IntCompositeEventStatusAccessor<AccessorSaga>(
            GetProperty<AccessorSaga>(nameof(AccessorSaga.IntStatus)));
        var structAccessor = new StructCompositeEventStatusAccessor<AccessorSaga>(
            GetProperty<AccessorSaga>(nameof(AccessorSaga.StructStatus)));
        var intStatus = new CompositeEventStatus(unchecked((int)0xA5A5A5A5));
        var structStatus = new CompositeEventStatus(unchecked((int)0x5A5A5A5A));

        intAccessor.Set(instance, intStatus);
        structAccessor.Set(instance, structStatus);

        Assert.Equal(intStatus, intAccessor.Get(instance));
        Assert.Equal(intStatus.Bits, instance.IntStatus);
        Assert.Equal(structStatus, structAccessor.Get(instance));
        Assert.Equal(structStatus, instance.StructStatus);
        Assert.Equal(2, instance.IntReadCount);
        Assert.Equal(1, instance.IntWriteCount);
        Assert.Equal(2, instance.StructReadCount);
        Assert.Equal(1, instance.StructWriteCount);

        RecordingProbeContext intProbe = CreateProbe();
        intAccessor.Probe(intProbe.Context);
        Assert.Equal(
            [("property", (object?)nameof(AccessorSaga.IntStatus)), ("type", nameof(Int32))],
            intProbe.Values);

        RecordingProbeContext structProbe = CreateProbe();
        structAccessor.Probe(structProbe.Context);
        Assert.Equal(
            [("property", (object?)nameof(AccessorSaga.StructStatus)), ("type", nameof(CompositeEventStatus))],
            structProbe.Values);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-probe-failure-order-and-identity")]
    public void Probe_StopsAtTheFailingWriteAndPreservesItsExactException()
    {
        ICompositeEventStatusAccessor<AccessorSaga>[] accessors =
        [
            new IntCompositeEventStatusAccessor<AccessorSaga>(GetProperty<AccessorSaga>(nameof(AccessorSaga.IntStatus))),
            new StructCompositeEventStatusAccessor<AccessorSaga>(GetProperty<AccessorSaga>(nameof(AccessorSaga.StructStatus))),
        ];

        foreach (ICompositeEventStatusAccessor<AccessorSaga> accessor in accessors)
        {
            for (var failureAttempt = 1; failureAttempt <= 2; failureAttempt++)
            {
                var expected = new InvalidOperationException($"probe failure {failureAttempt}");
                RecordingProbeContext probe = CreateProbe(failureAttempt, expected);

                InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => accessor.Probe(probe.Context));

                Assert.Same(expected, actual);
                Assert.Equal(failureAttempt, probe.Attempts.Count);
                Assert.Equal(failureAttempt - 1, probe.Values.Count);
                Assert.Equal(new[] { "property", "type" }.Take(failureAttempt), probe.Attempts.Select(entry => entry.Key));
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-exact-property-info-identity")]
    public void Accessors_BindTheExactInheritedAndHiddenPropertyDescriptors()
    {
        PropertyInfo inheritedInt = GetProperty<BaseStatusSaga>(nameof(BaseStatusSaga.IntStatus));
        PropertyInfo hiddenInt = GetProperty<HiddenStatusSaga>(nameof(HiddenStatusSaga.IntStatus));
        PropertyInfo inheritedStruct = GetProperty<BaseStatusSaga>(nameof(BaseStatusSaga.StructStatus));
        PropertyInfo hiddenStruct = GetProperty<HiddenStatusSaga>(nameof(HiddenStatusSaga.StructStatus));
        var inheritedIntAccessor = new IntCompositeEventStatusAccessor<HiddenStatusSaga>(inheritedInt);
        var hiddenIntAccessor = new IntCompositeEventStatusAccessor<HiddenStatusSaga>(hiddenInt);
        var inheritedStructAccessor = new StructCompositeEventStatusAccessor<HiddenStatusSaga>(inheritedStruct);
        var hiddenStructAccessor = new StructCompositeEventStatusAccessor<HiddenStatusSaga>(hiddenStruct);
        var instance = new HiddenStatusSaga();
        var inheritedIntStatus = new CompositeEventStatus(0x35);
        var hiddenIntStatus = new CompositeEventStatus(0x36);
        var inheritedStructStatus = new CompositeEventStatus(0x4A);
        var hiddenStructStatus = new CompositeEventStatus(0x4B);

        inheritedIntAccessor.Set(instance, inheritedIntStatus);
        hiddenIntAccessor.Set(instance, hiddenIntStatus);
        inheritedStructAccessor.Set(instance, inheritedStructStatus);
        hiddenStructAccessor.Set(instance, hiddenStructStatus);

        Assert.Equal(inheritedIntStatus, inheritedIntAccessor.Get(instance));
        Assert.Equal(inheritedIntStatus.Bits, ((BaseStatusSaga)instance).IntStatus);
        Assert.Equal(hiddenIntStatus, hiddenIntAccessor.Get(instance));
        Assert.Equal(hiddenIntStatus.Bits, instance.IntStatus);
        Assert.Equal(inheritedStructStatus, inheritedStructAccessor.Get(instance));
        Assert.Equal(inheritedStructStatus, ((BaseStatusSaga)instance).StructStatus);
        Assert.Equal(hiddenStructStatus, hiddenStructAccessor.Get(instance));
        Assert.Equal(hiddenStructStatus, instance.StructStatus);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-nonpublic-and-interface-properties")]
    public void Accessors_SupportNonPublicAndInterfaceDeclaredProperties()
    {
        PropertyInfo privateInt = typeof(NonPublicStatusSaga).GetProperty(
            "IntStatus",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        PropertyInfo privateStruct = typeof(NonPublicStatusSaga).GetProperty(
            "StructStatus",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var privateIntAccessor = new IntCompositeEventStatusAccessor<NonPublicStatusSaga>(privateInt);
        var privateStructAccessor = new StructCompositeEventStatusAccessor<NonPublicStatusSaga>(privateStruct);
        var privateInstance = new NonPublicStatusSaga();

        privateIntAccessor.Set(privateInstance, new CompositeEventStatus(0x12));
        privateStructAccessor.Set(privateInstance, new CompositeEventStatus(0x24));

        Assert.Equal(0x12, privateIntAccessor.Get(privateInstance).Bits);
        Assert.Equal(0x24, privateStructAccessor.Get(privateInstance).Bits);

        var interfaceIntAccessor = new IntCompositeEventStatusAccessor<IStatusSaga>(
            GetProperty<IStatusSaga>(nameof(IStatusSaga.IntStatus)));
        var interfaceStructAccessor = new StructCompositeEventStatusAccessor<IStatusSaga>(
            GetProperty<IStatusSaga>(nameof(IStatusSaga.StructStatus)));
        IStatusSaga interfaceInstance = new StatusSaga();

        interfaceIntAccessor.Set(interfaceInstance, new CompositeEventStatus(0x48));
        interfaceStructAccessor.Set(interfaceInstance, new CompositeEventStatus(0x60));

        Assert.Equal(0x48, interfaceIntAccessor.Get(interfaceInstance).Bits);
        Assert.Equal(0x60, interfaceStructAccessor.Get(interfaceInstance).Bits);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-failure-identity")]
    public void PropertyFailures_PreserveTheOriginatingExceptionIdentity()
    {
        var instance = new ThrowingStatusSaga();
        var intReadAccessor = new IntCompositeEventStatusAccessor<ThrowingStatusSaga>(
            GetProperty<ThrowingStatusSaga>(nameof(ThrowingStatusSaga.IntReadStatus)));
        var intWriteAccessor = new IntCompositeEventStatusAccessor<ThrowingStatusSaga>(
            GetProperty<ThrowingStatusSaga>(nameof(ThrowingStatusSaga.IntWriteStatus)));
        var structReadAccessor = new StructCompositeEventStatusAccessor<ThrowingStatusSaga>(
            GetProperty<ThrowingStatusSaga>(nameof(ThrowingStatusSaga.StructReadStatus)));
        var structWriteAccessor = new StructCompositeEventStatusAccessor<ThrowingStatusSaga>(
            GetProperty<ThrowingStatusSaga>(nameof(ThrowingStatusSaga.StructWriteStatus)));

        Exception intReadFailure = Assert.Throws<InvalidOperationException>(() => intReadAccessor.Get(instance));
        Exception intWriteFailure = Assert.Throws<ArgumentException>(
            () => intWriteAccessor.Set(instance, new CompositeEventStatus(1)));
        Exception structReadFailure = Assert.Throws<ApplicationException>(() => structReadAccessor.Get(instance));
        Exception structWriteFailure = Assert.Throws<NotSupportedException>(
            () => structWriteAccessor.Set(instance, new CompositeEventStatus(1)));

        Assert.Same(instance.IntReadFailure, intReadFailure);
        Assert.Same(instance.IntWriteFailure, intWriteFailure);
        Assert.Same(instance.StructReadFailure, structReadFailure);
        Assert.Same(instance.StructWriteFailure, structWriteFailure);

        var nonPublicInstance = new NonPublicThrowingStatusSaga();
        var nonPublicIntReadAccessor = new IntCompositeEventStatusAccessor<NonPublicThrowingStatusSaga>(
            GetNonPublicProperty<NonPublicThrowingStatusSaga>("IntReadStatus"));
        var nonPublicIntWriteAccessor = new IntCompositeEventStatusAccessor<NonPublicThrowingStatusSaga>(
            GetNonPublicProperty<NonPublicThrowingStatusSaga>("IntWriteStatus"));
        var nonPublicStructReadAccessor = new StructCompositeEventStatusAccessor<NonPublicThrowingStatusSaga>(
            GetNonPublicProperty<NonPublicThrowingStatusSaga>("StructReadStatus"));
        var nonPublicStructWriteAccessor = new StructCompositeEventStatusAccessor<NonPublicThrowingStatusSaga>(
            GetNonPublicProperty<NonPublicThrowingStatusSaga>("StructWriteStatus"));

        Assert.Same(
            nonPublicInstance.IntReadFailure,
            Assert.Throws<InvalidOperationException>(() => nonPublicIntReadAccessor.Get(nonPublicInstance)));
        Assert.Same(
            nonPublicInstance.IntWriteFailure,
            Assert.Throws<ArgumentException>(
                () => nonPublicIntWriteAccessor.Set(nonPublicInstance, new CompositeEventStatus(1))));
        Assert.Same(
            nonPublicInstance.StructReadFailure,
            Assert.Throws<ApplicationException>(() => nonPublicStructReadAccessor.Get(nonPublicInstance)));
        Assert.Same(
            nonPublicInstance.StructWriteFailure,
            Assert.Throws<NotSupportedException>(
                () => nonPublicStructWriteAccessor.Set(nonPublicInstance, new CompositeEventStatus(1))));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-217-accessor-independent-concurrency")]
    public async Task SharedAccessors_OperateIndependentlyAcrossConcurrentSagaInstancesAsync()
    {
        const int instanceCount = 8;
        const int iterations = 128;
        var intAccessor = new IntCompositeEventStatusAccessor<AccessorSaga>(
            GetProperty<AccessorSaga>(nameof(AccessorSaga.IntStatus)));
        var structAccessor = new StructCompositeEventStatusAccessor<AccessorSaga>(
            GetProperty<AccessorSaga>(nameof(AccessorSaga.StructStatus)));
        AccessorSaga[] instances = Enumerable.Range(1, instanceCount).Select(_ => new AccessorSaga()).ToArray();
        using var entrants = new CountdownEvent(instanceCount);
        using var release = new ManualResetEventSlim();

        Task[] executions = instances.Select((instance, index) => Task.Factory.StartNew(() =>
        {
            entrants.Signal();
            release.Wait();
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                var intStatus = new CompositeEventStatus(index | int.MinValue | (iteration << 8));
                var structStatus = new CompositeEventStatus(~(index | (iteration << 8)));
                intAccessor.Set(instance, intStatus);
                structAccessor.Set(instance, structStatus);
                Assert.Equal(intStatus, intAccessor.Get(instance));
                Assert.Equal(structStatus, structAccessor.Get(instance));
            }
        }, TestContext.Current.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        try
        {
            Assert.True(
                entrants.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken),
                "Every accessor task must be ready before release.");
        }
        finally
        {
            release.Set();
        }

        await Task.WhenAll(executions);
        Assert.All(instances, instance =>
        {
            Assert.Equal(iterations, instance.IntReadCount);
            Assert.Equal(iterations, instance.IntWriteCount);
            Assert.Equal(iterations, instance.StructReadCount);
            Assert.Equal(iterations, instance.StructWriteCount);
        });
    }

    private static void AssertAccessorClass(Type openType, NullabilityInfoContext nullability)
    {
        Assert.True(openType.IsClass);
        Assert.True(openType.IsPublic);
        Assert.False(openType.IsAbstract);
        Assert.False(openType.IsSealed);
        Type argument = Assert.Single(openType.GetGenericArguments());
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint, argument.GenericParameterAttributes);
        Type[] expectedInterfaces =
        [
            typeof(ICompositeEventStatusAccessor<>).MakeGenericType(argument),
            typeof(IProbeSite),
        ];
        Assert.Equal(
            expectedInterfaces.OrderBy(type => type.FullName, StringComparer.Ordinal),
            openType.GetInterfaces().OrderBy(type => type.FullName, StringComparer.Ordinal));
        Assert.Empty(openType.GetFields(DeclaredPublicInstance));
        Assert.Empty(openType.GetProperties(DeclaredPublicInstance));
        Assert.Empty(openType.GetEvents(DeclaredPublicInstance));

        ConstructorInfo constructor = Assert.Single(openType.GetConstructors(DeclaredPublicInstance));
        ParameterInfo constructorParameter = Assert.Single(constructor.GetParameters());
        Assert.Equal(typeof(PropertyInfo), constructorParameter.ParameterType);
        Assert.Equal("propertyInfo", constructorParameter.Name);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(constructorParameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(constructorParameter).WriteState);

        MethodInfo[] methods = openType.GetMethods(DeclaredPublicInstance);
        Assert.Equal(3, methods.Length);
        Assert.All(methods, method =>
        {
            Assert.True(method.IsVirtual);
            Assert.True(method.IsFinal);
            Assert.Same(openType, method.GetBaseDefinition().DeclaringType);
        });
        AssertMethod(methods, "Get", typeof(CompositeEventStatus), [argument], ["instance"]);
        AssertMethod(methods, "Set", typeof(void), [argument, typeof(CompositeEventStatus)], ["instance", "status"]);
        AssertMethod(methods, "Probe", typeof(void), [typeof(ProbeContext)], ["context"]);
        Assert.All(
            methods.SelectMany(method => method.GetParameters()).Where(parameter => !parameter.ParameterType.IsValueType),
            parameter =>
            {
                NullabilityInfo parameterNullability = nullability.Create(parameter);
                Assert.Equal(NullabilityState.NotNull, parameterNullability.ReadState);
                Assert.Equal(NullabilityState.NotNull, parameterNullability.WriteState);
            });
    }

    private static void AssertMethod(
        MethodInfo[] methods,
        string name,
        Type returnType,
        Type[] parameterTypes,
        string[] parameterNames)
    {
        MethodInfo method = Assert.Single(methods, candidate => candidate.Name == name);
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(parameterTypes, method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(parameterNames, method.GetParameters().Select(parameter => parameter.Name));
        Assert.False(method.IsStatic);
    }

    private static void AssertConstructorFailure<TException>(
        Action action,
        string parameterName,
        string? expectedMessage = null)
        where TException : ArgumentException
    {
        TException exception = Assert.Throws<TException>(action);
        Assert.Equal(parameterName, exception.ParamName);
        if (expectedMessage != null)
            Assert.True(
                exception.Message.StartsWith(expectedMessage, StringComparison.Ordinal),
                $"Expected message to start with '{expectedMessage}', actual '{exception.Message}'.");
    }

    private static PropertyInfo GetProperty<T>(string name, Type[]? indexParameterTypes = null)
    {
        if (indexParameterTypes == null)
            return typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;

        return typeof(T).GetProperty(
            name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
            binder: null,
            returnType: null,
            types: indexParameterTypes,
            modifiers: null)!;
    }

    private static PropertyInfo GetNonPublicProperty<T>(string name) =>
        typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static RecordingProbeContext CreateProbe(int? failureAttempt = null, Exception? failure = null)
    {
        ProbeContext context = DispatchProxy.Create<ProbeContext, RecordingProbeContext>();
        var probe = (RecordingProbeContext)(object)context;
        probe.Context = context;
        probe.FailureAttempt = failureAttempt;
        probe.Failure = failure;
        return probe;
    }

    private sealed class AccessorSaga
    {
        private int _intStatus;
        private CompositeEventStatus _structStatus;

        public int IntReadCount { get; private set; }

        public int IntWriteCount { get; private set; }

        public int StructReadCount { get; private set; }

        public int StructWriteCount { get; private set; }

        public int IntStatus
        {
            get
            {
                IntReadCount++;
                return _intStatus;
            }
            set
            {
                IntWriteCount++;
                _intStatus = value;
            }
        }

        public CompositeEventStatus StructStatus
        {
            get
            {
                StructReadCount++;
                return _structStatus;
            }
            set
            {
                StructWriteCount++;
                _structStatus = value;
            }
        }
    }

    private sealed class ForeignSaga
    {
        public int IntStatus { get; set; }

        public CompositeEventStatus StructStatus { get; set; }
    }

    private sealed class InvalidDescriptorSaga
    {
        public static int StaticIntStatus { get; set; }

        public static CompositeEventStatus StaticStructStatus { get; set; }

        public int ReadOnlyIntStatus => 0;

        public CompositeEventStatus ReadOnlyStructStatus => default;

        public int WriteOnlyIntStatus
        {
            set { }
        }

        public CompositeEventStatus WriteOnlyStructStatus
        {
            set { }
        }

        public int this[int index]
        {
            get => index;
            set { }
        }

        public CompositeEventStatus this[string index]
        {
            get => new(index.Length);
            set { }
        }
    }

    private class BaseStatusSaga
    {
        public int IntStatus { get; set; }

        public CompositeEventStatus StructStatus { get; set; }
    }

    private sealed class HiddenStatusSaga : BaseStatusSaga
    {
        public new int IntStatus { get; set; }

        public new CompositeEventStatus StructStatus { get; set; }
    }

    private sealed class NonPublicStatusSaga
    {
        private int IntStatus { get; set; }

        private CompositeEventStatus StructStatus { get; set; }
    }

    private interface IStatusSaga
    {
        int IntStatus { get; set; }

        CompositeEventStatus StructStatus { get; set; }
    }

    private sealed class StatusSaga : IStatusSaga
    {
        public int IntStatus { get; set; }

        public CompositeEventStatus StructStatus { get; set; }
    }

    private sealed class ThrowingStatusSaga
    {
        public InvalidOperationException IntReadFailure { get; } = new("int read failure");

        public ArgumentException IntWriteFailure { get; } = new("int write failure");

        public ApplicationException StructReadFailure { get; } = new("struct read failure");

        public NotSupportedException StructWriteFailure { get; } = new("struct write failure");

        public int IntReadStatus
        {
            get => throw IntReadFailure;
            set { }
        }

        public int IntWriteStatus
        {
            get => default;
            set => throw IntWriteFailure;
        }

        public CompositeEventStatus StructReadStatus
        {
            get => throw StructReadFailure;
            set { }
        }

        public CompositeEventStatus StructWriteStatus
        {
            get => default;
            set => throw StructWriteFailure;
        }
    }

    private sealed class NonPublicThrowingStatusSaga
    {
        public InvalidOperationException IntReadFailure { get; } = new("private int read failure");

        public ArgumentException IntWriteFailure { get; } = new("private int write failure");

        public ApplicationException StructReadFailure { get; } = new("private struct read failure");

        public NotSupportedException StructWriteFailure { get; } = new("private struct write failure");

        private int IntReadStatus
        {
            get => throw IntReadFailure;
            set { }
        }

        private int IntWriteStatus
        {
            get => default;
            set => throw IntWriteFailure;
        }

        private CompositeEventStatus StructReadStatus
        {
            get => throw StructReadFailure;
            set { }
        }

        private CompositeEventStatus StructWriteStatus
        {
            get => default;
            set => throw StructWriteFailure;
        }
    }

    private class RecordingProbeContext : DispatchProxy
    {
        public ProbeContext Context { get; set; } = null!;

        public Exception? Failure { get; set; }

        public int? FailureAttempt { get; set; }

        public List<(string Key, object? Value)> Attempts { get; } = [];

        public List<(string Key, object? Value)> Values { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ProbeContext.Add) && args is { Length: 2 } && args[0] is string key)
            {
                var entry = (key, args[1]);
                Attempts.Add(entry);
                if (Attempts.Count == FailureAttempt)
                    throw Failure ?? new InvalidOperationException("Configured probe failure is missing.");
                Values.Add(entry);
            }

            if (targetMethod?.ReturnType == typeof(void) || targetMethod == null)
                return null;

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}
