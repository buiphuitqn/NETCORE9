using System.Security.Claims;
using CORE_BE.Data;
using CORE_BE.Infrastructure;
using CORE_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CORE_BE.Filters;

namespace CORE_BE.Controllers;

[Authorize]
[EnableCors("CorsApi")]
[Route("api/[controller]")]
[ApiController]
public class MenuController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<MenuController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public MenuController(IUnitOfWork uow, ILogger<MenuController> logger, UserManager<ApplicationUser> userManager)
    {
        _uow = uow;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);

        // Get all role IDs
        var allRoles = _uow.GetRepository<ApplicationRole>()
            .GetAll(x => !x.IsDeleted && roles.Contains(x.Name))
            .Select(x => x.Id)
            .ToList();

        // Get all menu_roles for this user
        var userMenuRoles = _uow.GetRepository<Menu_Role>()
            .GetAll(x => allRoles.Contains(x.Role_Id) && !x.IsDeleted)
            .ToList();

        // Get all menus, filter to only allowed ones + their parents
        var allMenus = _uow.GetRepository<Menu>()
            .GetAll(x => !x.IsDeleted, x => x.OrderBy(m => m.ThuTu), null)
            .ToList();

        var allowedMenuIds = userMenuRoles.Where(x => x.View).Select(x => x.Menu_Id).Distinct().ToList();

        // Include parent menus for hierarchy
        var resultMenuIds = new HashSet<Guid>(allowedMenuIds);
        foreach (var menuId in allowedMenuIds)
        {
            var menu = allMenus.FirstOrDefault(m => m.Id == menuId);
            while (menu?.Parent_Id != null)
            {
                resultMenuIds.Add(menu.Parent_Id.Value);
                menu = allMenus.FirstOrDefault(m => m.Id == menu.Parent_Id.Value);
            }
        }

        var filteredMenus = allMenus.Where(m => resultMenuIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                m.TenMenu,
                m.Url,
                m.Parent_Id,
                m.ThuTu,
                m.Icon,
                Permission = new
                {
                    view = userMenuRoles.Where(x => x.Menu_Id == m.Id).Any(x => x.View),
                    add = userMenuRoles.Where(x => x.Menu_Id == m.Id).Any(x => x.Add),
                    edit = userMenuRoles.Where(x => x.Menu_Id == m.Id).Any(x => x.Edit),
                    del = userMenuRoles.Where(x => x.Menu_Id == m.Id).Any(x => x.Del),
                    cof = userMenuRoles.Where(x => x.Menu_Id == m.Id).Any(x => x.Cof),
                    print = userMenuRoles.Where(x => x.Menu_Id == m.Id).Any(x => x.Print)
                }
            })
            .ToList();

        return Ok(filteredMenus);
    }

    [HttpGet("GetAll")]
    public IActionResult GetAll()
    {
        var listMenu = _uow.GetRepository<Menu>()
            .GetAll(x => !x.IsDeleted, x => x.OrderBy(m => m.ThuTu), null);
        return Ok(listMenu);
    }

    [HttpPost]
    [Permission("he-thong/chuc-nang", "add")]
    public IActionResult Post(Menu model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        model.CreatedDate = DateTime.UtcNow;
        model.CreatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        _uow.GetRepository<Menu>().Add(model);
        _uow.Complete();
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    [Permission("he-thong/chuc-nang", "edit")]
    public IActionResult Put(Menu model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = _uow.GetRepository<Menu>().GetById(model.Id);
        if (existing == null || existing.IsDeleted)
            return NotFound(new { message = "Không tìm thấy menu" });

        existing.TenMenu = model.TenMenu;
        existing.Url = model.Url;
        existing.Icon = model.Icon;
        existing.Parent_Id = model.Parent_Id;
        existing.ThuTu = model.ThuTu;
        existing.UpdatedDate = DateTime.UtcNow;
        existing.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        _uow.Complete();
        return Ok(new { message = "Cập nhật menu thành công" });
    }

    [HttpDelete]
    [Permission("he-thong/chuc-nang", "del")]
    public IActionResult Delete(Guid id)
    {
        var item = _uow.GetRepository<Menu>().GetById(id);
        if (item == null)
            return NotFound();
        item.IsDeleted = true;
        item.DeletedDate = DateTime.UtcNow;
        _uow.Complete();
        return StatusCode(StatusCodes.Status204NoContent);
    }
}