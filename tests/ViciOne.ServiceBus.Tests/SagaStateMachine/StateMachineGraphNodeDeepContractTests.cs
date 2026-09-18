using System.Diagnostics;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineGraphNodeDeepContractTests
{
    const BindingFlags DeclaredPublic =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-node-exact-public-surface-nullability-and-enum-values")]
    public void PublicSurface_ExposesOnlyTheCanonicalImmutableNodeFactoriesAndEnumValues()
    {
        Type nodeType = typeof(StateMachineGraphNode);

        Assert.True(nodeType.IsPublic);
        Assert.True(nodeType.IsClass);
        Assert.True(nodeType.IsSealed);
        Assert.False(nodeType.IsAbstract);
        Assert.Same(typeof(object), nodeType.BaseType);
        Assert.Empty(nodeType.GetInterfaces());
        Assert.Empty(nodeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        Assert.Empty(nodeType.GetFields(DeclaredPublic));
        Assert.Empty(nodeType.GetEvents(DeclaredPublic));
        Assert.DoesNotContain(
            nodeType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic),
            type => type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem);

        ConstructorInfo constructor = Assert.Single(
            nodeType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.True(constructor.IsPrivate);
        Assert.Equal(
            [
                typeof(StateMachineGraphNodeKind),
                typeof(string),
                typeof(Type),
                typeof(Type),
                typeof(bool),
            ],
            constructor.GetParameters().Select(parameter => parameter.ParameterType));

        PropertyInfo[] properties = nodeType.GetProperties(DeclaredPublic);
        Assert.Equal(
            ["ExceptionType", "IsCompositeEvent", "Kind", "MessageType", "Name"],
            properties.Select(property => property.Name).Order(StringComparer.Ordinal));
        AssertProperty(properties, "Kind", typeof(StateMachineGraphNodeKind), NullabilityState.NotNull);
        AssertProperty(properties, "Name", typeof(string), NullabilityState.NotNull);
        AssertProperty(properties, "MessageType", typeof(Type), NullabilityState.Nullable);
        AssertProperty(properties, "ExceptionType", typeof(Type), NullabilityState.Nullable);
        AssertProperty(properties, "IsCompositeEvent", typeof(bool), NullabilityState.NotNull);

        MethodInfo[] methods = nodeType.GetMethods(DeclaredPublic).Where(method => !method.IsSpecialName).ToArray();
        Assert.Equal(
            ["CreateEvent", "CreateException", "CreateState", "ToString"],
            methods.Select(method => method.Name).Order(StringComparer.Ordinal));

        AssertFactory(
            methods,
            "CreateState",
            [typeof(string)],
            ["name"],
            [NullabilityState.NotNull],
            [false],
            [null]);
        AssertFactory(
            methods,
            "CreateEvent",
            [typeof(string), typeof(Type), typeof(bool)],
            ["name", "messageType", "isCompositeEvent"],
            [NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull],
            [false, true, true],
            [null, null, false]);
        AssertFactory(
            methods,
            "CreateException",
            [typeof(Type)],
            ["exceptionType"],
            [NullabilityState.NotNull],
            [false],
            [null]);

        MethodInfo toString = Assert.Single(methods, method => method.Name == nameof(ToString));
        Assert.False(toString.IsStatic);
        Assert.True(toString.IsVirtual);
        Assert.Same(typeof(object), toString.GetBaseDefinition().DeclaringType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(toString.ReturnParameter).ReadState);
        Assert.Empty(toString.GetParameters());

        DebuggerDisplayAttribute debuggerDisplay = Assert.Single(
            nodeType.GetCustomAttributes<DebuggerDisplayAttribute>(inherit: false));
        Assert.Equal("{DebuggerDisplay,nq}", debuggerDisplay.Value);

        Type kindType = typeof(StateMachineGraphNodeKind);
        Assert.True(kindType.IsPublic);
        Assert.True(kindType.IsEnum);
        Assert.True(kindType.IsSealed);
        Assert.Same(typeof(Enum), kindType.BaseType);
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(kindType));
        Assert.False(kindType.IsDefined(typeof(FlagsAttribute), inherit: false));
        Assert.Equal(["State", "Event", "Exception"], Enum.GetNames<StateMachineGraphNodeKind>());
        Assert.Equal(
            [
                StateMachineGraphNodeKind.State,
                StateMachineGraphNodeKind.Event,
                StateMachineGraphNodeKind.Exception,
            ],
            Enum.GetValues<StateMachineGraphNodeKind>());
        Assert.Equal(0, (int)StateMachineGraphNodeKind.State);
        Assert.Equal(1, (int)StateMachineGraphNodeKind.Event);
        Assert.Equal(2, (int)StateMachineGraphNodeKind.Exception);
        Assert.Equal("State", StateMachineGraphNodeKind.State.ToString());
        Assert.Equal("Event", StateMachineGraphNodeKind.Event.ToString());
        Assert.Equal("Exception", StateMachineGraphNodeKind.Exception.ToString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-node-name-precedence-unicode-and-ordinal-display")]
    public void Names_RejectEveryBlankFormBeforeTypeValidationAndPreserveMeaningfulTextOrdinally()
    {
        string?[] invalidNames = [null, string.Empty, " ", "\t", "\r\n", "\u00a0", "\u2003"];

        foreach (string? invalidName in invalidNames)
        {
            ArgumentException stateError = Assert.IsAssignableFrom<ArgumentException>(
                Record.Exception(() => StateMachineGraphNode.CreateState(invalidName!)));
            ArgumentException eventError = Assert.IsAssignableFrom<ArgumentException>(
                Record.Exception(() =>
                    StateMachineGraphNode.CreateEvent(invalidName!, typeof(int), isCompositeEvent: true)));

            Assert.Equal("name", stateError.ParamName);
            Assert.Equal("name", eventError.ParamName);
            Assert.Equal(
                invalidName is null ? typeof(ArgumentNullException) : typeof(ArgumentException),
                stateError.GetType());
            Assert.Equal(
                invalidName is null ? typeof(ArgumentNullException) : typeof(ArgumentException),
                eventError.GetType());
        }

        string decomposedName = string.Concat(" ", "e\u0301", "-İ", " ");
        string normalizedName = string.Concat(" ", "é", "-İ", " ");
        StateMachineGraphNode state = StateMachineGraphNode.CreateState(decomposedName);
        StateMachineGraphNode @event = StateMachineGraphNode.CreateEvent(normalizedName);

        Assert.Same(decomposedName, state.Name);
        Assert.Same(normalizedName, @event.Name);
        Assert.NotEqual(state.Name, @event.Name, StringComparer.Ordinal);
        Assert.Equal($"State: {decomposedName}", state.ToString());
        Assert.Equal($"Event: {normalizedName}", @event.ToString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-event-node-closed-reference-type-matrix")]
    public unsafe void EventFactory_AcceptsEveryClosedReferenceShapeAndRejectsUnsupportedClrTypeShapes()
    {
        Type[] validTypes =
        [
            typeof(object),
            typeof(IMessageContract),
            typeof(AbstractMessage),
            typeof(MessageDelegate),
            typeof(MessageRecord),
            typeof(MessageRecord[]),
            typeof(MessageRecord[,]),
            typeof(GenericMessage<string>),
            typeof(IReadOnlyDictionary<string, MessageRecord>),
        ];

        foreach (Type validType in validTypes)
        {
            StateMachineGraphNode node = StateMachineGraphNode.CreateEvent("Accepted", validType);

            Assert.Same(validType, node.MessageType);
            Assert.Equal(StateMachineGraphNodeKind.Event, node.Kind);
            Assert.Equal($"Event: Accepted ({validType.Name})", node.ToString());
        }

        Type genericParameter = typeof(GenericMessage<>).GetGenericArguments()[0];
        Type partiallyOpen = typeof(Dictionary<,>).MakeGenericType(typeof(string), genericParameter);
        Type[] invalidTypes =
        [
            typeof(void),
            typeof(int),
            typeof(int?),
            typeof(MessageKind),
            typeof(Span<int>),
            typeof(TypedReference),
            typeof(GenericMessage<>),
            genericParameter,
            partiallyOpen,
            typeof(StaticMessage),
            typeof(string).MakeByRefType(),
            typeof(int).MakePointerType(),
            typeof(delegate*<void>),
        ];

        foreach (Type invalidType in invalidTypes)
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() =>
                StateMachineGraphNode.CreateEvent("Rejected", invalidType));

            Assert.Equal("messageType", error.ParamName);
            Assert.StartsWith(
                "The event message type must be a closed, non-static reference type.",
                error.Message,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-exception-node-closed-exception-type-matrix")]
    public unsafe void ExceptionFactory_AcceptsClosedExceptionHierarchiesAndRejectsEveryOtherTypeShape()
    {
        Type[] validTypes =
        [
            typeof(Exception),
            typeof(AbstractGraphException),
            typeof(ConcreteGraphException),
            typeof(GenericGraphException<string>),
        ];

        foreach (Type validType in validTypes)
        {
            StateMachineGraphNode node = StateMachineGraphNode.CreateException(validType);

            Assert.Same(validType, node.ExceptionType);
            Assert.Equal(validType.Name, node.Name);
            Assert.Equal(StateMachineGraphNodeKind.Exception, node.Kind);
            Assert.Equal($"Exception: {validType.Name}", node.ToString());
        }

        Type genericParameter = typeof(ExceptionParameter<>).GetGenericArguments()[0];
        Type partiallyOpen = typeof(PairGraphException<,>).MakeGenericType(typeof(string), genericParameter);
        Type[] invalidTypes =
        [
            typeof(object),
            typeof(IMessageContract),
            typeof(Exception[]),
            typeof(GenericGraphException<>),
            genericParameter,
            partiallyOpen,
            typeof(StaticMessage),
            typeof(Exception).MakeByRefType(),
            typeof(int).MakePointerType(),
            typeof(delegate*<void>),
        ];

        foreach (Type invalidType in invalidTypes)
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() =>
                StateMachineGraphNode.CreateException(invalidType));

            Assert.Equal("exceptionType", error.ParamName);
            Assert.StartsWith(
                "The graph exception type must be a closed type derived from System.Exception.",
                error.Message,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-event-node-composite-occurrence-ownership")]
    public void CompositeFlag_IsOwnedOnlyByEachEventOccurrenceAndDoesNotChangeDisplayOrIdentity()
    {
        foreach (Type? messageType in new Type?[] { null, typeof(MessageRecord) })
        {
            StateMachineGraphNode ordinary = StateMachineGraphNode.CreateEvent("Ready", messageType, false);
            StateMachineGraphNode composite = StateMachineGraphNode.CreateEvent("Ready", messageType, true);

            Assert.False(ordinary.IsCompositeEvent);
            Assert.True(composite.IsCompositeEvent);
            Assert.Equal(ordinary.ToString(), composite.ToString());
            Assert.NotSame(ordinary, composite);
            Assert.NotEqual(ordinary, composite);
        }

        Assert.False(StateMachineGraphNode.CreateState("Ready").IsCompositeEvent);
        Assert.False(StateMachineGraphNode.CreateException(typeof(ConcreteGraphException)).IsCompositeEvent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-node-reference-identity-with-colliding-metadata")]
    public void Nodes_UseObjectIdentityEvenWhenAllMetadataAndNamesCollide()
    {
        string sharedName = new(['D', 'u', 'p', 'l', 'i', 'c', 'a', 't', 'e']);
        StateMachineGraphNode first = StateMachineGraphNode.CreateEvent(sharedName, typeof(MessageRecord), true);
        StateMachineGraphNode second = StateMachineGraphNode.CreateEvent(sharedName, typeof(MessageRecord), true);
        StateMachineGraphNode firstException = StateMachineGraphNode.CreateException(typeof(FirstContainer.CollisionException));
        StateMachineGraphNode secondException = StateMachineGraphNode.CreateException(typeof(SecondContainer.CollisionException));

        Assert.Same(sharedName, first.Name);
        Assert.Same(sharedName, second.Name);
        Assert.True(first.Equals(first));
        Assert.False(first.Equals(second));
        Assert.Equal(2, new HashSet<StateMachineGraphNode> { first, second }.Count);
        Assert.Equal(firstException.Name, secondException.Name);
        Assert.NotSame(firstException.ExceptionType, secondException.ExceptionType);
        Assert.False(firstException.Equals(secondException));

        Type nodeType = typeof(StateMachineGraphNode);
        Assert.DoesNotContain(
            nodeType.GetMethods(DeclaredPublic),
            method => method.Name is nameof(Equals) or nameof(GetHashCode));
        Assert.DoesNotContain(
            nodeType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly),
            method => method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal));
        Assert.Same(typeof(object), nodeType.GetMethod(nameof(Equals), [typeof(object)])!.DeclaringType);
        Assert.Same(typeof(object), nodeType.GetMethod(nameof(GetHashCode), Type.EmptyTypes)!.DeclaringType);
    }

    static void AssertProperty(
        PropertyInfo[] properties,
        string name,
        Type propertyType,
        NullabilityState expectedNullability)
    {
        PropertyInfo property = Assert.Single(properties, candidate => candidate.Name == name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.False(property.CanWrite);
        MethodInfo getter = property.GetMethod!;
        Assert.NotNull(getter);
        Assert.True(getter.IsPublic);
        Assert.False(getter.IsStatic);
        Assert.False(getter.IsVirtual);
        Assert.False(getter.IsFinal);
        Assert.Same(getter, getter.GetBaseDefinition());
        NullabilityInfo nullability = Nullability.Create(property);
        Assert.Equal(expectedNullability, nullability.ReadState);
        Assert.Equal(NullabilityState.Unknown, nullability.WriteState);
    }

    static void AssertFactory(
        MethodInfo[] methods,
        string name,
        Type[] parameterTypes,
        string[] parameterNames,
        NullabilityState[] parameterNullability,
        bool[] optional,
        object?[] defaultValues)
    {
        MethodInfo method = Assert.Single(methods, candidate =>
            candidate.Name == name
            && candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));
        Assert.True(method.IsPublic);
        Assert.True(method.IsStatic);
        Assert.False(method.IsVirtual);
        Assert.False(method.IsFinal);
        Assert.False(method.IsGenericMethod);
        Assert.Same(method, method.GetBaseDefinition());
        Assert.Equal(typeof(StateMachineGraphNode), method.ReturnType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).WriteState);

        ParameterInfo[] parameters = method.GetParameters();
        Assert.Equal(parameterNames, parameters.Select(parameter => parameter.Name));
        Assert.Equal(optional, parameters.Select(parameter => parameter.IsOptional));
        Assert.Equal(optional, parameters.Select(parameter => parameter.HasDefaultValue));
        Assert.Equal(defaultValues, parameters.Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : null));
        for (var index = 0; index < parameters.Length; index++)
        {
            NullabilityInfo nullability = Nullability.Create(parameters[index]);
            Assert.Equal(parameterNullability[index], nullability.ReadState);
            Assert.Equal(parameterNullability[index], nullability.WriteState);
        }
    }

    public interface IMessageContract
    {
    }

    public abstract class AbstractMessage
    {
    }

    public delegate void MessageDelegate();

    public sealed record MessageRecord;

    public sealed class GenericMessage<T>
    {
    }

    public enum MessageKind
    {
        First,
    }

    public abstract class AbstractGraphException : Exception
    {
    }

    public sealed class ConcreteGraphException : AbstractGraphException
    {
    }

    public sealed class GenericGraphException<T> : Exception
    {
    }

    public sealed class PairGraphException<TFirst, TSecond> : Exception
    {
    }

    public sealed class ExceptionParameter<T> where T : Exception
    {
    }

    public static class StaticMessage
    {
    }

    public static class FirstContainer
    {
        public sealed class CollisionException : Exception
        {
        }
    }

    public static class SecondContainer
    {
        public sealed class CollisionException : Exception
        {
        }
    }
}
