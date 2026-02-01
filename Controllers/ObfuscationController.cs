using Application.Interfaces.Crypto;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Obfuscation.Api.Controllers;

[ApiController]
[Route("api/obfuscation")]
public class ObfuscationController : ControllerBase
{
    private readonly IEncryptedIdService _service;
    private readonly IConfiguration _configuration;

    public ObfuscationController(IEncryptedIdService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    [HttpGet("encrypt/{id:long}")]
    public ActionResult<EncryptedId> Encrypt(long id)
    {
        return Ok(_service.Encrypt(id));
    }

    [HttpGet("decrypt/{value}")]
    public ActionResult<long> Decrypt(string value)
    {
        return Ok(_service.Decrypt(new EncryptedId(value)));
    }

    // Endpoints de DEBUG para verificar compatibilidade
    [HttpGet("debug/encrypt/{id:long}")]
    public ActionResult DebugEncrypt(long id)
    {
        try
        {
            var encrypted = _service.Encrypt(id);
            var decrypted = _service.Decrypt(encrypted);
            
            var secret = _configuration["ENCRYPTED_ID_SECRET"];
            using var sha = System.Security.Cryptography.SHA256.Create();
            var keyHash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(secret ?? ""));
            
            return Ok(new
            {
                Success = true,
                InputId = id,
                EncryptedValue = encrypted.Value,
                DecryptedId = decrypted,
                Match = id == decrypted,
                SecretLength = secret?.Length ?? 0,
                KeyHash = Convert.ToBase64String(keyHash),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
    
    [HttpGet("debug/test/{id:long}")]
    public ActionResult TestCompatibility(long id)
    {
        // Este endpoint simula EXATAMENTE o que o Ailos faz
        var secret = _configuration["ENCRYPTED_ID_SECRET"];
        
        if (string.IsNullOrEmpty(secret))
            return BadRequest(new { Error = "Chave não configurada" });
            
        using var sha = System.Security.Cryptography.SHA256.Create();
        var key = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(secret));
        
        // 1. BuildPayload (igual ao Ailos)
        var buffer = new byte[32];
        var idBytes = BitConverter.GetBytes(id);
        idBytes.CopyTo(buffer, 0);
        
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        var nonce = hmac.ComputeHash(idBytes);
        Array.Copy(nonce, 0, buffer, 8, 8);
        
        var signature = hmac.ComputeHash(buffer, 0, 16);
        Array.Copy(signature, 0, buffer, 16, 16);
        
        // 2. AES Encrypt (igual ao Ailos)
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.Mode = System.Security.Cryptography.CipherMode.ECB;
        aes.Padding = System.Security.Cryptography.PaddingMode.None;
        
        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
        
        // 3. Base64Url Encode (igual ao Ailos)
        var token = Convert.ToBase64String(cipher)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
            
        return Ok(new
        {
            Algorithm = "AES-256-ECB",
            Padding = "None",
            PayloadSize = "32 bytes",
            InputId = id,
            GeneratedToken = token,
            ExpectedFromAilos = "T-vk29yF2Ct8k6zg67aBEUzHxVA3yLcyuJal2hWqXO8",
            Match = token == "T-vk29yF2Ct8k6zg67aBEUzHxVA3yLcyuJal2hWqXO8"
        });
    }
}
