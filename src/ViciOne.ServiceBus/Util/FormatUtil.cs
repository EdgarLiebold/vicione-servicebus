using ViciOne.ServiceBus.NewIdFormatters;

namespace ViciOne.ServiceBus.Util;

public static class FormatUtil
{
    public static readonly INewIdFormatter Formatter = new ZBase32Formatter();
}
