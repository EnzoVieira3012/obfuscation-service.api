using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Crypto;
using Domain.ValueObjects;

namespace Infrastructure.Crypto;

public sealed class EncryptedIdService : IEncryptedIdService
{
    private readonly byte[] _key;

    public EncryptedIdService(IConfiguration configuration)
    {
        var secret = configuration["ENCRYPTED_ID_SECRET"]
            ?? throw new InvalidOperationException("ENCRYPTED_ID_SECRET não configurado.");

        using var sha = SHA256.Create();
        _key = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
    }

    public EncryptedId Encrypt(long id)
    {
        var payload = BuildPayload(id);          // 32 bytes
        var cipher = AesEncrypt(payload);        // 32 bytes
        var token = Base64UrlEncode(cipher);     // 64 chars

        return new EncryptedId(token);
    }

    public long Decrypt(EncryptedId encryptedId)
    {
        var cipher = Base64UrlDecode(encryptedId.Value);
        var payload = AesDecrypt(cipher);

        return BitConverter.ToInt64(payload, 0);
    }

    // ------------------ helpers ------------------

    private byte[] BuildPayload(long id)
    {
        var buffer = new byte[32];

        // 1️⃣ ID (8 bytes)
        BitConverter.GetBytes(id).CopyTo(buffer, 0);

        // 2️⃣ nonce determinístico (8 bytes)
        using var hmac = new HMACSHA256(_key);
        var nonce = hmac.ComputeHash(BitConverter.GetBytes(id));
        Array.Copy(nonce, 0, buffer, 8, 8);

        // 3️⃣ assinatura (16 bytes)
        var signature = hmac.ComputeHash(buffer[..16]);
        Array.Copy(signature, 0, buffer, 16, 16);

        return buffer;
    }

    private byte[] AesEncrypt(byte[] input)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(input, 0, input.Length);
    }

    private byte[] AesDecrypt(byte[] input)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(input, 0, input.Length);
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    private static byte[] Base64UrlDecode(string input)
    {
        input = input.Replace("-", "+").Replace("_", "/");
        while (input.Length % 4 != 0)
            input += "=";

        return Convert.FromBase64String(input);
    }
}
