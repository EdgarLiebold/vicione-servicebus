// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Util
{
    using NewIdFormatters;


    public static class FormatUtil
    {
        public static readonly INewIdFormatter Formatter = new ZBase32Formatter();
    }
}
