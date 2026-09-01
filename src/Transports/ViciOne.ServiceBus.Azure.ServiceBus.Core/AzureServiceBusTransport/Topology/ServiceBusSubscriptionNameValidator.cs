namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using System;
    using System.Text.RegularExpressions;


    public class ServiceBusSubscriptionNameValidator :
        IEntityNameValidator
    {
        const int MaxLength = 50;
        static readonly Regex _regex = new Regex(@"^[A-Za-z0-9\-_\.]+$", RegexOptions.Compiled);

        public static IEntityNameValidator Validator => Cached.EntityNameValidator;

        public void ThrowIfInvalidEntityName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ConfigurationException("The Azure Service Bus subscription name must not be null or empty.");

            var success = IsValidEntityName(name);
            if (!success)
                throw new ConfigurationException(
                    $"The Azure Service Bus subscription name '{name}' must be at most {MaxLength} characters and contain only letters, digits, hyphens, underscores, or periods.");
        }

        public bool IsValidEntityName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length <= MaxLength && _regex.IsMatch(name);
        }


        static class Cached
        {
            internal static readonly IEntityNameValidator EntityNameValidator = new ServiceBusSubscriptionNameValidator();
        }
    }
}
