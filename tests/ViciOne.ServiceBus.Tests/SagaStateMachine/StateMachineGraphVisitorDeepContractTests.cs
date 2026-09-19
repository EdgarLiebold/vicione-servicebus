using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineGraphVisitorDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-edge-enum-exact-surface-and-value-semantics")]
    public void EdgeAndKind_ExposeOnlyTheDocumentedSurfaceAndStableValues()
    {
        Type edgeType = typeof(StateMachineGraphEdge);
        Assert.True(edgeType.IsPublic);
        Assert.True(edgeType.IsSealed);
        Assert.False(edgeType.IsAbstract);
        Assert.Same(typeof(object), edgeType.BaseType);
        Assert.Equal([typeof(IEquatable<StateMachineGraphEdge>)], edgeType.GetInterfaces());
        Assert.Empty(edgeType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(edgeType.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.DoesNotContain(
            edgeType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic),
            type => type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem || type.IsNestedFamANDAssem);

        ConstructorInfo constructor = Assert.Single(edgeType.GetConstructors());
        var nullability = new NullabilityInfoContext();
        ParameterInfo[] parameters = constructor.GetParameters();
        Assert.Equal(
            [typeof(StateMachineGraphNode), typeof(StateMachineGraphNode), typeof(StateMachineGraphEdgeKind)],
            parameters.Select(parameter => parameter.ParameterType));
        Assert.Equal(["source", "target", "kind"], parameters.Select(parameter => parameter.Name));
        Assert.All(parameters, parameter =>
        {
            Assert.False(parameter.IsOptional);
            Assert.False(parameter.HasDefaultValue);
            NullabilityInfo parameterNullability = nullability.Create(parameter);
            Assert.Equal(NullabilityState.NotNull, parameterNullability.ReadState);
            Assert.Equal(NullabilityState.NotNull, parameterNullability.WriteState);
        });

        PropertyInfo[] properties = edgeType.GetProperties(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Equal(["Kind", "Source", "Target"], properties.Select(property => property.Name).Order(StringComparer.Ordinal));
        AssertEdgeProperty(properties, "Source", typeof(StateMachineGraphNode), nullability);
        AssertEdgeProperty(properties, "Target", typeof(StateMachineGraphNode), nullability);
        AssertEdgeProperty(properties, "Kind", typeof(StateMachineGraphEdgeKind), nullability);

        MethodInfo[] methods = edgeType
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();
        Assert.DoesNotContain(
            edgeType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly),
            method => method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal));
        Assert.Equal(
            [
                "Boolean Equals(Object)",
                "Boolean Equals(StateMachineGraphEdge)",
                "Int32 GetHashCode()",
                "String ToString()",
            ],
            methods.Select(DescribeMethod).Order(StringComparer.Ordinal));

        MethodInfo typedEquals = Assert.Single(methods, method =>
            method.Name == nameof(Equals) && method.GetParameters().SingleOrDefault()?.ParameterType == edgeType);
        MethodInfo objectEquals = Assert.Single(methods, method =>
            method.Name == nameof(Equals) && method.GetParameters().SingleOrDefault()?.ParameterType == typeof(object));
        MethodInfo getHashCode = Assert.Single(methods, method => method.Name == nameof(GetHashCode));
        MethodInfo toString = Assert.Single(methods, method => method.Name == nameof(ToString));
        Assert.All(methods, method => Assert.False(method.IsStatic));
        AssertMethodParameter(typedEquals, "other", hasDefaultValue: false);
        AssertMethodParameter(objectEquals, "obj", hasDefaultValue: false);
        Assert.Empty(getHashCode.GetParameters());
        Assert.Empty(toString.GetParameters());
        Assert.Same(typedEquals, typedEquals.GetBaseDefinition());
        Assert.Same(typeof(object), objectEquals.GetBaseDefinition().DeclaringType);
        Assert.Same(typeof(object), getHashCode.GetBaseDefinition().DeclaringType);
        Assert.Same(typeof(object), toString.GetBaseDefinition().DeclaringType);
        Assert.Equal(NullabilityState.Nullable, nullability.Create(Assert.Single(typedEquals.GetParameters())).ReadState);
        Assert.Equal(NullabilityState.Nullable, nullability.Create(Assert.Single(objectEquals.GetParameters())).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(typedEquals.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(objectEquals.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(getHashCode.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(toString.ReturnParameter).ReadState);

        Type kindType = typeof(StateMachineGraphEdgeKind);
        Assert.True(kindType.IsPublic);
        Assert.True(kindType.IsEnum);
        Assert.True(kindType.IsSealed);
        Assert.Same(typeof(Enum), kindType.BaseType);
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(kindType));
        Assert.Null(kindType.GetCustomAttribute<FlagsAttribute>());
        Assert.Equal(
            ["EventBinding", "StateTransition", "ExceptionHandler", "CompositeContribution", "StateInheritance"],
            Enum.GetNames<StateMachineGraphEdgeKind>());
        Assert.Equal([0, 1, 2, 3, 4], Enum.GetValues<StateMachineGraphEdgeKind>().Select(value => (int)value));

        StateMachineGraphNode source = StateMachineGraphNode.CreateState("Ready");
        StateMachineGraphNode target = StateMachineGraphNode.CreateEvent("Start");
        var first = new StateMachineGraphEdge(source, target, StateMachineGraphEdgeKind.EventBinding);
        var second = new StateMachineGraphEdge(source, target, StateMachineGraphEdgeKind.EventBinding);
        var differentSource = new StateMachineGraphEdge(
            StateMachineGraphNode.CreateState("Other"),
            target,
            StateMachineGraphEdgeKind.EventBinding);
        var differentTarget = new StateMachineGraphEdge(
            source,
            StateMachineGraphNode.CreateEvent("Other"),
            StateMachineGraphEdgeKind.EventBinding);
        var set = new HashSet<StateMachineGraphEdge> { first, second, differentSource, differentTarget };
        Assert.Equal(3, set.Count);
        Assert.True(first.Equals(second));
        Assert.True(second.Equals(first));
        Assert.True(first.Equals((object)second));
        Assert.False(first.Equals(differentSource));
        Assert.False(differentSource.Equals(first));
        Assert.False(first.Equals((object)differentSource));
        Assert.False(first.Equals(differentTarget));
        Assert.False(differentTarget.Equals(first));
        Assert.False(first.Equals((object)differentTarget));

        Type visitorDefinition = typeof(StateMachineGraph).Assembly.GetType(
            "ViciOne.ServiceBus.SagaStateMachine.StateMachineGraphVisitor`1",
            throwOnError: true)!;
        Assert.True(visitorDefinition.IsNotPublic);
        Assert.True(visitorDefinition.IsSealed);
        Assert.True(visitorDefinition.IsGenericTypeDefinition);
        Assert.Equal([typeof(IStateMachineVisitor)], visitorDefinition.GetInterfaces());
        Type visitorParameter = Assert.Single(visitorDefinition.GetGenericArguments());
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            visitorParameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISagaStateMachineInstance)], visitorParameter.GetGenericParameterConstraints());

        Type visitorType = visitorDefinition.MakeGenericType(typeof(GraphState));
        ConstructorInfo visitorConstructor = Assert.Single(visitorType.GetConstructors(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        Assert.True(visitorConstructor.IsAssembly);
        ParameterInfo visitorMachine = AssertMethodParameter(visitorConstructor, "machine", hasDefaultValue: false);
        Assert.Equal(typeof(IStateMachine<GraphState>), visitorMachine.ParameterType);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(visitorMachine).ReadState);
        PropertyInfo[] visitorProperties = visitorType.GetProperties(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.Equal(
            ["CurrentEvent", "CurrentState", "Graph"],
            visitorProperties.Select(property => property.Name).Order(StringComparer.Ordinal));
        PropertyInfo visitorGraph = Assert.Single(visitorProperties, property => property.Name == "Graph");
        Assert.Equal("Graph", visitorGraph.Name);
        Assert.Equal(typeof(StateMachineGraph), visitorGraph.PropertyType);
        Assert.False(visitorGraph.CanWrite);
        Assert.NotNull(visitorGraph.GetMethod);
        Assert.True(visitorGraph.GetMethod.IsAssembly);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(visitorGraph).ReadState);
        foreach (string contextPropertyName in new[] { "CurrentEvent", "CurrentState" })
        {
            PropertyInfo contextProperty = Assert.Single(
                visitorProperties,
                property => property.Name == contextPropertyName);
            Assert.Equal(typeof(StateMachineGraphNode), contextProperty.PropertyType);
            Assert.False(contextProperty.CanWrite);
            Assert.NotNull(contextProperty.GetMethod);
            Assert.True(contextProperty.GetMethod.IsPrivate);
            Assert.Equal(NullabilityState.NotNull, nullability.Create(contextProperty).ReadState);
        }

        InterfaceMapping visitorMap = visitorType.GetInterfaceMap(typeof(IStateMachineVisitor));
        MethodInfo[] visitorMethods = visitorType.GetMethods(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Equal(visitorMap.TargetMethods.OrderBy(DescribeContractMethod), visitorMethods.OrderBy(DescribeContractMethod));
        Assert.Equal(
            visitorMap.InterfaceMethods.Select(DescribeContractMethod).Order(StringComparer.Ordinal),
            visitorMap.TargetMethods.Select(DescribeContractMethod).Order(StringComparer.Ordinal));
        for (var index = 0; index < visitorMap.InterfaceMethods.Length; index++)
        {
            MethodInfo interfaceMethod = visitorMap.InterfaceMethods[index];
            MethodInfo targetMethod = visitorMap.TargetMethods[index];
            Assert.Equal(
                interfaceMethod.GetParameters().Select(parameter => parameter.Name),
                targetMethod.GetParameters().Select(parameter => parameter.Name));
            Assert.Equal(
                interfaceMethod.GetParameters().Select(parameter => parameter.HasDefaultValue),
                targetMethod.GetParameters().Select(parameter => parameter.HasDefaultValue));
            AssertNullabilityEquivalent(
                nullability.Create(interfaceMethod.ReturnParameter),
                nullability.Create(targetMethod.ReturnParameter));
            ParameterInfo[] interfaceParameters = interfaceMethod.GetParameters();
            ParameterInfo[] targetParameters = targetMethod.GetParameters();
            for (var parameterIndex = 0; parameterIndex < interfaceParameters.Length; parameterIndex++)
            {
                AssertNullabilityEquivalent(
                    nullability.Create(interfaceParameters[parameterIndex]),
                    nullability.Create(targetParameters[parameterIndex]));
            }
        }

        Assert.Equal(
            "source",
            Assert.Throws<ArgumentNullException>(() =>
                new StateMachineGraphEdge(null!, null!, (StateMachineGraphEdgeKind)int.MaxValue)).ParamName);
        Assert.Equal(
            "target",
            Assert.Throws<ArgumentNullException>(() =>
                new StateMachineGraphEdge(source, null!, (StateMachineGraphEdgeKind)int.MaxValue)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-ordered-state-local-disconnected-projection")]
    public void GetGraph_PreservesOrderingStateLocalBindingsAndDisconnectedElements()
    {
        var machine = new ProjectionMachine();

        StateMachineGraph first = machine.GetGraph();
        StateMachineGraph second = machine.GetGraph();

        Assert.Equal(first.Nodes.Select(DescribeNode), second.Nodes.Select(DescribeNode));
        Assert.Equal(first.Edges.Select(DescribeEdge), second.Edges.Select(DescribeEdge));
        Assert.DoesNotContain(first.Edges.GroupBy(edge => edge), group => group.Skip(1).Any());

        StateMachineGraphNode[] stateNodes = first.Nodes
            .Where(node => node.Kind == StateMachineGraphNodeKind.State)
            .ToArray();
        StateMachineGraphNode[] nonStateNodes = first.Nodes
            .Where(node => node.Kind != StateMachineGraphNodeKind.State)
            .ToArray();
        Assert.Equal(stateNodes.Concat(nonStateNodes), first.Nodes);
        Assert.Single(stateNodes, node => node.Name == machine.Orphan.Name);
        Assert.Single(nonStateNodes, node => node.Name == machine.Start.Name);
        Assert.Equal(3, nonStateNodes.Count(node => node.Name == machine.Shared.Name));
        Assert.Single(nonStateNodes, node => node.Name == machine.Unused.Name);

        StateMachineGraphEdge[] sharedBindings = first.Edges
            .Where(edge => edge.Kind == StateMachineGraphEdgeKind.EventBinding && edge.Target.Name == machine.Shared.Name)
            .OrderBy(edge => edge.Source.Name, StringComparer.Ordinal)
            .ToArray();
        Assert.Collection(
            sharedBindings,
            edge => Assert.Equal(machine.Child.Name, edge.Source.Name),
            edge => Assert.Equal(machine.Initial.Name, edge.Source.Name),
            edge => Assert.Equal(machine.Running.Name, edge.Source.Name));
        Assert.NotSame(sharedBindings[0].Target, sharedBindings[1].Target);
        Assert.NotSame(sharedBindings[1].Target, sharedBindings[2].Target);
        Assert.NotSame(sharedBindings[0].Target, sharedBindings[2].Target);

        StateMachineGraphEdge inheritance = Assert.Single(first.Edges, edge =>
            edge.Kind == StateMachineGraphEdgeKind.StateInheritance);
        Assert.Equal(machine.Child.Name, inheritance.Source.Name);
        Assert.Equal(machine.Running.Name, inheritance.Target.Name);

        foreach (StateMachineGraphEdge transition in first.Edges.Where(edge => edge.Kind == StateMachineGraphEdgeKind.StateTransition))
        {
            int bindingIndex = FindEdgeIndex(first, edge =>
                edge.Kind == StateMachineGraphEdgeKind.EventBinding
                && ReferenceEquals(edge.Target, transition.Source));
            int transitionIndex = FindEdgeIndex(first, edge => ReferenceEquals(edge, transition));
            Assert.InRange(bindingIndex, 0, transitionIndex - 1);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-repeat-graph-stability-and-deduplication")]
    public void ConcreteVisitor_RepeatedGraphAndVisitsRemainStableAndDeduplicated()
    {
        var machine = new ProjectionMachine();
        VisitorHarness harness = CreateVisitor(machine);
        machine.Accept(harness.Visitor);

        StateMachineGraph baseline = harness.GetGraph();
        StateMachineGraph repeated = harness.GetGraph();
        Assert.Equal(baseline.Nodes, repeated.Nodes);
        Assert.Equal(baseline.Edges, repeated.Edges);

        harness.Visitor.Visit(machine.Initial, _ =>
            harness.Visitor.Visit(machine.Start, _ => { }));
        harness.Visitor.Visit(machine.Initial, _ =>
            harness.Visitor.Visit(machine.Start, _ => { }));

        StateMachineGraph revisited = harness.GetGraph();
        Assert.Equal(baseline.Nodes, revisited.Nodes);
        Assert.Equal(baseline.Edges, revisited.Edges);
        Assert.DoesNotContain(revisited.Edges.GroupBy(edge => edge), group => group.Skip(1).Any());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-nested-exception-composite-transition-semantics")]
    public void ConcreteVisitor_ProjectsNestedExceptionCompositeAndTransitionEdgesInVisitOrder()
    {
        var machine = new NestedMachine();
        VisitorHarness harness = CreateVisitor(machine);
        var outer = new ExceptionActivity(typeof(InvalidOperationException));
        var inner = new ExceptionActivity(typeof(ArgumentException));
        IStateMachineActivity transition = CreateTransitionActivity(machine, machine.Running);
        var composite = new CompositeEventActivity<GraphState>(
            new IntCompositeEventStatusAccessor<GraphState>(
                typeof(GraphState).GetProperty(nameof(GraphState.CompositeStatus))!),
            1,
            default,
            machine.Ready,
            CompositeEventOptions.None);

        harness.Visitor.Visit(machine.Initial, _ =>
            harness.Visitor.Visit(machine.Trigger, _ =>
                harness.Visitor.Visit(outer, _ =>
                {
                    harness.Visitor.Visit(composite);
                    harness.Visitor.Visit(transition);
                    harness.Visitor.Visit(inner, _ => harness.Visitor.Visit(transition));
                })));

        StateMachineGraph graph = harness.GetGraph();
        Assert.Equal(
            [
                StateMachineGraphEdgeKind.EventBinding,
                StateMachineGraphEdgeKind.ExceptionHandler,
                StateMachineGraphEdgeKind.CompositeContribution,
                StateMachineGraphEdgeKind.StateTransition,
                StateMachineGraphEdgeKind.ExceptionHandler,
                StateMachineGraphEdgeKind.StateTransition,
            ],
            graph.Edges.Select(edge => edge.Kind));

        StateMachineGraphEdge[] handlers = graph.Edges
            .Where(edge => edge.Kind == StateMachineGraphEdgeKind.ExceptionHandler)
            .ToArray();
        Assert.Collection(
            handlers,
            edge =>
            {
                Assert.Equal(machine.Trigger.Name, edge.Source.Name);
                Assert.Equal(typeof(InvalidOperationException), edge.Target.ExceptionType);
            },
            edge =>
            {
                Assert.Equal(typeof(InvalidOperationException), edge.Source.ExceptionType);
                Assert.Equal(typeof(ArgumentException), edge.Target.ExceptionType);
            });

        StateMachineGraphEdge contribution = Assert.Single(graph.Edges, edge =>
            edge.Kind == StateMachineGraphEdgeKind.CompositeContribution);
        Assert.Equal(typeof(InvalidOperationException), contribution.Source.ExceptionType);
        Assert.Equal(machine.Ready.Name, contribution.Target.Name);
        Assert.True(contribution.Target.IsCompositeEvent);

        StateMachineGraphEdge[] transitions = graph.Edges
            .Where(edge => edge.Kind == StateMachineGraphEdgeKind.StateTransition)
            .ToArray();
        Assert.Collection(
            transitions,
            edge => Assert.Equal(typeof(InvalidOperationException), edge.Source.ExceptionType),
            edge => Assert.Equal(typeof(ArgumentException), edge.Source.ExceptionType));
        Assert.All(transitions, edge => Assert.Equal(machine.Running.Name, edge.Target.Name));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-visitor-null-and-illegal-visit-diagnostics")]
    public void ConcreteVisitor_RejectsNullAndIllegalVisitsWithExactDiagnostics()
    {
        var machine = new NestedMachine();
        Type visitorType = GetVisitorType();
        TargetInvocationException wrapped = Assert.Throws<TargetInvocationException>(() =>
            Activator.CreateInstance(
                visitorType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: [null],
                culture: null));
        Assert.Equal("machine", Assert.IsType<ArgumentNullException>(wrapped.InnerException).ParamName);

        IStateMachineVisitor visitor = CreateVisitor(machine).Visitor;
        AssertParam("state", () => visitor.Visit((IState)null!, _ => { }));
        AssertParam("next", () => visitor.Visit(machine.Initial, null!));
        AssertParam("event", () => visitor.Visit((IEvent)null!, _ => { }));
        AssertParam("next", () => visitor.Visit((IEvent)machine.Trigger, null!));
        AssertParam("event", () => visitor.Visit<Message>((IEvent<Message>)null!, _ => { }));
        AssertParam("next", () => visitor.Visit<Message>(machine.Message, null!));
        AssertParam("activity", () => visitor.Visit((IStateMachineActivity)null!));
        AssertParam("activity", () => visitor.Visit((IStateMachineActivity)null!, _ => { }));
        AssertParam("next", () => visitor.Visit(new PassiveActivity(), null!));
        AssertParam("activity", () => visitor.Visit((IStateMachineExceptionActivity)null!, _ => { }));
        AssertParam("next", () => visitor.Visit(
            new ExceptionActivity(typeof(Exception)),
            (Action<IStateMachineExceptionActivity>)null!));
        AssertParam("behavior", () => visitor.Visit<GraphState>(null!));
        AssertParam("behavior", () => visitor.Visit<GraphState>((IBehavior<GraphState>)null!, _ => { }));
        AssertParam("next", () => visitor.Visit(new EmptyBehavior<GraphState>(), null!));
        AssertParam("behavior", () => visitor.Visit<GraphState, Message>(null!));
        AssertParam("behavior", () => visitor.Visit<GraphState, Message>(null!, _ => { }));
        AssertParam("next", () => visitor.Visit(new EmptyBehavior<GraphState, Message>(), null!));

        AssertParam("state", () => visitor.Visit((IState)null!, null!));
        AssertParam("event", () => visitor.Visit((IEvent)null!, null!));
        AssertParam("event", () => visitor.Visit<Message>((IEvent<Message>)null!, null!));
        AssertParam("activity", () => visitor.Visit((IStateMachineActivity)null!, null!));
        AssertParam("activity", () => visitor.Visit(
            (IStateMachineExceptionActivity)null!,
            (Action<IStateMachineExceptionActivity>)null!));
        AssertParam("behavior", () => visitor.Visit<GraphState>((IBehavior<GraphState>)null!, null!));
        AssertParam("behavior", () => visitor.Visit<GraphState, Message>(null!, null!));

        var messageBehavior = new EmptyBehavior<GraphState, Message>();
        visitor.Visit<GraphState, Message>(messageBehavior);
        var behaviorCalls = 0;
        visitor.Visit<GraphState, Message>(messageBehavior, seen =>
        {
            Assert.Same(messageBehavior, seen);
            behaviorCalls++;
        });
        Assert.Equal(1, behaviorCalls);

        InvalidOperationException missingState = Assert.Throws<InvalidOperationException>(() =>
            visitor.Visit(machine.Trigger, _ => { }));
        Assert.Equal("A state-machine state must be visited before its events.", missingState.Message);

        IStateMachineActivity transition = CreateTransitionActivity(machine, machine.Running);
        visitor.Visit(machine.Initial, _ =>
        {
            InvalidOperationException missingEvent = Assert.Throws<InvalidOperationException>(() => visitor.Visit(transition));
            Assert.Equal("A state-machine event must be visited before its activities.", missingEvent.Message);
            InvalidOperationException missingExceptionParent = Assert.Throws<InvalidOperationException>(() =>
                visitor.Visit(new ExceptionActivity(typeof(Exception)), _ => { }));
            Assert.Equal("A state-machine event must be visited before its activities.", missingExceptionParent.Message);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-visitor-exception-safe-context-restoration")]
    public void ConcreteVisitor_RestoresEveryContextAndPreservesContinuationExceptionIdentity()
    {
        var machine = new NestedMachine();
        VisitorHarness harness = CreateVisitor(machine);
        var marker = new MarkerException();

        MarkerException stateFailure = Assert.Throws<MarkerException>(() =>
            harness.Visitor.Visit(machine.Initial, _ =>
            {
                harness.Visitor.Visit(machine.Trigger, _ => { });
                throw marker;
            }));
        Assert.Same(marker, stateFailure);
        InvalidOperationException afterState = Assert.Throws<InvalidOperationException>(() =>
            harness.Visitor.Visit(machine.Trigger, _ => { }));
        Assert.Equal("A state-machine state must be visited before its events.", afterState.Message);

        IStateMachineActivity transition = CreateTransitionActivity(machine, machine.Running);
        InvalidOperationException afterStateEvent = Assert.Throws<InvalidOperationException>(() =>
            harness.Visitor.Visit(transition));
        Assert.Equal("A state-machine event must be visited before its activities.", afterStateEvent.Message);
        harness.Visitor.Visit(machine.Initial, _ =>
        {
            MarkerException eventFailure = Assert.Throws<MarkerException>(() =>
                harness.Visitor.Visit(machine.Trigger, _ => throw marker));
            Assert.Same(marker, eventFailure);
            InvalidOperationException afterEvent = Assert.Throws<InvalidOperationException>(() =>
                harness.Visitor.Visit(transition));
            Assert.Equal("A state-machine event must be visited before its activities.", afterEvent.Message);

            harness.Visitor.Visit(machine.Trigger, _ => { });
            MarkerException typedEventFailure = Assert.Throws<MarkerException>(() =>
                harness.Visitor.Visit<Message>(machine.Message, _ => throw marker));
            Assert.Same(marker, typedEventFailure);
            harness.Visitor.Visit(transition);

            harness.Visitor.Visit(machine.Trigger, _ =>
            {
                MarkerException exceptionFailure = Assert.Throws<MarkerException>(() =>
                    harness.Visitor.Visit(
                        new ExceptionActivity(typeof(InvalidOperationException)),
                        _ => throw marker));
                Assert.Same(marker, exceptionFailure);
                harness.Visitor.Visit(transition);
            });
            harness.Visitor.Visit(machine.Other, _ => { });
            harness.Visitor.Visit(transition);
        });

        StateMachineGraph graph = harness.GetGraph();
        StateMachineGraphEdge[] transitionEdges = graph.Edges
            .Where(edge => edge.Kind == StateMachineGraphEdgeKind.StateTransition)
            .ToArray();
        Assert.Equal([machine.Trigger.Name, machine.Other.Name], transitionEdges.Select(edge => edge.Source.Name));
        Assert.All(transitionEdges, edge => Assert.Equal(StateMachineGraphNodeKind.Event, edge.Source.Kind));
        Assert.All(transitionEdges, edge => Assert.Equal(machine.Running.Name, edge.Target.Name));
        Assert.Single(graph.Edges, edge => edge.Kind == StateMachineGraphEdgeKind.ExceptionHandler);

        VisitorHarness nestedHarness = CreateVisitor(machine);
        nestedHarness.Visitor.Visit(machine.Initial, _ =>
        {
            nestedHarness.Visitor.Visit(machine.Trigger, _ =>
            {
                nestedHarness.Visitor.Visit<Message>(machine.Message, _ => { });
                nestedHarness.Visitor.Visit(transition);
            });
            nestedHarness.Visitor.Visit<Message>(machine.Message, _ =>
            {
                nestedHarness.Visitor.Visit(machine.Other, _ => { });
                nestedHarness.Visitor.Visit(transition);
            });
            nestedHarness.Visitor.Visit(machine.Trigger, _ => { });
            nestedHarness.Visitor.Visit(
                new ExceptionActivity(typeof(InvalidOperationException)),
                _ =>
                {
                    nestedHarness.Visitor.Visit<Message>(machine.Message, _ => { });
                    nestedHarness.Visitor.Visit(transition);
                });
            nestedHarness.Visitor.Visit(machine.Other, _ => { });
            nestedHarness.Visitor.Visit(transition);
        });
        StateMachineGraph nestedGraph = nestedHarness.GetGraph();
        Assert.Equal(
            [machine.Trigger.Name, machine.Message.Name, nameof(InvalidOperationException), machine.Other.Name],
            nestedGraph.Edges
                .Where(edge => edge.Kind == StateMachineGraphEdgeKind.StateTransition)
                .Select(edge => edge.Source.Name));
    }

    static string DescribeMethod(MethodInfo method) =>
        $"{method.ReturnType.Name} {method.Name}({string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.Name))})";

    static string DescribeContractMethod(MethodInfo method) =>
        $"{method.Name}`{method.GetGenericArguments().Length}({string.Join(",", method.GetParameters().Select(parameter => DescribeContractType(parameter.ParameterType)))})";

    static string DescribeContractType(Type type)
    {
        if (type.IsGenericParameter)
            return $"!{type.GenericParameterPosition}";
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        return $"{type.GetGenericTypeDefinition().FullName}<{string.Join(",", type.GetGenericArguments().Select(DescribeContractType))}>";
    }

    static string DescribeNode(StateMachineGraphNode node) =>
        $"{node.Kind}|{node.Name}|{node.MessageType?.FullName}|{node.ExceptionType?.FullName}|{node.IsCompositeEvent}";

    static string DescribeEdge(StateMachineGraphEdge edge) =>
        $"{edge.Kind}|{DescribeNode(edge.Source)}|{DescribeNode(edge.Target)}";

    static int FindEdgeIndex(StateMachineGraph graph, Func<StateMachineGraphEdge, bool> predicate)
    {
        for (var index = 0; index < graph.Edges.Count; index++)
        {
            if (predicate(graph.Edges[index]))
                return index;
        }

        throw new Xunit.Sdk.XunitException("The expected graph edge was not found.");
    }

    static void AssertEdgeProperty(
        PropertyInfo[] properties,
        string name,
        Type propertyType,
        NullabilityInfoContext nullability)
    {
        PropertyInfo property = Assert.Single(properties, candidate => candidate.Name == name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.False(property.CanWrite);
        MethodInfo getter = property.GetMethod!;
        Assert.NotNull(getter);
        Assert.True(getter.IsPublic);
        Assert.False(getter.IsStatic);
        Assert.False(getter.IsVirtual);
        Assert.Same(getter, getter.GetBaseDefinition());
        NullabilityInfo propertyNullability = nullability.Create(property);
        Assert.Equal(NullabilityState.NotNull, propertyNullability.ReadState);
        Assert.Equal(NullabilityState.Unknown, propertyNullability.WriteState);
    }

    static ParameterInfo AssertMethodParameter(MethodBase method, string name, bool hasDefaultValue)
    {
        ParameterInfo parameter = Assert.Single(method.GetParameters());
        Assert.Equal(name, parameter.Name);
        Assert.Equal(hasDefaultValue, parameter.HasDefaultValue);
        Assert.Equal(hasDefaultValue, parameter.IsOptional);
        return parameter;
    }

    static void AssertNullabilityEquivalent(NullabilityInfo expected, NullabilityInfo actual)
    {
        Assert.Equal(DescribeContractType(expected.Type), DescribeContractType(actual.Type));
        Assert.Equal(expected.ReadState, actual.ReadState);
        Assert.Equal(expected.WriteState, actual.WriteState);
        Assert.Equal(expected.GenericTypeArguments.Length, actual.GenericTypeArguments.Length);
        for (var index = 0; index < expected.GenericTypeArguments.Length; index++)
            AssertNullabilityEquivalent(expected.GenericTypeArguments[index], actual.GenericTypeArguments[index]);
    }

    static void AssertParam(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    static Type GetVisitorType() => typeof(StateMachineGraph).Assembly
        .GetType("ViciOne.ServiceBus.SagaStateMachine.StateMachineGraphVisitor`1", throwOnError: true)!
        .MakeGenericType(typeof(GraphState));

    static VisitorHarness CreateVisitor(IStateMachine<GraphState> machine)
    {
        object instance = Activator.CreateInstance(
            GetVisitorType(),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [machine],
            culture: null)
            ?? throw new InvalidOperationException("The graph visitor could not be created.");
        var visitor = Assert.IsAssignableFrom<IStateMachineVisitor>(instance);
        PropertyInfo graph = instance.GetType().GetProperty(
            "Graph",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The graph visitor's Graph property is missing.");
        return new VisitorHarness(visitor, () => Assert.IsType<StateMachineGraph>(graph.GetValue(instance)));
    }

    static IStateMachineActivity CreateTransitionActivity(NestedMachine machine, IState target)
    {
        Type type = typeof(StateMachineGraph).Assembly
            .GetType("ViciOne.ServiceBus.SagaStateMachine.TransitionActivity`1", throwOnError: true)!
            .MakeGenericType(typeof(GraphState));
        return Assert.IsAssignableFrom<IStateMachineActivity>(Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [machine.GetState(target.Name), machine.Accessor],
            culture: null));
    }

    sealed record VisitorHarness(IStateMachineVisitor Visitor, Func<StateMachineGraph> GetGraph);

    sealed class GraphState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public IState CurrentState { get; set; } = null!;

        public int CompositeStatus { get; set; }
    }

    sealed record Start;

    sealed record Message;

    sealed class ProjectionMachine : ViciOneServiceBusStateMachine<GraphState>
    {
        public ProjectionMachine()
        {
            InstanceState(instance => instance.CurrentState);
            SubState(() => Child, Running);
            Initially(
                When(Start).TransitionTo(Running),
                When(Shared).TransitionTo(Running));
            During(Running, When(Shared).TransitionTo(Child));
            During(Child, When(Shared).TransitionTo(Running));
        }

        public IState Running { get; private set; } = null!;

        public IState Child { get; private set; } = null!;

        public IState Orphan { get; private set; } = null!;

        public IEvent<Start> Start { get; private set; } = null!;

        public IEvent Shared { get; private set; } = null!;

        public IEvent Unused { get; private set; } = null!;
    }

    sealed class NestedMachine : ViciOneServiceBusStateMachine<GraphState>
    {
        public NestedMachine()
        {
            InstanceState(instance => instance.CurrentState);
            CompositeEvent(() => Ready, instance => instance.CompositeStatus, Trigger, Other);
            Initially(When(Trigger).TransitionTo(Running));
        }

        public IState Running { get; private set; } = null!;

        public IEvent Trigger { get; private set; } = null!;

        public IEvent Other { get; private set; } = null!;

        public IEvent Ready { get; private set; } = null!;

        public IEvent<Message> Message { get; private set; } = null!;
    }

    sealed class ExceptionActivity(Type exceptionType) : IStateMachineExceptionActivity
    {
        public Type ExceptionType { get; } = exceptionType;

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this, _ => { });

        public void Probe(ProbeContext context)
        {
        }
    }

    sealed class PassiveActivity : IStateMachineActivity
    {
        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    sealed class MarkerException : Exception;
}
