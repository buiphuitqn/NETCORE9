using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CORE_BE.Data;
using CORE_BE.Infrastructure;
using CORE_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[EnableCors("CorsApi")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _config;
    private readonly IUnitOfWork _uow;

    public AccountController(
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

    [HttpPost]
    public async Task<IActionResult> Post(RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var exit = await _userManager.FindByNameAsync(model.UserName);
        var exit_email = string.IsNullOrEmpty(model.Email)
            ? null
            : await _userManager.FindByEmailAsync(model.Email);
        // Kiểm tra tài khoản, email có tồn tại không
        //Nếu tài khoản không tồn tại -- Thêm mới
        var Password = _config.GetValue<string>("DefaultPass");
        if (exit == null && exit_email == null)
        {
            var user = new ApplicationUser()
            {
                UserName = model.UserName,
                FullName = model.FullName,
                Email = model.Email,
                IsActive = model.IsActive,
                MustChangePass = true,
                CreatedDate = DateTime.Now,
                GhiChu = model.GhiChu,
            };
            IdentityResult result = await _userManager.CreateAsync(user, Password);
            if (result.Succeeded)
            {
                foreach (string RoleName in model.RoleNames)
                {
                    await _userManager.AddToRoleAsync(user, RoleName);
                }
                return StatusCode(StatusCodes.Status201Created);
            }
            return BadRequest(string.Join(",", result.Errors));
        }
        else
        {
            if (
                exit != null
                && exit.IsDeleted
                && (exit_email == null || (exit_email != null && exit.Id == exit_email.Id))
            )
            {
                exit.Id = Guid.NewGuid();
                exit.UpdatedDate = DateTime.Now;
                exit.GhiChu = model.GhiChu;
                exit.Email = model.Email;
                exit.DeletedDate = null;
                exit.IsDeleted = false;
                exit.IsActive = model.IsActive;
                exit.MustChangePass = true;
                exit.PasswordHash = _userManager.PasswordHasher.HashPassword(exit, Password);
                var result = await _userManager.UpdateAsync(exit);
                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(exit);
                    foreach (string item_remove in roles)
                    {
                        await _userManager.RemoveFromRoleAsync(exit, item_remove);
                    }
                    foreach (string RoleName in model.RoleNames)
                    {
                        await _userManager.AddToRoleAsync(exit, RoleName);
                    }
                    if (model.ListDonVi != null)
                    {
                        foreach (var it in model.ListDonVi)
                        {
                            var item = new PhanQuyen_DonVi();
                            item.DonVi_Id = it;
                            item.User_Id = exit.Id;
                            _uow.GetRepository<PhanQuyen_DonVi>().Add(item);
                        }
                        _uow.Complete();
                    }
                    return StatusCode(StatusCodes.Status204NoContent);
                }
                return BadRequest(string.Join(",", result.Errors));
            }
            return StatusCode(
                StatusCodes.Status409Conflict,
                exit != null ? "Thông tin Tài khoản đã tồn tại" : "Thông tin Email đã tồn tại"
            );
        }
    }

    [HttpPost("ChangePassword")]
    public async Task<ActionResult> ChangePassword(ChangePasswordModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var appUser = await _userManager.FindByIdAsync(User.Identity.Name);
        appUser.MustChangePass = false;
        appUser.UpdatedDate = DateTime.Now;
        var result = await _userManager.ChangePasswordAsync(
            appUser,
            model.Password,
            model.NewPassword
        );
        if (result.Succeeded)
        {
            return StatusCode(StatusCodes.Status200OK, "Đổi mật khẩu thành công");
        }
        return BadRequest("Mật khẩu hiện tại không đúng");
    }
}
