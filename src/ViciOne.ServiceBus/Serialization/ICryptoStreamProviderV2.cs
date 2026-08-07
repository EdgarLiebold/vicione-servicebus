// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    using System.IO;


    public interface ICryptoStreamProviderV2 :
        IProbeSite
    {
        Stream GetDecryptStream(Stream stream, Headers headers);

        Stream GetEncryptStream(Stream stream, Headers headers);
    }
}
