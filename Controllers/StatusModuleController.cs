using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CORE_BE;
using CORE_BE.Data;
using CORE_BE.Infrastructure;
using CORE_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CORE_BE.Controllers;

[Authorize]
[EnableCors("CorsApi")]
[Route("api/[controller]")]
[ApiController]

public class StatusModuleController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _config;
    private readonly IUnitOfWork _uow;

    public StatusModuleController(
        UserManager<ApplicationUser> userMgr,
        RoleManager<ApplicationRole> roleMgr,
        IConfiguration config,
        IUnitOfWork uow
    )
    {
        _userManager = userMgr;
        _roleManager = roleMgr;
        _config = config;
        _uow = uow;
    }

    [HttpGet("GetbyServerId")]
    public IActionResult GetbyServerId(Guid serverId)
    {
        var list = _uow.GetRepository<StatusModule>()
            .GetAll(x => x.ServerId == serverId&& !x.IsDeleted, null, null);
        return Ok(list);
    }
}