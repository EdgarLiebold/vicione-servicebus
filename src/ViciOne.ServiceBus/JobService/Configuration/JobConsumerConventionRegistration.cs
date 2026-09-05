namespace ViciOne.ServiceBus.Configuration;

static class JobConsumerConventionRegistration
{
    internal static void Register()
    {
        ConsumerConvention.Register<JobConsumerConvention>();
    }
}
