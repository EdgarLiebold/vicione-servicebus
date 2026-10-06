using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class StateMachineRequestScheduleTimeoutDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST-SETTINGS", "one-response-defaults-settings-and-storage-identity")]
    public void RequestConfigurator_DefaultsSettingsAndStoresEveryOneResponseValueExactly()
    {
        var configurator = new StateMachineRequestConfigurator<RequestState, RequestMessage, ResponseOne>();
        var serviceAddress = new Uri("loopback://request-service");
        Action<IEventCorrelationConfigurator<RequestState, ResponseOne>> completed = _ => { };
        Action<IEventCorrelationConfigurator<RequestState, Fault<RequestMessage>>> faulted = _ => { };
        Action<IEventCorrelationConfigurator<RequestState, IRequestTimeoutExpired<RequestMessage>>> timeoutExpired = _ => { };

        Assert.Same(configurator, configurator.Settings);
        Assert.Equal(TimeSpan.FromSeconds(30), configurator.Timeout);
        Assert.False(configurator.ClearRequestIdOnFaulted);
        Assert.Null(configurator.TimeToLive);

        configurator.ServiceAddress = serviceAddress;
        configurator.Timeout = TimeSpan.FromSeconds(17);
        configurator.ClearRequestIdOnFaulted = true;
        configurator.TimeToLive = TimeSpan.FromSeconds(19);
        configurator.Completed = completed;
        configurator.Faulted = faulted;
        configurator.TimeoutExpired = timeoutExpired;

        IRequestSettings<RequestState, RequestMessage, ResponseOne> settings = configurator.Settings;
        Assert.Same(serviceAddress, settings.ServiceAddress);
        Assert.Equal(TimeSpan.FromSeconds(17), settings.Timeout);
        Assert.True(settings.ClearRequestIdOnFaulted);
        Assert.Equal(TimeSpan.FromSeconds(19), settings.TimeToLive);
        Assert.Same(completed, settings.Completed);
        Assert.Same(faulted, settings.Faulted);
        Assert.Same(timeoutExpired, settings.TimeoutExpired);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST-SETTINGS", "multi-response-inheritance-settings-and-callback-identity")]
    public void MultiResponseConfigurators_PreserveInheritedSettingsAndAdditionalCallbacks()
    {
        var two = new StateMachineRequestConfigurator<RequestState, RequestMessage, ResponseOne, ResponseTwo>();
        var three = new StateMachineRequestConfigurator<RequestState, RequestMessage, ResponseOne, ResponseTwo, ResponseThree>();
        Action<IEventCorrelationConfigurator<RequestState, ResponseTwo>> completed2 = _ => { };
        Action<IEventCorrelationConfigurator<RequestState, ResponseThree>> completed3 = _ => { };

        two.Timeout = TimeSpan.FromSeconds(11);
        two.Completed2 = completed2;
        three.Timeout = TimeSpan.FromSeconds(13);
        three.Completed2 = completed2;
        three.Completed3 = completed3;

        Assert.Same(two, two.Settings);
        Assert.Same(three, three.Settings);
        Assert.Equal(TimeSpan.FromSeconds(11), two.Settings.Timeout);
        Assert.Same(completed2, two.Settings.Completed2);
        Assert.Equal(TimeSpan.FromSeconds(13), three.Settings.Timeout);
        Assert.Same(completed2, three.Settings.Completed2);
        Assert.Same(completed3, three.Settings.Completed3);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "default-fixed-custom-delay-and-received-storage")]
    public void ScheduleConfigurator_ProjectsDefaultFixedCustomDelayAndReceivedCallbackExactly()
    {
        var configurator = new StateMachineScheduleConfigurator<RequestState, ScheduledMessage>();
        Action<IEventCorrelationConfigurator<RequestState, ScheduledMessage>> received = _ => { };

        Assert.Same(configurator, configurator.Settings);
        Assert.Equal(TimeSpan.FromSeconds(30), configurator.Settings.DelayProvider(null!));

        configurator.Delay = TimeSpan.FromSeconds(23);
        Assert.Equal(TimeSpan.FromSeconds(23), configurator.Settings.DelayProvider(null!));

        ScheduleDelayProvider<RequestState> provider = _ => TimeSpan.FromSeconds(29);
        configurator.DelayProvider = provider;
        ((IScheduleConfigurator<RequestState, ScheduledMessage>)configurator).Received = received;

        Assert.Same(provider, configurator.Settings.DelayProvider);
        Assert.Same(received, configurator.Settings.Received);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-TIMEOUT-ADAPTER", "constructor-required-owner-boundaries")]
    public void TimeoutObserver_RejectsMissingConfiguratorAndCallbackAtConstruction()
    {
        var configurator = new RecordingSagaConfigurator();
        Action<ITimeoutConfigurator> configure = _ => { };

        AssertConstructorArgument("configurator", () => CreateTimeoutObserver(null!, configure));
        AssertConstructorArgument("configure", () => CreateTimeoutObserver(configurator, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-TIMEOUT-ADAPTER", "non-message-notifications-are-noops")]
    public void TimeoutObserver_NonMessageNotificationsAreSideEffectFree()
    {
        var configurator = new RecordingSagaConfigurator();
        var callbackCalls = 0;
        ISagaConfigurationObserver observer = CreateTimeoutObserver(configurator, _ => callbackCalls++);

        observer.SagaConfigured(StrictStub<ISagaConfigurator<OtherSaga>>());
        observer.StateMachineSagaConfigured(StrictStub<ISagaConfigurator<OtherSaga>>(), new object());

        Assert.Equal(0, callbackCalls);
        Assert.Equal(0, configurator.MessageCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-TIMEOUT-ADAPTER", "message-callback-and-single-specification")]
    public void TimeoutObserver_ConfiguresAndAddsOneTimeoutSpecificationForTheObservedMessage()
    {
        var configurator = new RecordingSagaConfigurator();
        var callbackCalls = 0;
        var timeProvider = new ManualTimeProvider();
        ISagaConfigurationObserver observer = CreateTimeoutObserver(configurator, timeout =>
        {
            callbackCalls++;
            timeout.Timeout = TimeSpan.FromSeconds(31);
            timeout.TimeProvider = timeProvider;
        });

        observer.SagaMessageConfigured(StrictStub<ISagaMessageConfigurator<OtherSaga, TimeoutMessage>>());

        Assert.Equal(1, callbackCalls);
        Assert.Equal(1, configurator.MessageCalls);
        object specification = Assert.Single(configurator.Specifications);
        Assert.Equal("TimeoutSpecification`1", specification.GetType().Name);
        Assert.Equal(TimeSpan.FromSeconds(31), ReadProperty<TimeSpan>(specification, "Timeout"));
        Assert.Same(timeProvider, ReadField<TimeProvider>(specification, "_timeProvider"));
    }


    [Theory]
    [InlineData(1, "Completed")]
    [InlineData(1, "Faulted")]
    [InlineData(1, "TimeoutExpired")]
    [InlineData(2, "Completed2")]
    [InlineData(3, "Completed3")]
    [InlineData(0, "Received")]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST-SETTINGS", "optional-callback-default-reset-and-public-nullability")]
    public void OptionalCorrelationCallback_NullabilityMatchesDefaultResetAndActualDeclaration(int family, string member)
    {
        var observations = new List<object>();
        (object configurator, Type settingsType, Type configuratorType, Func<object> declare) = CreateCallbackProjection(family);
        PropertyInfo settingsProperty = Assert.IsAssignableFrom<PropertyInfo>(settingsType.GetProperty(member));
        PropertyInfo setterProperty = Assert.IsAssignableFrom<PropertyInfo>(configuratorType.GetProperty(member));
        PropertyInfo? concreteProperty = family == 0 ? null : configurator.GetType().GetProperty(member);
        if (family != 0)
            Assert.NotNull(concreteProperty);
        Assert.Null(settingsProperty.GetValue(configurator));
        if (concreteProperty is not null)
            Assert.Null(concreteProperty.GetValue(configurator));

        Delegate callback = member switch
        {
            "Completed" => CaptureCallback<ResponseOne>(observations),
            "Faulted" => CaptureCallback<Fault<RequestMessage>>(observations),
            "TimeoutExpired" => CaptureCallback<IRequestTimeoutExpired<RequestMessage>>(observations),
            "Completed2" => CaptureCallback<ResponseTwo>(observations),
            "Completed3" => CaptureCallback<ResponseThree>(observations),
            "Received" => CaptureCallback<ScheduledMessage>(observations),
            _ => throw new ArgumentOutOfRangeException(nameof(member)),
        };
        setterProperty.SetValue(configurator, callback);
        Assert.Same(callback, settingsProperty.GetValue(configurator));
        if (concreteProperty is not null)
            Assert.Same(callback, concreteProperty.GetValue(configurator));
        object declaredWithCallback = declare();
        Assert.NotNull(declaredWithCallback);
        object correlation = Assert.Single(observations);
        Type actualCallbackContextType = callback.GetType().GetGenericArguments()[0];
        Assert.True(actualCallbackContextType.IsInstanceOfType(correlation));

        setterProperty.SetValue(configurator, null);
        Assert.Null(settingsProperty.GetValue(configurator));
        if (concreteProperty is not null)
            Assert.Null(concreteProperty.GetValue(configurator));
        object declaredWithoutCallback = declare();
        Assert.NotSame(declaredWithCallback, declaredWithoutCallback);
        Assert.Same(correlation, Assert.Single(observations));

        var metadata = new NullabilityInfoContext();
        Assert.Equal(NullabilityState.Nullable, metadata.Create(settingsProperty).ReadState);
        Assert.Equal(NullabilityState.Nullable, metadata.Create(setterProperty).WriteState);
        if (concreteProperty is not null)
        {
            Assert.Equal(NullabilityState.Nullable, metadata.Create(concreteProperty).ReadState);
            Assert.Equal(NullabilityState.Nullable, metadata.Create(concreteProperty).WriteState);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST-SETTINGS", "required-delay-and-declared-events-remain-nonnullable")]
    public void RequiredDelayProviderAndDeclaredRequestEvents_RemainInitializedAndNonNullable()
    {
        var schedule = new StateMachineScheduleConfigurator<RequestState, ScheduledMessage>();
        Assert.NotNull(schedule.Settings.DelayProvider);
        Assert.Equal(TimeSpan.FromSeconds(30), schedule.Settings.DelayProvider(null!));
        var settings = new StateMachineRequestConfigurator<RequestState, RequestMessage, ResponseOne, ResponseTwo, ResponseThree>();
        var machine = new NullableThreeMachine(settings.Settings);
        Assert.NotNull(machine.Fetch.Completed);
        Assert.NotNull(machine.Fetch.Completed2);
        Assert.NotNull(machine.Fetch.Completed3);
        Assert.NotNull(machine.Fetch.Faulted);
        Assert.NotNull(machine.Fetch.TimeoutExpired);
        Assert.NotNull(machine.Fetch.Pending);
        var metadata = new NullabilityInfoContext();
        PropertyInfo delay = Assert.IsAssignableFrom<PropertyInfo>(typeof(IScheduleSettings<RequestState, ScheduledMessage>).GetProperty("DelayProvider"));
        Assert.Equal(NullabilityState.NotNull, metadata.Create(delay).ReadState);
        foreach (string member in new[] { "Completed", "Faulted", "TimeoutExpired", "Pending" })
        {
            PropertyInfo property = Assert.IsAssignableFrom<PropertyInfo>(typeof(IRequest<RequestState, RequestMessage, ResponseOne>).GetProperty(member));
            Assert.Equal(NullabilityState.NotNull, metadata.Create(property).ReadState);
        }
        PropertyInfo second = Assert.IsAssignableFrom<PropertyInfo>(typeof(IRequest<RequestState, RequestMessage, ResponseOne, ResponseTwo>).GetProperty("Completed2"));
        PropertyInfo third = Assert.IsAssignableFrom<PropertyInfo>(typeof(IRequest<RequestState, RequestMessage, ResponseOne, ResponseTwo, ResponseThree>).GetProperty("Completed3"));
        Assert.Equal(NullabilityState.NotNull, metadata.Create(second).ReadState);
        Assert.Equal(NullabilityState.NotNull, metadata.Create(third).ReadState);
    }

    static Action<IEventCorrelationConfigurator<RequestState, T>> CaptureCallback<T>(List<object> observations)
        where T : class => context => observations.Add(context);

    static (object Configurator, Type SettingsType, Type ConfiguratorType, Func<object> Declare) CreateCallbackProjection(int family)
    {
        switch (family)
        {
            case 1:
            {
                var value = new StateMachineRequestConfigurator<RequestState, RequestMessage, ResponseOne>();
                return (value, typeof(IRequestSettings<RequestState, RequestMessage, ResponseOne>),
                    typeof(IRequestConfigurator<RequestState, RequestMessage, ResponseOne>), () => new NullableOneMachine(value.Settings));
            }
            case 2:
            {
                var value = new StateMachineRequestConfigurator<RequestState, RequestMessage, ResponseOne, ResponseTwo>();
                return (value, typeof(IRequestSettings<RequestState, RequestMessage, ResponseOne, ResponseTwo>),
                    typeof(IRequestConfigurator<RequestState, RequestMessage, ResponseOne, ResponseTwo>), () => new NullableTwoMachine(value.Settings));
            }
            case 3:
            {
                var value = new StateMachineRequestConfigurator<RequestState, RequestMessage, ResponseOne, ResponseTwo, ResponseThree>();
                return (value, typeof(IRequestSettings<RequestState, RequestMessage, ResponseOne, ResponseTwo, ResponseThree>),
                    typeof(IRequestConfigurator<RequestState, RequestMessage, ResponseOne, ResponseTwo, ResponseThree>), () => new NullableThreeMachine(value.Settings));
            }
            case 0:
            {
                var value = new StateMachineScheduleConfigurator<RequestState, ScheduledMessage>();
                return (value, typeof(IScheduleSettings<RequestState, ScheduledMessage>),
                    typeof(IScheduleConfigurator<RequestState, ScheduledMessage>), () => new NullableScheduleMachine(value.Settings));
            }
            default: throw new ArgumentOutOfRangeException(nameof(family));
        }
    }

    sealed class NullableOneMachine : ViciOneServiceBusStateMachine<RequestState>
    {
        public NullableOneMachine(IRequestSettings<RequestState, RequestMessage, ResponseOne> settings) =>
            Request(() => Fetch, saga => saga.ActiveRequestId, settings);
        public IRequest<RequestState, RequestMessage, ResponseOne> Fetch { get; private set; } = null!;
    }

    sealed class NullableTwoMachine : ViciOneServiceBusStateMachine<RequestState>
    {
        public NullableTwoMachine(IRequestSettings<RequestState, RequestMessage, ResponseOne, ResponseTwo> settings) =>
            Request(() => Fetch, saga => saga.ActiveRequestId, settings);
        public IRequest<RequestState, RequestMessage, ResponseOne, ResponseTwo> Fetch { get; private set; } = null!;
    }

    sealed class NullableThreeMachine : ViciOneServiceBusStateMachine<RequestState>
    {
        public NullableThreeMachine(IRequestSettings<RequestState, RequestMessage, ResponseOne, ResponseTwo, ResponseThree> settings) =>
            Request(() => Fetch, saga => saga.ActiveRequestId, settings);
        public IRequest<RequestState, RequestMessage, ResponseOne, ResponseTwo, ResponseThree> Fetch { get; private set; } = null!;
    }

    sealed class NullableScheduleMachine : ViciOneServiceBusStateMachine<RequestState>
    {
        public NullableScheduleMachine(IScheduleSettings<RequestState, ScheduledMessage> settings) =>
            Schedule(() => Notice, saga => saga.NoticeTokenId, settings);
        public ISchedule<RequestState, ScheduledMessage> Notice { get; private set; } = null!;
    }

    static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    static void AssertConstructorArgument(string parameterName, Action action)
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(action);
        var exception = Assert.IsType<ArgumentNullException>(invocation.InnerException);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static ISagaConfigurationObserver CreateTimeoutObserver(
        ISagaConfigurator<TimeoutSaga> configurator,
        Action<ITimeoutConfigurator> configure)
    {
        Type openType = typeof(StateMachineRequestConfigurator<,,>).Assembly.GetType(
            "ViciOne.ServiceBus.Configuration.TimeoutSagaConfigurationObserver`1")
            ?? throw new InvalidOperationException("Timeout saga observer type was not found.");
        Type type = openType.MakeGenericType(typeof(TimeoutSaga));
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        return Assert.IsAssignableFrom<ISagaConfigurationObserver>(constructor.Invoke([configurator, configure]));
    }

    static T ReadProperty<T>(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? target.GetType().BaseType?.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The property '{name}' was not found on {target.GetType()}.");
        return Assert.IsType<T>(property.GetValue(target));
    }

    static T ReadField<T>(object target, string name)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return Assert.IsAssignableFrom<T>(field.GetValue(target));
        }

        throw new InvalidOperationException($"The field '{name}' was not found on {target.GetType()}.");
    }

    static T StrictStub<T>()
        where T : class => DispatchProxy.Create<T, StrictDispatchProxy>();

    class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected collaborator call: {targetMethod?.Name}.");
    }

    sealed class RecordingSagaConfigurator : ISagaConfigurator<TimeoutSaga>
    {
        public int MessageCalls { get; private set; }
        public List<object> Specifications { get; } = [];
        public int? ConcurrentMessageLimit { set { } }

        public void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
            where T : class
        {
            Assert.Equal(typeof(TimeoutMessage), typeof(T));
            MessageCalls++;
            var messageConfigurator = new RecordingMessageConfigurator<T>(Specifications);
            configure(messageConfigurator);
        }

        public void SagaMessage<T>(Action<ISagaMessageConfigurator<TimeoutSaga, T>> configure)
            where T : class => throw new NotSupportedException();

        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TimeoutSaga>> specification) =>
            throw new NotSupportedException();

        public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer) =>
            throw new NotSupportedException();

        public T Options<T>(Action<T>? configure = null)
            where T : IOptions, new() => throw new NotSupportedException();

        public T Options<T>(T options, Action<T>? configure = null)
            where T : IOptions => throw new NotSupportedException();

        public bool TryGetOptions<T>(out T options)
            where T : IOptions => throw new NotSupportedException();

        public IEnumerable<T> SelectOptions<T>()
            where T : class => throw new NotSupportedException();
    }

    sealed class RecordingMessageConfigurator<T>(ICollection<object> specifications) : ISagaMessageConfigurator<T>
        where T : class
    {
        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<T>> specification) => specifications.Add(specification);
    }

    sealed class ManualTimeProvider : TimeProvider;

    sealed class RequestState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid? ActiveRequestId { get; set; }
        public Guid? NoticeTokenId { get; set; }
    }

    sealed class RequestMessage;
    sealed class ResponseOne;
    sealed class ResponseTwo;
    sealed class ResponseThree;
    sealed class ScheduledMessage;

    sealed class TimeoutSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    sealed class OtherSaga;
    sealed class TimeoutMessage;
}
