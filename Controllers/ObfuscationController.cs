using Application.Interfaces.Crypto;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Obfuscation.Api.Controllers;

[ApiController]
[Route("api/obfuscation")]
public class ObfuscationController : ControllerBase
{
    private readonly IEncryptedIdService _service;

    public ObfuscationController(IEncryptedIdService service)
    {
        _service = service;
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
}
