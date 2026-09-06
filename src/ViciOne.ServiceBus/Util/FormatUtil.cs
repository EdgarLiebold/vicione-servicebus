using ViciOne.ServiceBus.NewIdFormatters;

namespace ViciOne.ServiceBus.Util;

/// <summary>Provides utility operations for format.</summary>
public static class FormatUtil
{
    /// <summary>Exposes the formatter used by the containing type.</summary>
    public static readonly INewIdFormatter Formatter = new ZBase32Formatter();
}
