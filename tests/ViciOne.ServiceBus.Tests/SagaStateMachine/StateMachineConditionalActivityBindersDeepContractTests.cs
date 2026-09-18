using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineConditionalActivityBindersDeepContractTests
{
    static readonly BinderKind[] BinderKinds = Enum.GetValues<BinderKind>();

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-exact-public-surface-nullability")]
    public void PublicSurface_ExposesTheExactFourConditionalBinderContractsAndNullability()
    {
        Type[] definitions =
        [
            typeof(ConditionalActivityBinder<>),
            typeof(ConditionalActivityBinder<,>),
            typeof(ConditionalExceptionActivityBinder<,>),
            typeof(ConditionalExceptionActivityBinder<,,>),
        ];
        Type[] closedTypes =
        [
            typeof(ConditionalActivityBinder<TestSaga>),
            typeof(ConditionalActivityBinder<TestSaga, Message>),
            typeof(ConditionalExceptionActivityBinder<TestSaga, MarkerException>),
            typeof(ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>),
        ];
        var nullability = new NullabilityInfoContext();

        for (var index = 0; index < definitions.Length; index++)
        {
            Type definition = definitions[index];
            Type closedType = closedTypes[index];
            Assert.True(definition.IsPublic);
            Assert.True(definition.IsClass);
            Assert.False(definition.IsAbstract);
            Assert.False(definition.IsSealed);
            Assert.Equal([typeof(IActivityBinder<>).MakeGenericType(definition.GetGenericArguments()[0])], definition.GetInterfaces());
            AssertSagaParameter(definition.GetGenericArguments()[0]);

            Type[] genericArguments = definition.GetGenericArguments();
            if (definition == typeof(ConditionalActivityBinder<,>))
                AssertReferenceTypeParameter(genericArguments[1]);
            else if (definition == typeof(ConditionalExceptionActivityBinder<,>))
                AssertExceptionParameter(genericArguments[1]);
            else if (definition == typeof(ConditionalExceptionActivityBinder<,,>))
            {
                AssertReferenceTypeParameter(genericArguments[1]);
                AssertExceptionParameter(genericArguments[2]);
            }

            ConstructorInfo[] constructors = closedType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(2, constructors.Length);
            Assert.All(constructors, constructor =>
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                Assert.Equal(["event", "condition", "thenActivities", "elseActivities"], parameters.Select(parameter => parameter.Name));
                Assert.Equal(typeof(IEvent), parameters[0].ParameterType);
                Assert.Equal(typeof(IEventActivities<TestSaga>), parameters[2].ParameterType);
                Assert.Equal(typeof(IEventActivities<TestSaga>), parameters[3].ParameterType);
                Assert.All(parameters, parameter =>
                {
                    Assert.False(parameter.IsOptional);
                    Assert.False(parameter.HasDefaultValue);
                    NullabilityInfo parameterNullability = nullability.Create(parameter);
                    Assert.Equal(NullabilityState.NotNull, parameterNullability.ReadState);
                    Assert.Equal(NullabilityState.NotNull, parameterNullability.WriteState);
                });
            });
            Assert.Equal(typeof(bool), Assert.Single(constructors, constructor =>
                    constructor.GetParameters()[1].ParameterType.GetMethod("Invoke")!.ReturnType == typeof(bool))
                .GetParameters()[1].ParameterType.GetMethod("Invoke")!.ReturnType);
            Assert.Equal(typeof(Task<bool>), Assert.Single(constructors, constructor =>
                    constructor.GetParameters()[1].ParameterType.GetMethod("Invoke")!.ReturnType == typeof(Task<bool>))
                .GetParameters()[1].ParameterType.GetMethod("Invoke")!.ReturnType);

            PropertyInfo property = Assert.Single(closedType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            Assert.Equal(nameof(IActivityBinder<TestSaga>.Event), property.Name);
            Assert.Equal(typeof(IEvent), property.PropertyType);
            Assert.True(property.CanRead);
            Assert.False(property.CanWrite);
            Assert.Equal(NullabilityState.NotNull, nullability.Create(property).ReadState);

            MethodInfo[] methods = closedType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .ToArray();
            Assert.Equal(3, methods.Length);
            Assert.Single(methods, method => method.Name == nameof(IActivityBinder<TestSaga>.IsStateTransitionEvent)
                && method.ReturnType == typeof(bool)
                && method.GetParameters().Single().ParameterType == typeof(IState));
            Assert.Single(methods, method => method.Name == nameof(IActivityBinder<TestSaga>.Bind)
                && method.GetParameters().Single().ParameterType == typeof(IState<TestSaga>));
            Assert.Single(methods, method => method.Name == nameof(IActivityBinder<TestSaga>.Bind)
                && method.GetParameters().Single().ParameterType == typeof(IBehaviorBuilder<TestSaga>));
            Assert.All(methods.SelectMany(method => method.GetParameters()), parameter =>
                Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));
            Assert.DoesNotContain(methods, method => method.Name.EndsWith("Async", StringComparison.Ordinal));
            Assert.Empty(closedType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
            Assert.Empty(closedType.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-required-input-boundaries")]
    public void ConstructorsAndMethods_RejectEveryMissingRequiredInputBeforeCollaboratorEffects()
    {
        Type[] binderTypes =
        [
            typeof(ConditionalActivityBinder<TestSaga>),
            typeof(ConditionalActivityBinder<TestSaga, Message>),
            typeof(ConditionalExceptionActivityBinder<TestSaga, MarkerException>),
            typeof(ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>),
        ];

        foreach (Type binderType in binderTypes)
        {
            foreach (ConstructorInfo constructor in binderType.GetConstructors())
            {
                object?[] validArguments = CreateConstructorArguments(constructor);
                ParameterInfo[] parameters = constructor.GetParameters();
                for (var index = 0; index < parameters.Length; index++)
                {
                    object?[] arguments = (object?[])validArguments.Clone();
                    arguments[index] = null;
                    TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(arguments));
                    var failure = Assert.IsType<ArgumentNullException>(invocation.InnerException);
                    Assert.Equal(parameters[index].Name, failure.ParamName);
                }
            }
        }

        foreach (BinderKind kind in BinderKinds)
        {
            var branches = new BranchActivities("branch", []);
            IActivityBinder<TestSaga> binder = CreateBinder(kind, new TriggerEvent($"event-{kind}"), branches, branches);

            AssertParam("state", () => binder.IsStateTransitionEvent(null!));
            AssertParam("state", () => binder.Bind((IState<TestSaga>)null!));
            AssertParam("builder", () => binder.Bind((IBehaviorBuilder<TestSaga>)null!));
            Assert.Equal(0, branches.GetCalls);
            Assert.Equal(0, branches.EnumerationCalls);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-transition-classification")]
    public void IsStateTransitionEvent_MatchesExactlyTheFourLifecycleEventsAndPreservesEventIdentity()
    {
        var enter = new TriggerEvent("enter");
        var beforeEnter = new MessageEvent<IState>("before-enter");
        var afterLeave = new MessageEvent<IState>("after-leave");
        var leave = new TriggerEvent("leave");
        IState<TestSaga> state = CreateState(enter, beforeEnter, afterLeave, leave, out _);
        var unrelated = new TriggerEvent("unrelated");

        foreach (BinderKind kind in BinderKinds)
        {
            foreach (IEvent lifecycleEvent in new IEvent[] { enter, beforeEnter, afterLeave, leave })
            {
                IActivityBinder<TestSaga> binder = CreateBinder(
                    kind,
                    lifecycleEvent,
                    new BranchActivities("then", []),
                    new BranchActivities("else", []));

                Assert.Same(lifecycleEvent, binder.Event);
                Assert.True(binder.IsStateTransitionEvent(state));
            }

            IActivityBinder<TestSaga> unrelatedBinder = CreateBinder(
                kind,
                unrelated,
                new BranchActivities("then", []),
                new BranchActivities("else", []));
            Assert.False(unrelatedBinder.IsStateTransitionEvent(state));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-sync-wrapper-context-result-failure")]
    public async Task SynchronousConditions_PreserveContextResultAndSynchronousFailureAcrossAllFourBinderShapesAsync()
    {
        IBehaviorContext<TestSaga> normalContext = CreateStrictProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> messageContext = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> exceptionContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> messageExceptionContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();

        IBehaviorContext<TestSaga>? observedNormal = null;
        var normal = new ConditionalActivityBinder<TestSaga>(
            new TriggerEvent("normal"),
            context =>
            {
                observedNormal = context;
                return true;
            },
            EmptyBranch("then"),
            EmptyBranch("else"));
        StateMachineAsyncCondition<TestSaga> normalWrapper = GetCondition<StateMachineAsyncCondition<TestSaga>>(Bind(normal));
        Task<bool> normalResult = normalWrapper(normalContext);
        Assert.True(normalResult.IsCompletedSuccessfully);
        Assert.True(await normalResult);
        Assert.Same(normalContext, observedNormal);

        IBehaviorContext<TestSaga, Message>? observedMessage = null;
        var message = new ConditionalActivityBinder<TestSaga, Message>(
            new TriggerEvent("message"),
            context =>
            {
                observedMessage = context;
                return false;
            },
            EmptyBranch("then"),
            EmptyBranch("else"));
        StateMachineAsyncCondition<TestSaga, Message> messageWrapper =
            GetCondition<StateMachineAsyncCondition<TestSaga, Message>>(Bind(message));
        Task<bool> messageResult = messageWrapper(messageContext);
        Assert.True(messageResult.IsCompletedSuccessfully);
        Assert.False(await messageResult);
        Assert.Same(messageContext, observedMessage);

        IBehaviorExceptionContext<TestSaga, MarkerException>? observedException = null;
        var exception = new ConditionalExceptionActivityBinder<TestSaga, MarkerException>(
            new TriggerEvent("exception"),
            context =>
            {
                observedException = context;
                return true;
            },
            EmptyBranch("then"),
            EmptyBranch("else"));
        StateMachineAsyncExceptionCondition<TestSaga, MarkerException> exceptionWrapper =
            GetCondition<StateMachineAsyncExceptionCondition<TestSaga, MarkerException>>(Bind(exception));
        Task<bool> exceptionResult = exceptionWrapper(exceptionContext);
        Assert.True(exceptionResult.IsCompletedSuccessfully);
        Assert.True(await exceptionResult);
        Assert.Same(exceptionContext, observedException);

        IBehaviorExceptionContext<TestSaga, Message, MarkerException>? observedMessageException = null;
        var messageException = new ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>(
            new TriggerEvent("message-exception"),
            context =>
            {
                observedMessageException = context;
                return false;
            },
            EmptyBranch("then"),
            EmptyBranch("else"));
        StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException> messageExceptionWrapper =
            GetCondition<StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException>>(Bind(messageException));
        Task<bool> messageExceptionResult = messageExceptionWrapper(messageExceptionContext);
        Assert.True(messageExceptionResult.IsCompletedSuccessfully);
        Assert.False(await messageExceptionResult);
        Assert.Same(messageExceptionContext, observedMessageException);

        var expected = new MarkerException("sync condition failed");
        AssertSynchronousFailure(
            new ConditionalActivityBinder<TestSaga>(new TriggerEvent("normal-failure"),
                (StateMachineCondition<TestSaga>)(_ => throw expected),
                EmptyBranch("then"), EmptyBranch("else")),
            normalContext,
            expected);
        AssertSynchronousFailure(
            new ConditionalActivityBinder<TestSaga, Message>(new TriggerEvent("message-failure"),
                (StateMachineCondition<TestSaga, Message>)(_ => throw expected),
                EmptyBranch("then"), EmptyBranch("else")),
            messageContext,
            expected);
        AssertSynchronousFailure(
            new ConditionalExceptionActivityBinder<TestSaga, MarkerException>(
                new TriggerEvent("exception-failure"),
                (StateMachineExceptionCondition<TestSaga, MarkerException>)(_ => throw expected),
                EmptyBranch("then"), EmptyBranch("else")),
            exceptionContext,
            expected);
        AssertSynchronousFailure(
            new ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>(
                new TriggerEvent("message-exception-failure"),
                (StateMachineExceptionCondition<TestSaga, Message, MarkerException>)(_ => throw expected),
                EmptyBranch("then"), EmptyBranch("else")),
            messageExceptionContext,
            expected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-async-task-failure-cancellation-identity")]
    public async Task AsynchronousConditions_PreserveDelegateContextAndExactResultFailureAndCancellationTasksAsync()
    {
        IBehaviorContext<TestSaga> normalContext = CreateStrictProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> messageContext = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> exceptionContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> messageExceptionContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        var failure = new MarkerException("async condition failed");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task<bool>[] expectedTasks =
        [
            Task.FromResult(true),
            Task.FromException<bool>(failure),
            Task.FromCanceled<bool>(cancellation.Token),
        ];

        foreach (Task<bool> expectedTask in expectedTasks)
        {
            IBehaviorContext<TestSaga>? observedNormal = null;
            StateMachineAsyncCondition<TestSaga> normalCondition = context =>
            {
                observedNormal = context;
                return expectedTask;
            };
            var normal = new ConditionalActivityBinder<TestSaga>(new TriggerEvent("normal"), normalCondition,
                EmptyBranch("then"), EmptyBranch("else"));
            StateMachineAsyncCondition<TestSaga> capturedNormal = GetCondition<StateMachineAsyncCondition<TestSaga>>(Bind(normal));
            Assert.Same(normalCondition, capturedNormal);
            Assert.Same(expectedTask, capturedNormal(normalContext));
            Assert.Same(normalContext, observedNormal);

            IBehaviorContext<TestSaga, Message>? observedMessage = null;
            StateMachineAsyncCondition<TestSaga, Message> messageCondition = context =>
            {
                observedMessage = context;
                return expectedTask;
            };
            var message = new ConditionalActivityBinder<TestSaga, Message>(new TriggerEvent("message"), messageCondition,
                EmptyBranch("then"), EmptyBranch("else"));
            StateMachineAsyncCondition<TestSaga, Message> capturedMessage =
                GetCondition<StateMachineAsyncCondition<TestSaga, Message>>(Bind(message));
            Assert.Same(messageCondition, capturedMessage);
            Assert.Same(expectedTask, capturedMessage(messageContext));
            Assert.Same(messageContext, observedMessage);

            IBehaviorExceptionContext<TestSaga, MarkerException>? observedException = null;
            StateMachineAsyncExceptionCondition<TestSaga, MarkerException> exceptionCondition = context =>
            {
                observedException = context;
                return expectedTask;
            };
            var exception = new ConditionalExceptionActivityBinder<TestSaga, MarkerException>(
                new TriggerEvent("exception"), exceptionCondition, EmptyBranch("then"), EmptyBranch("else"));
            StateMachineAsyncExceptionCondition<TestSaga, MarkerException> capturedException =
                GetCondition<StateMachineAsyncExceptionCondition<TestSaga, MarkerException>>(Bind(exception));
            Assert.Same(exceptionCondition, capturedException);
            Assert.Same(expectedTask, capturedException(exceptionContext));
            Assert.Same(exceptionContext, observedException);

            IBehaviorExceptionContext<TestSaga, Message, MarkerException>? observedMessageException = null;
            StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException> messageExceptionCondition = context =>
            {
                observedMessageException = context;
                return expectedTask;
            };
            var messageException = new ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>(
                new TriggerEvent("message-exception"), messageExceptionCondition, EmptyBranch("then"), EmptyBranch("else"));
            StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException> capturedMessageException =
                GetCondition<StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException>>(Bind(messageException));
            Assert.Same(messageExceptionCondition, capturedMessageException);
            Assert.Same(expectedTask, capturedMessageException(messageExceptionContext));
            Assert.Same(messageExceptionContext, observedMessageException);
        }

        Assert.Same(failure, await Assert.ThrowsAsync<MarkerException>(() => expectedTasks[1]));
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => expectedTasks[2]);
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
    }

    [Theory]
    [InlineData(BinderKind.Normal, false)]
    [InlineData(BinderKind.Normal, true)]
    [InlineData(BinderKind.Message, false)]
    [InlineData(BinderKind.Message, true)]
    [InlineData(BinderKind.Exception, false)]
    [InlineData(BinderKind.Exception, true)]
    [InlineData(BinderKind.MessageException, false)]
    [InlineData(BinderKind.MessageException, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-bind-order-and-failure-atomicity")]
    public void Bind_MaterializesThenBeforeElseOnceAndPublishesOnlyAfterBothBranches(
        BinderKind kind,
        bool bindToState)
    {
        var log = new List<string>();
        var then = new BranchActivities("then", [new RecordingBinder("then-activity", log)], log);
        var @else = new BranchActivities("else", [new RecordingBinder("else-activity", log)], log);
        IActivityBinder<TestSaga> binder = CreateBinder(kind, new TriggerEvent("event"), then, @else);

        if (bindToState)
        {
            IState<TestSaga> state = CreateState(
                new TriggerEvent("enter"),
                new MessageEvent<IState>("before-enter"),
                new MessageEvent<IState>("after-leave"),
                new TriggerEvent("leave"),
                out RecordingStateProxy stateProxy,
                log);
            binder.Bind(state);
            Assert.Single(stateProxy.Bindings);
            Assert.Same(binder.Event, stateProxy.Bindings[0].Event);
        }
        else
        {
            var builder = new CapturingBuilder(log);
            binder.Bind(builder);
            Assert.Single(builder.Activities);
        }

        Assert.Equal(
            [
                "then:get", "then:enumerate", "then-activity:bind", "then:end",
                "else:get", "else:enumerate", "else-activity:bind", "else:end",
                "target:add",
            ],
            log);
        Assert.Equal(1, then.GetCalls);
        Assert.Equal(1, then.EnumerationCalls);
        Assert.Equal(1, @else.GetCalls);
        Assert.Equal(1, @else.EnumerationCalls);

        var expected = new MarkerException("then materialization failed");
        log.Clear();
        var failingThen = new BranchActivities("then", [new RecordingBinder("then-failure", log, expected)], log);
        var untouchedElse = new BranchActivities("else", [new RecordingBinder("else-activity", log)], log);
        IActivityBinder<TestSaga> failingBinder = CreateBinder(kind, new TriggerEvent("failure"), failingThen, untouchedElse);
        var untouchedTarget = new CapturingBuilder(log);

        MarkerException actual = Assert.Throws<MarkerException>(() => failingBinder.Bind(untouchedTarget));

        Assert.Same(expected, actual);
        Assert.Equal(["then:get", "then:enumerate", "then-failure:bind"], log);
        Assert.Equal(0, untouchedElse.GetCalls);
        Assert.Empty(untouchedTarget.Activities);

        var elseFailure = new MarkerException("else materialization failed");
        log.Clear();
        var successfulThen = new BranchActivities("then", [new RecordingBinder("then-activity", log)], log);
        var failingElse = new BranchActivities("else", [new RecordingBinder("else-failure", log, elseFailure)], log);
        IActivityBinder<TestSaga> failingElseBinder = CreateBinder(
            kind,
            new TriggerEvent("else-failure"),
            successfulThen,
            failingElse);
        var failedElseBuilder = new CapturingBuilder(log);
        RecordingStateProxy? failedElseState = null;
        Action bindWithFailingElse;
        if (bindToState)
        {
            IState<TestSaga> state = CreateState(
                new TriggerEvent("enter"),
                new MessageEvent<IState>("before-enter"),
                new MessageEvent<IState>("after-leave"),
                new TriggerEvent("leave"),
                out RecordingStateProxy stateProxy,
                log);
            failedElseState = stateProxy;
            bindWithFailingElse = () => failingElseBinder.Bind(state);
        }
        else
            bindWithFailingElse = () => failingElseBinder.Bind(failedElseBuilder);

        MarkerException actualElseFailure = Assert.Throws<MarkerException>(bindWithFailingElse);

        Assert.Same(elseFailure, actualElseFailure);
        Assert.Equal(
            [
                "then:get", "then:enumerate", "then-activity:bind", "then:end",
                "else:get", "else:enumerate", "else-failure:bind",
            ],
            log);
        Assert.Equal(1, successfulThen.GetCalls);
        Assert.Equal(1, successfulThen.EnumerationCalls);
        Assert.Equal(1, failingElse.GetCalls);
        Assert.Equal(1, failingElse.EnumerationCalls);
        if (failedElseState is null)
            Assert.Empty(failedElseBuilder.Activities);
        else
            Assert.Empty(failedElseState.Bindings);
    }

    [Theory]
    [InlineData(BinderKind.Normal, false)]
    [InlineData(BinderKind.Normal, true)]
    [InlineData(BinderKind.Message, false)]
    [InlineData(BinderKind.Message, true)]
    [InlineData(BinderKind.Exception, false)]
    [InlineData(BinderKind.Exception, true)]
    [InlineData(BinderKind.MessageException, false)]
    [InlineData(BinderKind.MessageException, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-state-bind-runtime-parity")]
    public async Task StateBind_PublishesExactRuntimeWithUncrossedThenElseExecutionAndInspectionAsync(
        BinderKind kind,
        bool selected)
    {
        var calls = new List<string>();
        var @event = new TriggerEvent("state-bound");
        IActivityBinder<TestSaga> binder = kind switch
        {
            BinderKind.Normal => new ConditionalActivityBinder<TestSaga>(
                @event, _ => Task.FromResult(selected), RuntimeBranch("then", calls), RuntimeBranch("else", calls)),
            BinderKind.Message => new ConditionalActivityBinder<TestSaga, Message>(
                @event, _ => Task.FromResult(selected), RuntimeBranch("then", calls), RuntimeBranch("else", calls)),
            BinderKind.Exception => new ConditionalExceptionActivityBinder<TestSaga, MarkerException>(
                @event, _ => Task.FromResult(selected), RuntimeBranch("then", calls), RuntimeBranch("else", calls)),
            BinderKind.MessageException => new ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>(
                @event, _ => Task.FromResult(selected), RuntimeBranch("then", calls), RuntimeBranch("else", calls)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        IState<TestSaga> state = CreateState(
            new TriggerEvent("enter"),
            new MessageEvent<IState>("before-enter"),
            new MessageEvent<IState>("after-leave"),
            new TriggerEvent("leave"),
            out RecordingStateProxy stateProxy);

        binder.Bind(state);

        (IEvent boundEvent, IStateMachineActivity<TestSaga> activity) = Assert.Single(stateProxy.Bindings);
        Assert.Same(@event, boundEvent);
        Assert.Equal(ExpectedRuntimeType(kind), activity.GetType());

        switch (kind)
        {
            case BinderKind.Normal:
                {
                    IBehaviorContext<TestSaga> context = CreateStrictProxy<IBehaviorContext<TestSaga>>();
                    var next = new RecordingBehavior(calls);
                    await activity.ExecuteAsync(context, next);
                    Assert.All(next.Contexts, candidate => Assert.Same(context, candidate));
                    break;
                }
            case BinderKind.Message:
                {
                    IBehaviorContext<TestSaga, Message> context = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();
                    var next = new RecordingMessageBehavior(calls);
                    await activity.ExecuteAsync(context, next);
                    Assert.All(next.Contexts, candidate => Assert.Same(context, candidate));
                    break;
                }
            case BinderKind.Exception:
                {
                    IBehaviorExceptionContext<TestSaga, MarkerException> context =
                        CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
                    var next = new RecordingBehavior(calls);
                    await activity.FaultedAsync(context, next);
                    Assert.All(next.Contexts, candidate => Assert.Same(context, candidate));
                    break;
                }
            case BinderKind.MessageException:
                {
                    IBehaviorExceptionContext<TestSaga, Message, MarkerException> context =
                        CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
                    var next = new RecordingMessageBehavior(calls);
                    await activity.FaultedAsync(context, next);
                    Assert.All(next.Contexts, candidate => Assert.Same(context, candidate));
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        string operation = kind switch
        {
            BinderKind.Normal => "execute",
            BinderKind.Message => "execute-message",
            BinderKind.Exception => "fault",
            BinderKind.MessageException => "fault-message",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        Assert.Equal([selected ? $"then:{operation}" : $"else:{operation}", $"next:{operation}"], calls);

        calls.Clear();
        activity.Accept(new TraversingVisitor(calls));
        Assert.Equal(["condition:visit", "then:accept", "condition:visit", "else:accept"], calls);

        calls.Clear();
        activity.Probe(new RecordingProbeContext(calls));
        Assert.Equal(["scope:condition", "then:probe", "else:probe"], calls);
    }

    [Theory]
    [InlineData(BinderKind.Normal)]
    [InlineData(BinderKind.Message)]
    [InlineData(BinderKind.Exception)]
    [InlineData(BinderKind.MessageException)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-target-failure-identity")]
    public void Bind_PreservesExactStateAndBuilderFailureIdentity(BinderKind kind)
    {
        IActivityBinder<TestSaga> binder = CreateBinder(
            kind,
            new TriggerEvent("event"),
            EmptyBranch("then"),
            EmptyBranch("else"));
        var stateFailure = new MarkerException("state bind failed");
        IState<TestSaga> state = CreateState(
            new TriggerEvent("enter"),
            new MessageEvent<IState>("before-enter"),
            new MessageEvent<IState>("after-leave"),
            new TriggerEvent("leave"),
            out RecordingStateProxy stateProxy);
        stateProxy.BindFailure = stateFailure;

        MarkerException actualStateFailure = Assert.Throws<MarkerException>(() => binder.Bind(state));

        Assert.Same(stateFailure, actualStateFailure);
        Assert.Empty(stateProxy.Bindings);

        var builderFailure = new MarkerException("builder add failed");
        var builder = new CapturingBuilder { AddFailure = builderFailure };

        MarkerException actualBuilderFailure = Assert.Throws<MarkerException>(() => binder.Bind(builder));

        Assert.Same(builderFailure, actualBuilderFailure);
        Assert.Empty(builder.Activities);
    }

    [Theory]
    [InlineData(BinderKind.Normal)]
    [InlineData(BinderKind.Message)]
    [InlineData(BinderKind.Exception)]
    [InlineData(BinderKind.MessageException)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-snapshot-and-concurrent-materialization")]
    public async Task Bind_FreezesEachPublishedSnapshotAndConcurrentMaterializationsRemainIndependentAsync(BinderKind kind)
    {
        var snapshotLog = new List<string>();
        var thenBinders = new List<IActivityBinder<TestSaga>> { new RecordingBinder("then-original", snapshotLog) };
        var elseBinders = new List<IActivityBinder<TestSaga>> { new RecordingBinder("else-original", snapshotLog) };
        var then = new BranchActivities("then", thenBinders);
        var @else = new BranchActivities("else", elseBinders);
        IActivityBinder<TestSaga> binder = CreateBinder(kind, new TriggerEvent("snapshot"), then, @else);

        IStateMachineActivity<TestSaga> firstSnapshot = Bind(binder);
        thenBinders.Add(new RecordingBinder("then-late", snapshotLog));
        elseBinders.Add(new RecordingBinder("else-late", snapshotLog));
        snapshotLog.Clear();
        firstSnapshot.Accept(new TraversingVisitor());
        Assert.Equal(["then-original:accept", "else-original:accept"], snapshotLog);

        snapshotLog.Clear();
        IStateMachineActivity<TestSaga> secondSnapshot = Bind(binder);
        snapshotLog.Clear();
        secondSnapshot.Accept(new TraversingVisitor());
        Assert.Equal(
            ["then-original:accept", "then-late:accept", "else-original:accept", "else-late:accept"],
            snapshotLog);
        Assert.NotSame(firstSnapshot, secondSnapshot);

        const int bindCount = 16;
        var snapshotId = new AsyncLocal<int>();
        var concurrentThenBinder = new PairingBinder("then", snapshotId);
        var concurrentElseBinder = new PairingBinder("else", snapshotId);
        var concurrentThen = new BranchActivities("then", [concurrentThenBinder]);
        var concurrentElse = new BranchActivities("else", [concurrentElseBinder]);
        IActivityBinder<TestSaga> concurrentBinder = CreateBinder(
            kind,
            new TriggerEvent("concurrent"),
            concurrentThen,
            concurrentElse);
        CapturingBuilder[] targets = Enumerable.Range(0, bindCount).Select(_ => new CapturingBuilder()).ToArray();

        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task[] bindings = targets.Select((target, index) => Task.Run(async () =>
            {
                await gate.Task;
                snapshotId.Value = index;
                concurrentBinder.Bind(target);
            }))
            .ToArray();
        gate.SetResult(true);

        await Task.WhenAll(bindings);

        Assert.All(targets, target => Assert.Single(target.Activities));
        Assert.Equal(bindCount, targets.Select(target => target.Activities[0]).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(bindCount, concurrentThen.GetCalls);
        Assert.Equal(bindCount, concurrentThen.EnumerationCalls);
        Assert.Equal(bindCount, concurrentElse.GetCalls);
        Assert.Equal(bindCount, concurrentElse.EnumerationCalls);
        Assert.Equal(bindCount, concurrentThenBinder.BindCalls);
        Assert.Equal(bindCount, concurrentElseBinder.BindCalls);
        for (var index = 0; index < targets.Length; index++)
        {
            var visitor = new SnapshotContentVisitor();
            targets[index].Activities[0].Accept(visitor);
            Assert.Equal([$"then:{index}", $"else:{index}"], visitor.Labels);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-runtime-context-branch-and-outcome-identity")]
    public async Task BoundActivities_SelectOnlyTheExactBranchAndPreserveContextFailureAndCancellationIdentityAsync()
    {
        await VerifyNormalRuntimeAsync(selected: true);
        await VerifyNormalRuntimeAsync(selected: false);
        await VerifyMessageRuntimeAsync(selected: true);
        await VerifyMessageRuntimeAsync(selected: false);
        await VerifyExceptionRuntimeAsync(selected: true);
        await VerifyExceptionRuntimeAsync(selected: false);
        await VerifyMessageExceptionRuntimeAsync(selected: true);
        await VerifyMessageExceptionRuntimeAsync(selected: false);

        var failure = new MarkerException("condition task failed");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        IBehaviorContext<TestSaga> context = CreateStrictProxy<IBehaviorContext<TestSaga>>();
        var next = new RecordingBehavior([]);

        var failedBinder = new ConditionalActivityBinder<TestSaga>(
            new TriggerEvent("failed"),
            _ => Task.FromException<bool>(failure),
            EmptyBranch("then"),
            EmptyBranch("else"));
        var failedActivity = Assert.IsType<ConditionActivity<TestSaga>>(Bind(failedBinder));
        MarkerException actualFailure = await Assert.ThrowsAsync<MarkerException>(() => failedActivity.ExecuteAsync(context, next));
        Assert.Same(failure, actualFailure);
        Assert.Empty(next.Calls);

        var canceledBinder = new ConditionalExceptionActivityBinder<TestSaga, MarkerException>(
            new TriggerEvent("canceled"),
            _ => Task.FromCanceled<bool>(cancellation.Token),
            EmptyBranch("then"),
            EmptyBranch("else"));
        var canceledActivity = Assert.IsType<ConditionExceptionActivity<TestSaga, MarkerException>>(Bind(canceledBinder));
        IBehaviorExceptionContext<TestSaga, MarkerException> exceptionContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            canceledActivity.FaultedAsync(exceptionContext, next));
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Empty(next.Calls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "iteration-218-conditional-exception-catch-continuation")]
    public async Task ExceptionBranches_CompleteMatchingFaultAndForwardNonmatchingFaultAsync(
        bool message,
        bool selected)
    {
        var calls = new List<string>();
        var thenActivity = new ContinuingFaultActivity("then", calls);
        var elseActivity = new ContinuingFaultActivity("else", calls);
        var then = new BranchActivities("then", [new RecordingBinder(thenActivity)]);
        var @else = new BranchActivities("else", [new RecordingBinder(elseActivity)]);
        var conditionCalls = 0;
        IActivityBinder<TestSaga> binder = message
            ? new ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>(
                new TriggerEvent("message-exception"),
                _ =>
                {
                    conditionCalls++;
                    return Task.FromResult(selected);
                },
                then,
                @else)
            : new ConditionalExceptionActivityBinder<TestSaga, MarkerException>(
                new TriggerEvent("exception"),
                _ =>
                {
                    conditionCalls++;
                    return Task.FromResult(selected);
                },
                then,
                @else);
        IStateMachineActivity<TestSaga> activity = Bind(binder);

        object matchingContext;
        if (message)
        {
            IBehaviorExceptionContext<TestSaga, Message, MarkerException> context =
                CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
            var next = new RecordingMessageBehavior(calls);
            matchingContext = context;
            await activity.FaultedAsync(context, next);
            Assert.Same(context, Assert.Single(next.Contexts));
        }
        else
        {
            IBehaviorExceptionContext<TestSaga, MarkerException> context =
                CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
            var next = new RecordingBehavior(calls);
            matchingContext = context;
            await activity.FaultedAsync(context, next);
            Assert.Same(context, Assert.Single(next.Contexts));
        }

        string faultOperation = message ? "fault-message" : "fault";
        Assert.Equal([selected ? $"then:{faultOperation}" : $"else:{faultOperation}", $"next:{faultOperation}"], calls);
        ContinuingFaultActivity selectedActivity = selected ? thenActivity : elseActivity;
        ContinuingFaultActivity unselectedActivity = selected ? elseActivity : thenActivity;
        Assert.Same(matchingContext, Assert.Single(selectedActivity.Contexts));
        Assert.Empty(unselectedActivity.Contexts);
        Assert.Equal(1, conditionCalls);

        calls.Clear();
        object nonmatchingContext;
        if (message)
        {
            IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> context =
                CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();
            var next = new RecordingMessageBehavior(calls);
            nonmatchingContext = context;
            await activity.FaultedAsync(context, next);
            Assert.Same(context, Assert.Single(next.Contexts));
        }
        else
        {
            IBehaviorExceptionContext<TestSaga, InvalidOperationException> context =
                CreateStrictProxy<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
            var next = new RecordingBehavior(calls);
            nonmatchingContext = context;
            await activity.FaultedAsync(context, next);
            Assert.Same(context, Assert.Single(next.Contexts));
        }

        Assert.Equal([$"next:{faultOperation}"], calls);
        Assert.Same(matchingContext, Assert.Single(selectedActivity.Contexts));
        Assert.DoesNotContain(nonmatchingContext, selectedActivity.Contexts);
        Assert.Empty(unselectedActivity.Contexts);
        Assert.Equal(1, conditionCalls);
    }

    [Theory]
    [InlineData(BinderKind.Normal)]
    [InlineData(BinderKind.Message)]
    [InlineData(BinderKind.Exception)]
    [InlineData(BinderKind.MessageException)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-conditional-binder-visitor-probe-order-and-failure-identity")]
    public void BoundActivities_VisitAndProbeThenBeforeElseAndPreserveFailureIdentity(BinderKind kind)
    {
        var calls = new List<string>();
        var thenActivity = new RecordingActivity("then", calls);
        var elseActivity = new RecordingActivity("else", calls);
        IActivityBinder<TestSaga> binder = CreateBinder(
            kind,
            new TriggerEvent("event"),
            new BranchActivities("then", [new RecordingBinder(thenActivity)]),
            new BranchActivities("else", [new RecordingBinder(elseActivity)]));
        IStateMachineActivity<TestSaga> activity = Bind(binder);

        activity.Accept(new TraversingVisitor(calls));
        Assert.Equal(["condition:visit", "then:accept", "condition:visit", "else:accept"], calls);

        calls.Clear();
        activity.Probe(new RecordingProbeContext(calls));
        Assert.Equal(["scope:condition", "then:probe", "else:probe"], calls);

        var visitFailure = new MarkerException("visit failed");
        thenActivity.AcceptFailure = visitFailure;
        calls.Clear();
        MarkerException actualVisitFailure = Assert.Throws<MarkerException>(() => activity.Accept(new TraversingVisitor(calls)));
        Assert.Same(visitFailure, actualVisitFailure);
        Assert.Equal(["condition:visit", "then:accept"], calls);

        thenActivity.AcceptFailure = null;
        var probeFailure = new MarkerException("probe failed");
        thenActivity.ProbeFailure = probeFailure;
        calls.Clear();
        MarkerException actualProbeFailure = Assert.Throws<MarkerException>(() => activity.Probe(new RecordingProbeContext(calls)));
        Assert.Same(probeFailure, actualProbeFailure);
        Assert.Equal(["scope:condition", "then:probe"], calls);
    }

    static async Task VerifyNormalRuntimeAsync(bool selected)
    {
        var calls = new List<string>();
        IBehaviorContext<TestSaga> context = CreateStrictProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga>? observed = null;
        var binder = new ConditionalActivityBinder<TestSaga>(
            new TriggerEvent("normal"),
            candidate =>
            {
                observed = candidate;
                return Task.FromResult(selected);
            },
            RuntimeBranch("then", calls),
            RuntimeBranch("else", calls));
        var activity = Assert.IsType<ConditionActivity<TestSaga>>(Bind(binder));
        var next = new RecordingBehavior(calls);

        await activity.ExecuteAsync(context, next);

        Assert.Same(context, observed);
        Assert.Equal([selected ? "then:execute" : "else:execute", "next:execute"], calls);
        Assert.All(next.Contexts, candidate => Assert.Same(context, candidate));
    }

    static async Task VerifyMessageRuntimeAsync(bool selected)
    {
        var calls = new List<string>();
        IBehaviorContext<TestSaga, Message> context = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorContext<TestSaga, Message>? observed = null;
        var binder = new ConditionalActivityBinder<TestSaga, Message>(
            new TriggerEvent("message"),
            candidate =>
            {
                observed = candidate;
                return Task.FromResult(selected);
            },
            RuntimeBranch("then", calls),
            RuntimeBranch("else", calls));
        var activity = Assert.IsType<ConditionActivity<TestSaga, Message>>(Bind(binder));
        var next = new RecordingMessageBehavior(calls);

        await activity.ExecuteAsync(context, next);

        Assert.Same(context, observed);
        Assert.Equal([selected ? "then:execute-message" : "else:execute-message", "next:execute-message"], calls);
        Assert.All(next.Contexts, candidate => Assert.Same(context, candidate));
    }

    static async Task VerifyExceptionRuntimeAsync(bool selected)
    {
        var calls = new List<string>();
        IBehaviorExceptionContext<TestSaga, MarkerException> context =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, MarkerException>? observed = null;
        var binder = new ConditionalExceptionActivityBinder<TestSaga, MarkerException>(
            new TriggerEvent("exception"),
            candidate =>
            {
                observed = candidate;
                return Task.FromResult(selected);
            },
            RuntimeBranch("then", calls),
            RuntimeBranch("else", calls));
        var activity = Assert.IsType<ConditionExceptionActivity<TestSaga, MarkerException>>(Bind(binder));
        var next = new RecordingBehavior(calls);

        await activity.FaultedAsync(context, next);

        Assert.Same(context, observed);
        Assert.Equal([selected ? "then:fault" : "else:fault", "next:fault"], calls);
        Assert.All(next.Contexts, candidate => Assert.Same(context, candidate));
    }

    static async Task VerifyMessageExceptionRuntimeAsync(bool selected)
    {
        var calls = new List<string>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> context =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException>? observed = null;
        var binder = new ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>(
            new TriggerEvent("message-exception"),
            candidate =>
            {
                observed = candidate;
                return Task.FromResult(selected);
            },
            RuntimeBranch("then", calls),
            RuntimeBranch("else", calls));
        var activity = Assert.IsType<ConditionExceptionActivity<TestSaga, Message, MarkerException>>(Bind(binder));
        var next = new RecordingMessageBehavior(calls);

        await activity.FaultedAsync(context, next);

        Assert.Same(context, observed);
        Assert.Equal([selected ? "then:fault-message" : "else:fault-message", "next:fault-message"], calls);
        Assert.All(next.Contexts, candidate => Assert.Same(context, candidate));
    }

    static object?[] CreateConstructorArguments(ConstructorInfo constructor)
    {
        ParameterInfo[] parameters = constructor.GetParameters();
        return
        [
            new TriggerEvent("event"),
            CreateCondition(parameters[1].ParameterType),
            EmptyBranch("then"),
            EmptyBranch("else"),
        ];
    }

    static Delegate CreateCondition(Type delegateType)
    {
        MethodInfo invoke = delegateType.GetMethod("Invoke")!;
        var context = Expression.Parameter(invoke.GetParameters()[0].ParameterType, "context");
        Expression body = invoke.ReturnType == typeof(bool)
            ? Expression.Constant(true)
            : Expression.Constant(Task.FromResult(true), typeof(Task<bool>));
        return Expression.Lambda(delegateType, body, context).Compile();
    }

    static IActivityBinder<TestSaga> CreateBinder(
        BinderKind kind,
        IEvent @event,
        IEventActivities<TestSaga> thenActivities,
        IEventActivities<TestSaga> elseActivities) =>
        kind switch
        {
            BinderKind.Normal => new ConditionalActivityBinder<TestSaga>(
                @event, (StateMachineAsyncCondition<TestSaga>)(_ => Task.FromResult(true)), thenActivities, elseActivities),
            BinderKind.Message => new ConditionalActivityBinder<TestSaga, Message>(
                @event, (StateMachineAsyncCondition<TestSaga, Message>)(_ => Task.FromResult(true)), thenActivities, elseActivities),
            BinderKind.Exception => new ConditionalExceptionActivityBinder<TestSaga, MarkerException>(
                @event, (StateMachineAsyncExceptionCondition<TestSaga, MarkerException>)(_ => Task.FromResult(true)),
                thenActivities, elseActivities),
            BinderKind.MessageException => new ConditionalExceptionActivityBinder<TestSaga, Message, MarkerException>(
                @event, (StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException>)(_ => Task.FromResult(true)),
                thenActivities, elseActivities),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    static Type ExpectedRuntimeType(BinderKind kind) => kind switch
    {
        BinderKind.Normal => typeof(ConditionActivity<TestSaga>),
        BinderKind.Message => typeof(ConditionActivity<TestSaga, Message>),
        BinderKind.Exception => typeof(ConditionExceptionActivity<TestSaga, MarkerException>),
        BinderKind.MessageException => typeof(ConditionExceptionActivity<TestSaga, Message, MarkerException>),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    static IStateMachineActivity<TestSaga> Bind(IActivityBinder<TestSaga> binder)
    {
        var builder = new CapturingBuilder();
        binder.Bind(builder);
        return Assert.Single(builder.Activities);
    }

    static TDelegate GetCondition<TDelegate>(IStateMachineActivity<TestSaga> activity)
        where TDelegate : Delegate
    {
        FieldInfo field = activity.GetType().GetField("_condition", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new Xunit.Sdk.XunitException($"{activity.GetType()} does not expose its condition field for contract verification.");
        return Assert.IsType<TDelegate>(field.GetValue(activity));
    }

    static void AssertSynchronousFailure<TContext>(
        IActivityBinder<TestSaga> binder,
        TContext context,
        MarkerException expected)
        where TContext : class
    {
        IStateMachineActivity<TestSaga> activity = Bind(binder);
        FieldInfo field = activity.GetType().GetField("_condition", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new Xunit.Sdk.XunitException("The bound condition activity does not expose its condition field.");
        var condition = Assert.IsAssignableFrom<Delegate>(field.GetValue(activity));
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => condition.DynamicInvoke(context));
        Assert.Same(expected, invocation.InnerException);
    }

    static void AssertParam(string name, Action action) =>
        Assert.Equal(name, Assert.Throws<ArgumentNullException>(action).ParamName);

    static void AssertSagaParameter(Type parameter)
    {
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISagaStateMachineInstance)], parameter.GetGenericParameterConstraints());
    }

    static void AssertReferenceTypeParameter(Type parameter)
    {
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Empty(parameter.GetGenericParameterConstraints());
    }

    static void AssertExceptionParameter(Type parameter)
    {
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(
            GenericParameterAttributes.None,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(Exception)], parameter.GetGenericParameterConstraints());
    }

    static BranchActivities EmptyBranch(string name) => new(name, []);

    static BranchActivities RuntimeBranch(string name, List<string> calls) =>
        new(name, [new RecordingBinder(new RecordingActivity(name, calls))]);

    static T CreateStrictProxy<T>()
        where T : class => DispatchProxy.Create<T, StrictDispatchProxy>();

    static IState<TestSaga> CreateState(
        IEvent enter,
        IEvent<IState> beforeEnter,
        IEvent<IState> afterLeave,
        IEvent leave,
        out RecordingStateProxy proxy,
        List<string>? log = null)
    {
        IState<TestSaga> state = DispatchProxy.Create<IState<TestSaga>, RecordingStateProxy>();
        proxy = (RecordingStateProxy)(object)state;
        proxy.Enter = enter;
        proxy.BeforeEnter = beforeEnter;
        proxy.AfterLeave = afterLeave;
        proxy.Leave = leave;
        proxy.Log = log;
        return state;
    }

    public enum BinderKind
    {
        Normal,
        Message,
        Exception,
        MessageException,
    }

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record Message;

    public sealed class MarkerException(string message) : Exception(message);

    sealed class BranchActivities : IEventActivities<TestSaga>
    {
        readonly IReadOnlyList<IActivityBinder<TestSaga>> _binders;
        readonly List<string>? _log;
        readonly string _name;
        int _enumerationCalls;
        int _getCalls;

        public BranchActivities(string name, IReadOnlyList<IActivityBinder<TestSaga>> binders, List<string>? log = null)
        {
            _name = name;
            _binders = binders;
            _log = log;
        }

        public int EnumerationCalls => Volatile.Read(ref _enumerationCalls);

        public int GetCalls => Volatile.Read(ref _getCalls);

        public IEnumerable<IActivityBinder<TestSaga>> GetStateActivityBinders()
        {
            Interlocked.Increment(ref _getCalls);
            _log?.Add($"{_name}:get");
            return Enumerate();
        }

        IEnumerable<IActivityBinder<TestSaga>> Enumerate()
        {
            Interlocked.Increment(ref _enumerationCalls);
            _log?.Add($"{_name}:enumerate");
            foreach (IActivityBinder<TestSaga> binder in _binders)
                yield return binder;
            _log?.Add($"{_name}:end");
        }
    }

    sealed class CapturingBuilder(List<string>? log = null) : IBehaviorBuilder<TestSaga>
    {
        public MarkerException? AddFailure { get; set; }

        public List<IStateMachineActivity<TestSaga>> Activities { get; } = [];

        public void Add(IStateMachineActivity<TestSaga> activity)
        {
            log?.Add("target:add");
            if (AddFailure is { } failure)
                throw failure;
            Activities.Add(activity);
        }
    }

    sealed class RecordingBinder : IActivityBinder<TestSaga>
    {
        readonly IStateMachineActivity<TestSaga> _activity;
        readonly MarkerException? _bindFailure;
        readonly List<string>? _log;
        readonly string _name;

        public RecordingBinder(string name, List<string> log, MarkerException? bindFailure = null)
            : this(new RecordingActivity(name, log), name, log, bindFailure)
        {
        }

        public RecordingBinder(IStateMachineActivity<TestSaga> activity)
            : this(activity, "activity", null, null)
        {
        }

        RecordingBinder(
            IStateMachineActivity<TestSaga> activity,
            string name,
            List<string>? log,
            MarkerException? bindFailure)
        {
            _activity = activity;
            _name = name;
            _log = log;
            _bindFailure = bindFailure;
        }

        public IEvent Event { get; } = new TriggerEvent("nested");

        public bool IsStateTransitionEvent(IState state) => false;

        public void Bind(IState<TestSaga> state) => throw new Xunit.Sdk.XunitException("A conditional branch bound directly to a state.");

        public void Bind(IBehaviorBuilder<TestSaga> builder)
        {
            _log?.Add($"{_name}:bind");
            if (_bindFailure is { } failure)
                throw failure;
            builder.Add(_activity);
        }
    }

    sealed class PairingBinder(string branch, AsyncLocal<int> snapshotId) : IActivityBinder<TestSaga>
    {
        int _bindCalls;

        public int BindCalls => Volatile.Read(ref _bindCalls);

        public IEvent Event { get; } = new TriggerEvent("counting");

        public bool IsStateTransitionEvent(IState state) => false;

        public void Bind(IState<TestSaga> state) => throw new Xunit.Sdk.XunitException("A conditional branch bound directly to a state.");

        public void Bind(IBehaviorBuilder<TestSaga> builder)
        {
            Interlocked.Increment(ref _bindCalls);
            builder.Add(new SnapshotActivity($"{branch}:{snapshotId.Value}"));
        }
    }

    sealed class SnapshotActivity(string label) : IStateMachineActivity<TestSaga>
    {
        public string Label => label;

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context) => throw Unexpected();

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next) => throw Unexpected();

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class => throw Unexpected();

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context, IBehavior<TestSaga> next)
            where TException : Exception => throw Unexpected();

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<TestSaga, T, TException> context,
            IBehavior<TestSaga, T> next)
            where T : class
            where TException : Exception => throw Unexpected();

        static Exception Unexpected() =>
            new Xunit.Sdk.XunitException("A snapshot-content activity was executed instead of inspected.");
    }

    sealed class RecordingActivity(string name, List<string> calls) : IStateMachineActivity<TestSaga>
    {
        public MarkerException? AcceptFailure { get; set; }

        public MarkerException? ProbeFailure { get; set; }

        public void Accept(IStateMachineVisitor visitor)
        {
            calls.Add($"{name}:accept");
            if (AcceptFailure is { } failure)
                throw failure;
        }

        public void Probe(ProbeContext context)
        {
            calls.Add($"{name}:probe");
            if (ProbeFailure is { } failure)
                throw failure;
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next)
        {
            calls.Add($"{name}:execute");
            return Task.CompletedTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class
        {
            calls.Add($"{name}:execute-message");
            return Task.CompletedTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context, IBehavior<TestSaga> next)
            where TException : Exception
        {
            calls.Add($"{name}:fault");
            return Task.CompletedTask;
        }

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<TestSaga, T, TException> context,
            IBehavior<TestSaga, T> next)
            where T : class
            where TException : Exception
        {
            calls.Add($"{name}:fault-message");
            return Task.CompletedTask;
        }
    }

    sealed class ContinuingFaultActivity(string name, List<string> calls) : IStateMachineActivity<TestSaga>
    {
        public List<object> Contexts { get; } = [];

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context) => context.CreateScope(name);

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next) => throw Unexpected();

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class => throw Unexpected();

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<TestSaga, TException> context,
            IBehavior<TestSaga> next)
            where TException : Exception
        {
            Contexts.Add(context);
            calls.Add($"{name}:fault");
            return next.FaultedAsync(context);
        }

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<TestSaga, T, TException> context,
            IBehavior<TestSaga, T> next)
            where T : class
            where TException : Exception
        {
            Contexts.Add(context);
            calls.Add($"{name}:fault-message");
            return next.FaultedAsync(context);
        }

        static Exception Unexpected() =>
            new Xunit.Sdk.XunitException("A fault-continuation activity was executed outside the fault path.");
    }

    sealed class RecordingBehavior(List<string> calls) : IBehavior<TestSaga>
    {
        public List<string> Calls => calls;

        public List<object> Contexts { get; } = [];

        public void Accept(IStateMachineVisitor visitor)
        {
        }

        public void Probe(ProbeContext context)
        {
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context)
        {
            Contexts.Add(context);
            calls.Add("next:execute");
            return Task.CompletedTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context)
            where T : class
        {
            Contexts.Add(context);
            calls.Add("next:execute-message");
            return Task.CompletedTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception
        {
            Contexts.Add(context);
            calls.Add("next:fault");
            return Task.CompletedTask;
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class
            where TException : Exception
        {
            Contexts.Add(context);
            calls.Add("next:fault-message");
            return Task.CompletedTask;
        }
    }

    sealed class RecordingMessageBehavior(List<string> calls) : IBehavior<TestSaga, Message>
    {
        public List<object> Contexts { get; } = [];

        public void Accept(IStateMachineVisitor visitor)
        {
        }

        public void Probe(ProbeContext context)
        {
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context)
        {
            Contexts.Add(context);
            calls.Add("next:execute-message");
            return Task.CompletedTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context)
            where TException : Exception
        {
            Contexts.Add(context);
            calls.Add("next:fault-message");
            return Task.CompletedTask;
        }
    }

    sealed class TraversingVisitor(List<string>? calls = null) : IStateMachineVisitor
    {
        public void Visit(IState state, Action<IState> next) => next(state);

        public void Visit(IEvent @event, Action<IEvent> next) => next(@event);

        public void Visit<T>(IEvent<T> @event, Action<IEvent<T>> next)
            where T : class => next(@event);

        public void Visit(IStateMachineActivity activity)
        {
        }

        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => next(activity);

        public void Visit<T>(IBehavior<T> behavior)
            where T : class, ISagaStateMachineInstance
        {
        }

        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
            where T : class, ISagaStateMachineInstance => next(behavior);

        public void Visit<T, TData>(IBehavior<T, TData> behavior)
            where T : class, ISagaStateMachineInstance
            where TData : class
        {
        }

        public void Visit<T, TData>(IBehavior<T, TData> behavior, Action<IBehavior<T, TData>> next)
            where T : class, ISagaStateMachineInstance
            where TData : class => next(behavior);

        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next)
        {
            calls?.Add("condition:visit");
            next(activity);
        }
    }

    sealed class SnapshotContentVisitor : IStateMachineVisitor
    {
        public List<string> Labels { get; } = [];

        public void Visit(IState state, Action<IState> next) => next(state);

        public void Visit(IEvent @event, Action<IEvent> next) => next(@event);

        public void Visit<T>(IEvent<T> @event, Action<IEvent<T>> next)
            where T : class => next(@event);

        public void Visit(IStateMachineActivity activity)
        {
            if (activity is SnapshotActivity snapshot)
                Labels.Add(snapshot.Label);
        }

        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => next(activity);

        public void Visit<T>(IBehavior<T> behavior)
            where T : class, ISagaStateMachineInstance
        {
        }

        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
            where T : class, ISagaStateMachineInstance => next(behavior);

        public void Visit<T, TData>(IBehavior<T, TData> behavior)
            where T : class, ISagaStateMachineInstance
            where TData : class
        {
        }

        public void Visit<T, TData>(IBehavior<T, TData> behavior, Action<IBehavior<T, TData>> next)
            where T : class, ISagaStateMachineInstance
            where TData : class => next(behavior);

        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next) => next(activity);
    }

    sealed class RecordingProbeContext(List<string> calls) : ProbeContext
    {
        public CancellationToken CancellationToken => default;

        public void Add(string key, string? value)
        {
        }

        public void Add(string key, object? value)
        {
        }

        public void Set(object values)
        {
        }

        public void Set(IEnumerable<KeyValuePair<string, object?>> values)
        {
        }

        public ProbeContext CreateScope(string key)
        {
            calls.Add($"scope:{key}");
            return this;
        }
    }

    public class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new Xunit.Sdk.XunitException($"The condition unexpectedly observed context member '{targetMethod?.Name}'.");
    }

    public class RecordingStateProxy : DispatchProxy
    {
        public IEvent<IState> AfterLeave { get; set; } = null!;

        public IEvent<IState> BeforeEnter { get; set; } = null!;

        public List<(IEvent Event, IStateMachineActivity<TestSaga> Activity)> Bindings { get; } = [];

        public MarkerException? BindFailure { get; set; }

        public IEvent Enter { get; set; } = null!;

        public IEvent Leave { get; set; } = null!;

        public List<string>? Log { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name switch
            {
                "get_AfterLeave" => AfterLeave,
                "get_BeforeEnter" => BeforeEnter,
                "get_Enter" => Enter,
                "get_Leave" => Leave,
                "get_Name" => "State",
                "Bind" => RecordBinding(args),
                _ => throw new Xunit.Sdk.XunitException($"The binder unexpectedly called state member '{targetMethod.Name}'."),
            };
        }

        object? RecordBinding(object?[]? args)
        {
            Assert.NotNull(args);
            var @event = Assert.IsAssignableFrom<IEvent>(args[0]);
            var activity = Assert.IsAssignableFrom<IStateMachineActivity<TestSaga>>(args[1]);
            Log?.Add("target:add");
            if (BindFailure is { } failure)
                throw failure;
            Bindings.Add((@event, activity));
            return null;
        }
    }
}
