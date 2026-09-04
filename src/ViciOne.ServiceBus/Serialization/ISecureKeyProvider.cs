namespace ViciOne.ServiceBus.Serialization;

public interface ISecureKeyProvider :
    IProbeSite
{
    byte[] GetKey(Headers headers);
}
