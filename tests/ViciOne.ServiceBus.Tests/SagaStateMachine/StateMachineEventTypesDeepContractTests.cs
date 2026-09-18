using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineEventTypesDeepContractTests
{
    const BindingFlags DeclaredPublic = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-event-types-exact-public-surface")]
    public void PublicSurface_ExposesOnlyTheCanonicalSynchronousEventContracts()
    {
        Type triggerType = typeof(TriggerEvent);
        Type messageType = typeof(MessageEvent<EventMessage>);

        Assert.True(triggerType.IsPublic);
        Assert.False(triggerType.IsAbstract);
        Assert.False(triggerType.IsSealed);
        Assert.Same(typeof(object), triggerType.BaseType);
        Assert.Equal(
            SortedTypes(typeof(IComparable<IEvent>), typeof(IEvent), typeof(IProbeSite), typeof(IVisitable)),
            SortedTypes(triggerType.GetInterfaces()));

        Assert.True(messageType.IsPublic);
        Assert.False(messageType.IsAbstract);
        Assert.False(messageType.IsSealed);
        Assert.Same(triggerType, messageType.BaseType);
        Assert.Equal(
            SortedTypes(
                typeof(IComparable<IEvent>),
                typeof(IEquatable<MessageEvent<EventMessage>>),
                typeof(IEvent),
                typeof(IEvent<EventMessage>),
                typeof(IProbeSite),
                typeof(IVisitable)),
            SortedTypes(messageType.GetInterfaces()));

        Type messageParameter = Assert.Single(typeof(MessageEvent<>).GetGenericArguments());
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            messageParameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Empty(messageParameter.GetGenericParameterConstraints());

        var nullability = new NullabilityInfoContext();
        AssertConstructor(triggerType, nullability);
        AssertConstructor(messageType, nullability);

        PropertyInfo name = Assert.Single(triggerType.GetProperties(DeclaredPublic));
        Assert.Equal("Name", name.Name);
        Assert.Equal(typeof(string), name.PropertyType);
        Assert.True(name.GetMethod!.IsPublic);
        Assert.True(name.GetMethod.IsVirtual);
        Assert.True(name.GetMethod.IsFinal);
        Assert.Same(triggerType, name.GetMethod.GetBaseDefinition().DeclaringType);
        Assert.False(name.CanWrite);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(name).ReadState);
        Assert.Empty(messageType.GetProperties(DeclaredPublic));

        Assert.Empty(triggerType.GetFields(DeclaredPublic));
        FieldInfo instance = Assert.Single(messageType.GetFields(DeclaredPublic));
        Assert.Equal("Instance", instance.Name);
        Assert.Equal(typeof(IEvent<EventMessage>), instance.FieldType);
        Assert.True(instance.IsPublic);
        Assert.True(instance.IsStatic);
        Assert.True(instance.IsInitOnly);
        Assert.False(instance.IsLiteral);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(instance).ReadState);

        Assert.Equal(7, triggerType.GetMethods(DeclaredPublic).Count(method => !method.IsSpecialName));
        AssertMethod(triggerType, "Accept", typeof(void), [typeof(IStateMachineVisitor)], ["visitor"], [NullabilityState.NotNull], triggerType);
        AssertMethod(triggerType, "Probe", typeof(void), [typeof(ProbeContext)], ["context"], [NullabilityState.NotNull], triggerType);
        AssertMethod(
            triggerType,
            "CompareTo",
            typeof(int),
            [typeof(IEvent)],
            ["other"],
            [NullabilityState.Nullable],
            baseDefinitionOwner: triggerType,
            isFinal: true);
        AssertMethod(triggerType, "Equals", typeof(bool), [typeof(TriggerEvent)], ["other"], [NullabilityState.Nullable]);
        AssertMethod(triggerType, "Equals", typeof(bool), [typeof(object)], ["obj"], [NullabilityState.Nullable], typeof(object));
        AssertMethod(triggerType, "GetHashCode", typeof(int), [], [], [], typeof(object));
        AssertMethod(triggerType, "ToString", typeof(string), [], [], [], typeof(object), NullabilityState.NotNull);

        Assert.Equal(6, messageType.GetMethods(DeclaredPublic).Count(method => !method.IsSpecialName));
        AssertMethod(messageType, "Accept", typeof(void), [typeof(IStateMachineVisitor)], ["visitor"], [NullabilityState.NotNull], triggerType);
        AssertMethod(messageType, "Probe", typeof(void), [typeof(ProbeContext)], ["context"], [NullabilityState.NotNull], triggerType);
        AssertMethod(
            messageType,
            "Equals",
            typeof(bool),
            [typeof(MessageEvent<EventMessage>)],
            ["other"],
            [NullabilityState.Nullable],
            baseDefinitionOwner: messageType,
            isFinal: true);
        AssertMethod(messageType, "Equals", typeof(bool), [typeof(object)], ["obj"], [NullabilityState.Nullable], typeof(object));
        AssertMethod(messageType, "GetHashCode", typeof(int), [], [], [], typeof(object));
        AssertMethod(messageType, "ToString", typeof(string), [], [], [], typeof(object), NullabilityState.NotNull);

        Assert.Empty(triggerType.GetEvents(DeclaredPublic));
        Assert.Empty(messageType.GetEvents(DeclaredPublic));
        Assert.DoesNotContain(triggerType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic), IsPublicOrProtected);
        Assert.DoesNotContain(messageType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic), IsPublicOrProtected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-event-types-name-null-and-singleton-ownership")]
    public void Construction_PreservesExactNamesAndOwnsOneSingletonPerMessageType()
    {
        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => new TriggerEvent(null!)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => new MessageEvent<EventMessage>(null!)).ParamName);

        string name = string.Concat(" ", "Ready", " ");
        var trigger = new TriggerEvent(name);
        var message = new MessageEvent<EventMessage>(name);

        Assert.Same(name, trigger.Name);
        Assert.Same(name, message.Name);
        Assert.Equal(string.Empty, new TriggerEvent(string.Empty).Name);
        Assert.Equal(string.Empty, new MessageEvent<EventMessage>(string.Empty).Name);

        IEvent<EventMessage> instance = MessageEvent<EventMessage>.Instance;
        MessageEvent<EventMessage> concrete = Assert.IsType<MessageEvent<EventMessage>>(instance);
        Assert.Same(instance, MessageEvent<EventMessage>.Instance);
        Assert.Equal(TypeCache<EventMessage>.ShortName, concrete.Name);
        Assert.NotSame(instance, MessageEvent<OtherMessage>.Instance);
        Assert.Equal($"{TypeCache<EventMessage>.ShortName}<{nameof(EventMessage)}> (Event)", concrete.ToString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-event-types-equality-order-and-display-semantics")]
    public void EqualityOrderingAndDisplay_PreserveOrdinalAndSupportedCrossTypeSemantics()
    {
        var trigger = new TriggerEvent("Ready");
        var equivalentTrigger = new TriggerEvent("Ready");
        var differentCaseTrigger = new TriggerEvent("ready");
        var message = new MessageEvent<EventMessage>("Ready");
        var equivalentMessage = new MessageEvent<EventMessage>("Ready");
        var differentCaseMessage = new MessageEvent<EventMessage>("ready");
        var derivedMessage = new DerivedMessageEvent("Ready");
        var otherMessageType = new MessageEvent<OtherMessage>("Ready");

        Assert.True(trigger.Equals(trigger));
        Assert.True(trigger.Equals(equivalentTrigger));
        Assert.False(trigger.Equals(differentCaseTrigger));
        Assert.Equal(StringComparer.Ordinal.GetHashCode("Ready"), trigger.GetHashCode());
        Assert.False(trigger.Equals((TriggerEvent?)null));
        Assert.True(trigger.Equals((object)trigger));
        Assert.True(trigger.Equals((object)equivalentTrigger));
        Assert.False(trigger.Equals((object)differentCaseTrigger));
        Assert.False(trigger.Equals((object?)null));
        Assert.True(trigger.Equals((TriggerEvent)message));
        Assert.True(message.Equals((TriggerEvent)trigger));
        Assert.False(trigger.Equals((object)message));
        Assert.False(message.Equals((object)trigger));
        Assert.False(trigger.Equals("Ready"));
        Assert.False(EqualityComparer<TriggerEvent>.Default.Equals(trigger, message));
        Assert.Equal(trigger.GetHashCode(), equivalentTrigger.GetHashCode());

        Assert.True(message.Equals(message));
        Assert.True(message.Equals(equivalentMessage));
        Assert.True(message.Equals(derivedMessage));
        Assert.True(message.Equals((object)message));
        Assert.True(message.Equals((object)derivedMessage));
        Assert.False(message.Equals(differentCaseMessage));
        Assert.False(message.Equals((object)differentCaseMessage));
        Assert.False(message.Equals((MessageEvent<EventMessage>?)null));
        Assert.False(message.Equals((object?)null));
        Assert.True(message.Equals((TriggerEvent)otherMessageType));
        Assert.False(message.Equals((object)otherMessageType));
        Assert.Equal(message.GetHashCode(), equivalentMessage.GetHashCode());
        Assert.Equal(message.GetHashCode(), derivedMessage.GetHashCode());
        Assert.Equal(
            unchecked(trigger.GetHashCode() * 27 + typeof(EventMessage).GetHashCode()),
            message.GetHashCode());

        Assert.Equal(0, trigger.CompareTo(equivalentTrigger));
        Assert.Equal(0, trigger.CompareTo(message));
        Assert.Equal(string.CompareOrdinal(trigger.Name, differentCaseTrigger.Name), trigger.CompareTo(differentCaseTrigger));
        Assert.Equal(1, trigger.CompareTo(null));
        Assert.Equal("Ready (Event)", trigger.ToString());
        Assert.Equal("Ready<EventMessage> (Event)", message.ToString());
        Assert.Equal(" (Event)", new TriggerEvent(string.Empty).ToString());
        Assert.Equal("<EventMessage> (Event)", new MessageEvent<EventMessage>(string.Empty).ToString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-event-types-visitor-overload-and-failure-identity")]
    public void Accept_SelectsTheExactVisitorOverloadAndPreservesFailureIdentity()
    {
        var trigger = new TriggerEvent("Trigger");
        var message = new MessageEvent<EventMessage>("Message");

        Assert.Equal("visitor", Assert.Throws<ArgumentNullException>(() => trigger.Accept(null!)).ParamName);
        Assert.Equal("visitor", Assert.Throws<ArgumentNullException>(() => message.Accept(null!)).ParamName);

        var triggerVisitor = new RecordingVisitor();
        trigger.Accept(triggerVisitor);
        Assert.Equal(1, triggerVisitor.UntypedEventCalls);
        Assert.Equal(0, triggerVisitor.MessageEventCalls);
        Assert.Equal(1, triggerVisitor.ContinuationCalls);
        Assert.Same(trigger, triggerVisitor.VisitedEvent);
        Assert.Null(triggerVisitor.VisitedMessageType);

        var messageVisitor = new RecordingVisitor();
        message.Accept(messageVisitor);
        Assert.Equal(0, messageVisitor.UntypedEventCalls);
        Assert.Equal(1, messageVisitor.MessageEventCalls);
        Assert.Equal(1, messageVisitor.ContinuationCalls);
        Assert.Same(message, messageVisitor.VisitedEvent);
        Assert.Same(typeof(EventMessage), messageVisitor.VisitedMessageType);

        var failure = new InvalidOperationException("visitor failed");
        var triggerFailureVisitor = new RecordingVisitor(failure);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => trigger.Accept(triggerFailureVisitor)));
        Assert.Equal(1, triggerFailureVisitor.UntypedEventCalls);
        Assert.Equal(0, triggerFailureVisitor.ContinuationCalls);

        var messageFailureVisitor = new RecordingVisitor(failure);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => message.Accept(messageFailureVisitor)));
        Assert.Equal(1, messageFailureVisitor.MessageEventCalls);
        Assert.Equal(0, messageFailureVisitor.ContinuationCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-event-types-probe-order-data-type-and-failure-identity")]
    public void Probe_WritesExactOrderedMetadataAndPreservesFailureIdentity()
    {
        var trigger = new TriggerEvent("Trigger");
        var message = new MessageEvent<EventMessage>("Message");

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => trigger.Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => message.Probe(null!)).ParamName);

        var triggerContext = new RecordingProbeContext();
        trigger.Probe(triggerContext);
        Assert.Collection(
            triggerContext.Entries,
            entry => Assert.Equal(new ProbeEntry("name", "Trigger", true), entry));

        var messageContext = new RecordingProbeContext();
        message.Probe(messageContext);
        Assert.Collection(
            messageContext.Entries,
            entry => Assert.Equal(new ProbeEntry("name", "Message", true), entry),
            entry => Assert.Equal(new ProbeEntry("dataType", TypeCache<EventMessage>.ShortName, true), entry));

        var firstFailure = new InvalidOperationException("first add failed");
        var firstFailureContext = new RecordingProbeContext(1, firstFailure);
        Assert.Same(firstFailure, Assert.Throws<InvalidOperationException>(() => message.Probe(firstFailureContext)));
        Assert.Collection(
            firstFailureContext.Attempts,
            entry => Assert.Equal(new ProbeEntry("name", "Message", true), entry));
        Assert.Empty(firstFailureContext.Entries);

        var secondFailure = new InvalidOperationException("second add failed");
        var secondFailureContext = new RecordingProbeContext(2, secondFailure);
        Assert.Same(secondFailure, Assert.Throws<InvalidOperationException>(() => message.Probe(secondFailureContext)));
        Assert.Collection(
            secondFailureContext.Attempts,
            entry => Assert.Equal(new ProbeEntry("name", "Message", true), entry),
            entry => Assert.Equal(new ProbeEntry("dataType", TypeCache<EventMessage>.ShortName, true), entry));
        Assert.Collection(
            secondFailureContext.Entries,
            entry => Assert.Equal(new ProbeEntry("name", "Message", true), entry));
    }

    private static void AssertConstructor(Type type, NullabilityInfoContext nullability)
    {
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(DeclaredPublic));
        ParameterInfo parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal(typeof(string), parameter.ParameterType);
        Assert.Equal("name", parameter.Name);
        Assert.False(parameter.IsOptional);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState);
    }

    private static void AssertMethod(
        Type owner,
        string name,
        Type returnType,
        Type[] parameterTypes,
        string[] parameterNames,
        NullabilityState[] parameterNullability,
        Type? baseDefinitionOwner = null,
        NullabilityState? returnNullability = null,
        bool isFinal = false)
    {
        MethodInfo method = Assert.Single(owner.GetMethods(DeclaredPublic), candidate =>
            candidate.Name == name
            && candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(parameterNames, method.GetParameters().Select(parameter => parameter.Name));

        var nullability = new NullabilityInfoContext();
        Assert.Equal(
            parameterNullability,
            method.GetParameters().Select(parameter => nullability.Create(parameter).ReadState));

        if (baseDefinitionOwner is null)
        {
            Assert.False(method.IsVirtual);
        }
        else
        {
            Assert.True(method.IsVirtual);
            Assert.Equal(isFinal, method.IsFinal);
            Assert.Same(baseDefinitionOwner, method.GetBaseDefinition().DeclaringType);
        }

        if (returnNullability.HasValue)
            Assert.Equal(returnNullability.Value, nullability.Create(method.ReturnParameter).ReadState);
    }

    private static Type[] SortedTypes(params Type[] types) =>
        types.OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();

    private static bool IsPublicOrProtected(Type type) =>
        type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem || type.IsNestedFamANDAssem;

    private sealed class RecordingVisitor(Exception? failure = null) : IStateMachineVisitor
    {
        public int ContinuationCalls { get; private set; }
        public int MessageEventCalls { get; private set; }
        public int UntypedEventCalls { get; private set; }
        public IEvent? VisitedEvent { get; private set; }
        public Type? VisitedMessageType { get; private set; }

        public void Visit(IEvent @event, Action<IEvent> next)
        {
            UntypedEventCalls++;
            VisitedEvent = @event;
            if (failure is not null)
                throw failure;

            next(@event);
            ContinuationCalls++;
        }

        public void Visit<TMessage>(IEvent<TMessage> @event, Action<IEvent<TMessage>> next)
            where TMessage : class
        {
            MessageEventCalls++;
            VisitedEvent = @event;
            VisitedMessageType = typeof(TMessage);
            if (failure is not null)
                throw failure;

            next(@event);
            ContinuationCalls++;
        }

        public void Visit(IState state, Action<IState> next) => throw Unexpected();
        public void Visit(IStateMachineActivity activity) => throw Unexpected();
        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => throw Unexpected();
        public void Visit<T>(IBehavior<T> behavior)
            where T : class, ISagaStateMachineInstance => throw Unexpected();
        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
            where T : class, ISagaStateMachineInstance => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
            where T : class, ISagaStateMachineInstance
            where TMessage : class => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
            where T : class, ISagaStateMachineInstance
            where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next) => throw Unexpected();

        private static Exception Unexpected() => new InvalidOperationException("An unexpected visitor overload was selected.");
    }

    private sealed class RecordingProbeContext(int failAtCall = 0, Exception? failure = null) : ProbeContext
    {
        public List<ProbeEntry> Attempts { get; } = [];
        public CancellationToken CancellationToken => default;
        public List<ProbeEntry> Entries { get; } = [];

        public void Add(string key, string? value) => AddCore(new ProbeEntry(key, value, true));
        public void Add(string key, object? value) => AddCore(new ProbeEntry(key, value, false));
        public void Set(object values) => throw new InvalidOperationException("Set was not expected.");
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw new InvalidOperationException("Set was not expected.");
        public ProbeContext CreateScope(string key) => throw new InvalidOperationException("CreateScope was not expected.");

        private void AddCore(ProbeEntry entry)
        {
            Attempts.Add(entry);
            if (Attempts.Count == failAtCall)
                throw failure ?? new InvalidOperationException("Synthetic probe failure.");

            Entries.Add(entry);
        }
    }

    private sealed class DerivedMessageEvent(string name) : MessageEvent<EventMessage>(name);
    private sealed record EventMessage;
    private sealed record OtherMessage;
    private sealed record ProbeEntry(string Key, object? Value, bool UsedStringOverload);
}
