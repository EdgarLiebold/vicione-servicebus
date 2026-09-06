using System;
using System.Text;

namespace ViciOne.ServiceBus.Advanced;

internal static class MessageDefaults
{
    static readonly Lazy<Encoding> _encoding = new(() => new UTF8Encoding(false, true));

    public static Encoding Encoding => _encoding.Value;
}
