namespace ViciOne.ServiceBus.NewIdParsers;

/// <summary>
/// Provides a z base32 parser implementation.
/// </summary>
public class ZBase32Parser :
    Base32Parser
{
    const string ConvertChars = "ybndrfg8ejkmcpqxot1uwisza345h769YBNDRFG8EJKMCPQXOT1UWISZA345H769";

    const string TransposeChars = "ybndrfg8ejkmcpqx0tlvwis2a345h769YBNDRFG8EJKMCPQX0TLVWIS2A345H769";

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handleTransposedCharacters">The handle transposed characters value.</param>
    public ZBase32Parser(bool handleTransposedCharacters = false)
        : base(handleTransposedCharacters ? ConvertChars + TransposeChars : ConvertChars)
    {
    }
}
