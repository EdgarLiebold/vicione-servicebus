using System;
using System.Text;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides the strict, BOM-free UTF-8 encoding used for message text and metadata.</summary>
internal static class MessageDefaults
{
    static readonly Lazy<Encoding> _encoding = new(() => new UTF8Encoding(false, true));

    /// <summary>Gets the shared encoding that rejects malformed byte sequences.</summary>
    public static Encoding Encoding => _encoding.Value;
}
