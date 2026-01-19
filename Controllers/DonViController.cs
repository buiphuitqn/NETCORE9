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
public class DonViController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _config;
    private readonly IUnitOfWork _uow;

    public DonViController(
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
        var list = _uow.GetRepository<DonVi>()
            .GetAllPaging(
                page,
                pageSize,
                out totalPage,
                out totalRow,
                x =>
                    !x.IsDeleted
                    && (
                        x.MaDonVi.ToLower().Contains(keyword)
                        || x.TenDonVi.ToLower().Contains(keyword)
                        || x.KhuVuc.ToLower().Contains(keyword)
                    )
            )
            .Select(x => new
            {
                x.Id,
                x.MaDonVi,
                x.TenDonVi,
                x.DiaChi,
                x.KhuVuc,
            })
            .Distinct()
            .OrderBy(x => x.MaDonVi)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        return Ok(
            new
            {
                list,
                totalPage,
                totalRow,
            }
        );
    }

    [HttpPost]
    public IActionResult Post(DonVi model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (
            _uow.GetRepository<DonVi>()
                .Exists(x =>
                    x.MaDonVi == model.MaDonVi && !x.IsDeleted && x.Parent_Id == model.Parent_Id
                )
        )
            return StatusCode(StatusCodes.Status409Conflict, "Mã đơn vị đã tồn tại");
        if (model.Parent_Id != null)
        {
            var parent = _uow.GetRepository<DonVi>().GetById(model.Parent_Id.Value);
            if (parent != null)
            {
                model.Level = parent.Level + 1;
            }
        }
        model.CreatedDate = DateTime.Now;
        model.CreatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        _uow.GetRepository<DonVi>().Add(model);
        _uow.Complete();
        return StatusCode(StatusCodes.Status201Created);
    }
}
