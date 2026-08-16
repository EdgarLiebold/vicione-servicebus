namespace ViciOne.ServiceBus.DependencyInjection
{
    using Microsoft.Extensions.Options;


    public class ValidateViciOneServiceBusHostOptions :
        IValidateOptions<ViciOneServiceBusHostOptions>
    {
        public ValidateOptionsResult Validate(string name, ViciOneServiceBusHostOptions options)
        {
            if (options.StopTimeout < options.ConsumerStopTimeout)
                return ValidateOptionsResult.Fail($"{nameof(options.ConsumerStopTimeout)} should be less than or equals to ${nameof(options.StopTimeout)}");

            return ValidateOptionsResult.Success;
        }
    }
}
