using CORE_BE.Infrastructure;
using CORE_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CORE_BE.Controllers;

[Authorize]
[EnableCors("CorsApi")]
[Route("api/[controller]")]
[ApiController]
public class InfoServerController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InfoServerController> _logger;

    public InfoServerController(IUnitOfWork uow, ILogger<InfoServerController> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    [HttpGet("GetInfoByServerId")]
    public IActionResult GetInfoByServerId(Guid serverId)
    {
        var list = _uow.GetRepository<InfoServer>()
            .GetAll(x => x.ServerId == serverId && !x.IsDeleted, null, null);
        return Ok(list);
    }
}