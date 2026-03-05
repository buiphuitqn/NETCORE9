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
public class ServerController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _config;
    private readonly IUnitOfWork _uow;

    public ServerController(
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

    [HttpGet]
    public IActionResult Get(int page = 1, int pageSize = 20, string keyword = null)
    {
        if (keyword == null)
        {
            keyword = "";
        }
        else
        {
            keyword = keyword.ToLower();
        }
        int totalPage = 0,
            totalRow = 0;
        var list = _uow.GetRepository<Server>()
            .GetAllPaging(
                page,
                pageSize,
                out totalPage,
                out totalRow,
                x =>
                    x.TenServer.ToLower().Contains(keyword)
                    || x.MaServer.ToLower().Contains(keyword)
            );
        return Ok(
            new
            {
                data = list,
                totalPage,
                totalRow,
            }
        );
    }

    [HttpPost]
    public IActionResult Post(Server model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (_uow.GetRepository<Server>().Exists(x => x.MaServer == model.MaServer))
        {
            return BadRequest("Mã server đã tồn tại");
        }
        model.CreatedDate = DateTime.Now;
        model.CreatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        _uow.GetRepository<Server>().Add(model);
        _uow.Complete();
        return StatusCode(StatusCodes.Status201Created);
    }
}
