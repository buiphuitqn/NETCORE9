using System.Security.Claims;
using CORE_BE.Infrastructure;
using CORE_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using CORE_BE.Filters;

namespace CORE_BE.Controllers;

[Authorize]
[EnableCors("CorsApi")]
[Route("api/[controller]")]
[ApiController]
public class ServerController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ServerController> _logger;

    public ServerController(IUnitOfWork uow, ILogger<ServerController> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    [HttpGet]
    [Permission("he-thong/may-chu", "view")]
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
        string[] includes = { "StatusModules" };
        var list = _uow.GetRepository<Server>()
            .GetAllPaging(
                page,
                pageSize,
                out totalPage,
                out totalRow,
                x =>
                    !x.IsDeleted
                    && (
                    x.TenServer.ToLower().Contains(keyword)
                    || x.MaServer.ToLower().Contains(keyword)),
                x => x.OrderBy(d => d.MaServer),
                includes
            ).Select(x => new
            {
                x.Id,
                x.MaServer,
                x.TenServer,
                x.DiaChiIP,
                x.Username,
                x.IDRACVersion,
                x.IsActive,
                Lst_Status = x.StatusModules.Where(sm => !sm.IsDeleted).Select(
                    sm => new
                    {
                        sm.ModuleName,
                        sm.Status,
                    }
                )
            });
        return Ok(
            new
            {
                data = list,
                totalPage,
                totalRow,
            }
        );
    }

    [HttpGet("GetByDonviId")]
    [Permission("he-thong/may-chu", "view")]
    public IActionResult GetByDonviId(Guid DonViId)
    {
        var item = _uow.GetRepository<Server>()
        .GetAll(x => x.DonVi_Id == DonViId && !x.IsDeleted, null, null);
        return Ok(item);
    }

    [HttpPost]
    [Permission("he-thong/may-chu", "add")]
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
        model.CreatedDate = DateTime.UtcNow;
        model.CreatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        _uow.GetRepository<Server>().Add(model);
        _uow.Complete();
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    [Permission("he-thong/may-chu", "edit")]
    public IActionResult Put(Server model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = _uow.GetRepository<Server>().GetById(model.Id);
        if (existing == null || existing.IsDeleted)
            return NotFound(new { message = "Không tìm thấy máy chủ" });

        if (_uow.GetRepository<Server>()
                .Exists(x => x.MaServer == model.MaServer && x.Id != model.Id && !x.IsDeleted))
        {
            return StatusCode(StatusCodes.Status409Conflict, "Mã server đã tồn tại");
        }

        existing.MaServer = model.MaServer;
        existing.TenServer = model.TenServer;
        existing.DiaChiIP = model.DiaChiIP;
        existing.Username = model.Username;
        existing.Password = model.Password;
        existing.IDRACVersion = model.IDRACVersion;
        existing.IsActive = model.IsActive;
        existing.DonVi_Id = model.DonVi_Id;
        existing.UpdatedDate = DateTime.UtcNow;
        existing.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        _uow.Complete();
        return Ok(new { message = "Cập nhật máy chủ thành công" });
    }

    [HttpDelete]
    [Permission("he-thong/may-chu", "del")]
    public IActionResult Delete(Guid id)
    {
        var item = _uow.GetRepository<Server>().GetById(id);
        if (item == null)
            return NotFound();
        item.IsDeleted = true;
        item.DeletedDate = DateTime.UtcNow;
        _uow.Complete();
        return StatusCode(StatusCodes.Status204NoContent);
    }
}
