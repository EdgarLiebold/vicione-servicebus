namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// When added to a consuming type (consumer, saga, activity, etc), prevents
    /// ViciOne.ServiceBus from configuring endpoint for it when ConfigureEndpoints called
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ExcludeFromConfigureEndpointsAttribute :
        Attribute
    {
    }
}
