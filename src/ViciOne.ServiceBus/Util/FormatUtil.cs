using ViciOne.ServiceBus.NewIdFormatters;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides a format util implementation.
/// </summary>
public static class FormatUtil
{
    /// <summary>
    /// Defines the formatter value.
    /// </summary>
    public static readonly INewIdFormatter Formatter = new ZBase32Formatter();
}
