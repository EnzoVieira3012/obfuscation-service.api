using Domain.ValueObjects;

namespace Application.Interfaces.Crypto;

public interface IEncryptedIdService
{
    EncryptedId Encrypt(long id);
    long Decrypt(EncryptedId encryptedId);
}