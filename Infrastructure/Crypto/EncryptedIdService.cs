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

        return new EncryptedId($"obf_{token}");
    }

    public long Decrypt(EncryptedId encryptedId)
    {
        // Remover prefixo "obf_" se existir
        var value = encryptedId.Value.StartsWith("obf_") 
            ? encryptedId.Value.Substring(4) 
            : encryptedId.Value;
            
        var cipher = Base64UrlDecode(value);
        var payload = AesDecrypt(cipher);
        
        // Validar assinatura antes de retornar
        ValidatePayload(payload, out var id);
        
        return id;
    }

    // ------------------ helpers ------------------

    private byte[] BuildPayload(long id)
    {
        var buffer = new byte[32];

        // 1️⃣ ID (8 bytes)
        BitConverter.GetBytes(id).CopyTo(buffer, 0);

        // 2️⃣ nonce determinístico (8 bytes) - baseado no ID + chave
        using var hmac = new HMACSHA256(_key);
        var nonce = hmac.ComputeHash(BitConverter.GetBytes(id));
        Array.Copy(nonce, 0, buffer, 8, 8);

        // 3️⃣ assinatura (16 bytes) - validação de integridade
        var signature = hmac.ComputeHash(buffer[..16]);
        Array.Copy(signature, 0, buffer, 16, 16);

        return buffer;
    }

    private bool ValidatePayload(byte[] payload, out long id)
    {
        if (payload.Length != 32)
            throw new ArgumentException("Payload inválido. Tamanho incorreto.");
        
        id = BitConverter.ToInt64(payload, 0);
        
        // Verificar assinatura
        using var hmac = new HMACSHA256(_key);
        var expectedSignature = hmac.ComputeHash(payload[..16]);
        
        // Comparar assinaturas byte a byte
        for (int i = 0; i < 16; i++)
        {
            if (payload[16 + i] != expectedSignature[i])
                throw new ArgumentException("Token inválido ou corrompido. Assinatura não confere.");
        }
        
        return true;
    }

    private byte[] AesEncrypt(byte[] input)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.ECB; // Determinístico - sempre gera mesmo output para mesmo input
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