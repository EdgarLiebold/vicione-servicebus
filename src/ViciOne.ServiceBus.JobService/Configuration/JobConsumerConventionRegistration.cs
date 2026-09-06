namespace ViciOne.ServiceBus.Configuration;

internal static class JobConsumerConventionRegistration
{
    internal static void Register()
    {
        ConsumerConvention.Register<JobConsumerConvention>();
    }
}
