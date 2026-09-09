using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class ReliableMessagingOptionsValidator<TBus> : IValidateOptions<ReliableMessagingOptions<TBus>>
    where TBus : class, IBus
{
    public ValidateOptionsResult Validate(string? name, ReliableMessagingOptions<TBus> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            _ = options.ValidateAndFreeze();
            return ValidateOptionsResult.Success;
        }
        catch (ConfigurationException exception)
        {
            string bus = BusRegistrationIdentity.GetKey(typeof(TBus));
            return ValidateOptionsResult.Fail(
                $"Reliable messaging for bus '{bus}': {exception.Message} Correct the named value before starting the host.");
        }
    }
}
