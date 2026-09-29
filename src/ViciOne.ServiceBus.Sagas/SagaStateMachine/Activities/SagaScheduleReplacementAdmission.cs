using System;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.SagaStateMachine;

internal static class SagaScheduleReplacementAdmission
{
    public static void RequireSafeReplacement(ConsumeContext context, MessageSchedulerContext scheduler,
        Guid? previousTokenId)
    {
        if (!previousTokenId.HasValue || scheduler is not IScheduleCancellationCapability capability)
            return;

        bool unsafeReplacement = capability.CancellationMode switch
        {
            ScheduleCancellationMode.Unsupported or ScheduleCancellationMode.CallerSpecifiedTokenWithoutCancellation =>
                context.GetSchedulingTokenId() != previousTokenId,
            ScheduleCancellationMode.ProviderAssignedToken => true,
            _ => false
        };

        if (unsafeReplacement)
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Saga", "unknown",
                    "The message scheduler cannot safely replace an existing saga schedule",
                    "Use a scheduler with caller-specified cancellation tokens or avoid replacing an active schedule"));
    }

    public static void RequireSupportedCancellation(MessageSchedulerContext scheduler)
    {
        if (scheduler is IScheduleCancellationCapability
            { CancellationMode: ScheduleCancellationMode.Unsupported or ScheduleCancellationMode.CallerSpecifiedTokenWithoutCancellation })
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Saga", "unknown",
                    "The message scheduler cannot safely cancel an existing saga schedule",
                    "Use a scheduler with caller-specified cancellation tokens before unscheduling an active message"));
    }
}
