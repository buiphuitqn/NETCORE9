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
public class IdracLogController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<IdracLogController> _logger;

    public IdracLogController(IUnitOfWork uow, ILogger<IdracLogController> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    [HttpGet("GetByServerId")]
    public IActionResult GetByServerId(Guid serverId)
    {
        var list = _uow.GetRepository<IdracLog>()
            .GetAll(x => x.ServerId == serverId && !x.IsDeleted, x => x.OrderByDescending(d => d.Timestamp), null);
        return Ok(list);
    }

    [HttpGet("GetNotOk")]
    public IActionResult GetNotOk(int pageSize = 200)
    {
        string[] includes = { "Server" };
        var list = _uow.GetRepository<IdracLog>()
            .GetAll(
                x => !x.IsDeleted && x.Serverity != null && x.Serverity.ToLower() != "ok",
                x => x.OrderByDescending(d => d.Timestamp),
                includes
            )
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Serverity,
                x.LogMessage,
                x.Timestamp,
                TenServer = x.Server != null ? x.Server.TenServer : ""
            })
            .ToList();
        return Ok(list);
    }
}