using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class SchedulingExtensionContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULER-BOUNDARY", "every-extension-rejects-null-scheduler")]
    public void EveryAdvancedSchedulerExtension_RejectsANullScheduler()
    {
        MethodInfo[] methods = typeof(AdvancedMessageSchedulerExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(static method => method.ToString(), StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(methods);
        foreach (MethodInfo definition in methods)
        {
            MethodInfo method = definition.IsGenericMethodDefinition
                ? definition.MakeGenericMethod(typeof(ProbeMessage))
                : definition;
            object?[] arguments = CreateNullReceiverArguments(method);

            TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
            ArgumentNullException exception = Assert.IsType<ArgumentNullException>(invocation.InnerException);
            Assert.Equal("scheduler", exception.ParamName);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-CONTEXT-BOUNDARY", "adapter-is-sealed-and-requires-collaborators")]
    public void ConsumeSchedulerContext_IsSealedAndRequiresItsCollaborators()
    {
        Assert.True(typeof(ConsumeMessageSchedulerContext).IsSealed);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() =>
            new ConsumeMessageSchedulerContext(null!, _ => throw new InvalidOperationException())).ParamName);

        ConsumeContext context = DispatchProxy.Create<ConsumeContext, UnexpectedInvocationProxy>();
        Assert.Equal("schedulerFactory", Assert.Throws<ArgumentNullException>(() =>
            new ConsumeMessageSchedulerContext(context, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULING-EXTENSION-BOUNDARY", "every-overload-rejects-null-context")]
    public void EverySchedulingExtension_RejectsANullConsumeContext()
    {
        Type[] extensionTypes =
        [
            typeof(ConsumeContextSchedulerExtensions),
            typeof(ConsumeContextSelfSchedulerExtensions),
            typeof(SchedulePublishExtensions),
            typeof(SchedulingExtensions),
        ];

        foreach (Type extensionType in extensionTypes)
        {
            MethodInfo[] methods = extensionType
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .OrderBy(static method => method.ToString(), StringComparer.Ordinal)
                .ToArray();

            Assert.NotEmpty(methods);
            foreach (MethodInfo definition in methods)
            {
                MethodInfo method = definition.IsGenericMethodDefinition
                    ? definition.MakeGenericMethod(typeof(ProbeMessage))
                    : definition;
                object?[] arguments = CreateNullReceiverArguments(method);

                TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
                ArgumentNullException exception = Assert.IsType<ArgumentNullException>(invocation.InnerException);
                Assert.Equal("context", exception.ParamName);
            }
        }
    }

    private static object?[] CreateNullReceiverArguments(MethodInfo method) =>
        method
            .GetParameters()
            .Select(static (parameter, index) => index == 0
                ? null
                : parameter.ParameterType.IsValueType
                    ? Activator.CreateInstance(parameter.ParameterType)
                    : null)
            .ToArray();

    private sealed record ProbeMessage;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The invalid scheduler boundary invoked {targetMethod?.Name}.");
    }
}
