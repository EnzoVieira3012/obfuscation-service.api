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
        var secretKey = configuration["ENCRYPTED_ID_SECRET"]
            ?? throw new InvalidOperationException("ENCRYPTED_ID_SECRET não configurada");

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("A chave secreta não pode ser vazia", nameof(secretKey));
        
        using var sha = SHA256.Create();
        _key = sha.ComputeHash(Encoding.UTF8.GetBytes(secretKey));
    }

    public EncryptedId Encrypt(long id)
    {
        var payload = BuildPayload(id);
        var cipher = AesEncrypt(payload);
        var token = Base64UrlEncode(cipher);

        // SEM PREFIXO "obf_" - igual ao Ailos
        return new EncryptedId(token);
    }

    public long Decrypt(EncryptedId encryptedId)
    {
        var cipher = Base64UrlDecode(encryptedId.Value);
        var payload = AesDecrypt(cipher);
        Console.WriteLine($"Payload (Base64): {Convert.ToBase64String(payload)}");
        
        // Validar payload e extrair ID
        if (!ValidatePayload(payload, out var id))
            throw new ArgumentException("Token inválido ou corrompido.");
        
        Console.WriteLine($"ID decifrado: {id}");
        Console.WriteLine($"=== FIM DECRYPT DEBUG ===");
        
        return id;
    }

    public bool TryDecrypt(string encryptedValue, out long id)
    {
        id = 0;
        
        try
        {
            var cipher = Base64UrlDecode(encryptedValue);
            var payload = AesDecrypt(cipher);
            
            if (!ValidatePayload(payload, out var decryptedId))
                return false;
                
            id = decryptedId;
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ------------------ Métodos Privados ------------------

    private byte[] BuildPayload(long id)
    {
        var buffer = new byte[32];

        // 1️⃣ ID (8 bytes) - IGUAL AO AILOS
        var idBytes = BitConverter.GetBytes(id);
        idBytes.CopyTo(buffer, 0);

        // 2️⃣ nonce determinístico (8 bytes)
        using var hmac = new HMACSHA256(_key);
        var nonce = hmac.ComputeHash(idBytes);
        Array.Copy(nonce, 0, buffer, 8, 8);

        // 3️⃣ assinatura (16 bytes)
        var signature = hmac.ComputeHash(buffer[..16]);
        Array.Copy(signature, 0, buffer, 16, 16);

        return buffer;
    }

    private void ValidatePayload(byte[] payload, out long id)
    {
        id = BitConverter.ToInt64(payload, 0);
        
        // Verificar assinatura - IGUAL AO AILOS
        using var hmac = new HMACSHA256(_key);
        
        // Recalcular assinatura dos primeiros 16 bytes
        var expectedSignature = hmac.ComputeHash(payload[..16]);
        
        // Comparar com os últimos 16 bytes
        for (int i = 0; i < 16; i++)
        {
            if (payload[16 + i] != expectedSignature[i])
                return false;
        }
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
        aes.Mode = CipherMode.ECB; // IGUAL AO AILOS
        aes.Padding = PaddingMode.None; // IGUAL AO AILOS

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