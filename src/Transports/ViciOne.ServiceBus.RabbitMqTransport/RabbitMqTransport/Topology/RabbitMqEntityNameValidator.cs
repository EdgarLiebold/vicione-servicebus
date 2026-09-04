using System.Text;
using System.Text.RegularExpressions;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public sealed partial class RabbitMqEntityNameValidator :
    IEntityNameValidator
{
    const int MaxEntityNameBytes = 255;

    public static IEntityNameValidator Validator => Cached.EntityNameValidator;

    public void ThrowIfInvalidEntityName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new RabbitMqAddressException("The entity name must not be null, empty, or whitespace.");

        if (Encoding.UTF8.GetByteCount(name) > MaxEntityNameBytes)
            throw new RabbitMqAddressException($"The UTF-8 encoded entity name must not exceed {MaxEntityNameBytes} bytes.");

        if (!EntityNamePattern().IsMatch(name))
        {
            throw new RabbitMqAddressException(
                "The entity name may contain only Unicode letters and decimal digits, hyphen, underscore, period, or colon.");
        }
    }

    public bool IsValidEntityName(string name)
    {
        return !string.IsNullOrWhiteSpace(name)
            && Encoding.UTF8.GetByteCount(name) <= MaxEntityNameBytes
            && EntityNamePattern().IsMatch(name);
    }

    [GeneratedRegex(@"^[\p{L}\p{Nd}_\-.:]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EntityNamePattern();


    static class Cached
    {
        internal static readonly IEntityNameValidator EntityNameValidator = new RabbitMqEntityNameValidator();
    }
}
