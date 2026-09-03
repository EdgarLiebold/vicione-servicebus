namespace ViciOne.ServiceBus.Configuration
{
    using System;


    /// <summary>
    /// Configures application-wide message-contract conventions before the first bus topology is created.
    /// Bus- and endpoint-specific runtime policy belongs on their individual configurators.
    /// </summary>
    public static class ApplicationMessageTopology
    {
        public static void ExcludeFromConsumeTopology<T>()
        {
            GlobalTopology.MarkMessageTypeNotConsumable(typeof(T));
        }

        public static void SeparatePublishFromSendConventions()
        {
            GlobalTopology.SeparatePublishFromSend();
        }
    }
}
