using System.IO;

namespace ViciOne.ServiceBus.Serialization;

public interface ICryptoStreamProviderV2 :
    IProbeSite
{
    Stream GetDecryptStream(Stream stream, Headers headers);

    Stream GetEncryptStream(Stream stream, Headers headers);
}
