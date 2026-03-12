using CORE_BE.Data;
using CORE_BE.Infrastructure;
using CORE_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CORE_BE.Filters;

namespace CORE_BE.Controllers;

[EnableCors("CorsApi")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userMgr,
        IConfiguration config,
        IUnitOfWork uow,
        ILogger<AccountController> logger
    )
    {
        _userManager = userMgr;
        _config = config;
        _uow = uow;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get(string keyword = "")
    {
        keyword = (keyword ?? "").ToLower();
        var users = _userManager.Users
            .Where(u => !u.IsDeleted &&
                (u.UserName.ToLower().Contains(keyword) ||
                 u.FullName.ToLower().Contains(keyword) ||
                 (u.Email != null && u.Email.ToLower().Contains(keyword))))
            .OrderBy(u => u.UserName)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.FullName,
                u.Email,
                u.IsActive,
                u.GhiChu,
                u.AvatarUrl,
                u.CreatedDate,
            })
            .ToList();

        // Lấy roles cho mỗi user
        var result = users.Select(u =>
        {
            var appUser = _userManager.FindByIdAsync(u.Id.ToString()).Result;
            var roles = _userManager.GetRolesAsync(appUser).Result;
            return new
            {
                u.Id,
                u.UserName,
                u.FullName,
                u.Email,
                u.IsActive,
                u.GhiChu,
                u.AvatarUrl,
                u.CreatedDate,
                Roles = roles
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("GetRoles")]
    public IActionResult GetRoles()
    {
        var roles = _uow.GetRepository<ApplicationRole>()
            .GetAll(x => !x.IsDeleted)
            .Select(x => new { x.Id, x.Name, x.Description })
            .ToList();
        return Ok(roles);
    }

    [HttpPost("CreateRole")]
    [Permission("he-thong/vai-tro", "add")]
    public async Task<IActionResult> CreateRole([FromBody] RoleModel model)
    {
        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<ApplicationRole>>();
        if (await roleManager.RoleExistsAsync(model.Name))
            return StatusCode(StatusCodes.Status409Conflict, "Vai trò đã tồn tại");

        var role = new ApplicationRole
        {
            Name = model.Name,
            Description = model.Description,
            CreatedDate = DateTime.UtcNow,
        };
        var result = await roleManager.CreateAsync(role);
        if (result.Succeeded)
            return StatusCode(StatusCodes.Status201Created);
        return BadRequest(string.Join(",", result.Errors.Select(e => e.Description)));
    }

    [HttpPut("UpdateRole")]
    [Permission("he-thong/vai-tro", "edit")]
    public async Task<IActionResult> UpdateRole([FromBody] RoleModel model)
    {
        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = await roleManager.FindByIdAsync(model.Id);
        if (role == null || role.IsDeleted)
            return NotFound(new { message = "Không tìm thấy vai trò" });

        role.Name = model.Name;
        role.Description = model.Description;
        role.UpdatedDate = DateTime.UtcNow;
        var result = await roleManager.UpdateAsync(role);
        if (result.Succeeded)
            return Ok(new { message = "Cập nhật thành công" });
        return BadRequest(string.Join(",", result.Errors.Select(e => e.Description)));
    }

    [HttpDelete("DeleteRole")]
    [Permission("he-thong/vai-tro", "del")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound();
        role.IsDeleted = true;
        role.DeletedDate = DateTime.UtcNow;
        await roleManager.UpdateAsync(role);
        return StatusCode(StatusCodes.Status204NoContent);
    }

    [HttpPost("SeedAdmin")]
    public async Task<IActionResult> SeedAdmin([FromBody] SeedAdminModel model)
    {
        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<ApplicationRole>>();

        // Create Administrator role if not exists
        if (!await roleManager.RoleExistsAsync("Administrator"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Administrator",
                Description = "Quản trị hệ thống",
                CreatedDate = DateTime.UtcNow,
            });
        }

        // Assign to user
        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user == null)
            return NotFound(new { message = $"Không tìm thấy tài khoản {model.UserName}" });

        if (!await _userManager.IsInRoleAsync(user, "Administrator"))
        {
            await _userManager.AddToRoleAsync(user, "Administrator");
        }

        return Ok(new { message = $"Đã gán quyền Administrator cho {model.UserName}" });
    }

    [HttpGet("GetMenuPermissions")]
    public IActionResult GetMenuPermissions(Guid roleId)
    {
        var permissions = _uow.GetRepository<Menu_Role>()
            .GetAll(x => x.Role_Id == roleId && !x.IsDeleted, null, null)
            .Select(x => new
            {
                x.Menu_Id,
                x.Role_Id,
                x.View,
                x.Add,
                x.Edit,
                x.Del,
                x.Cof,
                x.Print,
            })
            .ToList();
        return Ok(permissions);
    }

    [HttpPost("SaveMenuPermissions")]
    [Permission("he-thong/vai-tro", "edit")]
    public IActionResult SaveMenuPermissions([FromBody] SaveMenuPermissionsModel model)
    {
        var existing = _uow.GetRepository<Menu_Role>()
            .GetAll(x => x.Role_Id == model.RoleId, null, null)
            .ToList();
        if (existing.Any())
        {
            _uow.GetRepository<Menu_Role>().RemoveRange(existing);
        }
        _uow.Complete();

        if (model.Permissions != null)
        {
            foreach (var perm in model.Permissions)
            {
                var menuRole = new Menu_Role
                {
                    Menu_Id = perm.MenuId,
                    Role_Id = model.RoleId,
                    View = perm.View,
                    Add = perm.Add,
                    Edit = perm.Edit,
                    Del = perm.Del,
                    Cof = perm.Cof,
                    Print = perm.Print,
                };
                _uow.GetRepository<Menu_Role>().Add(menuRole);
            }
            _uow.Complete();
        }

        return Ok(new { message = "Lưu phân quyền thành công" });
    }

    [HttpGet("GetDonViPermissions")]
    public IActionResult GetDonViPermissions(Guid userId)
    {
        var permissions = _uow.GetRepository<PhanQuyen_DonVi>()
            .GetAll(x => x.User_Id == userId)
            .Select(x => new { x.DonVi_Id, x.IsFull })
            .ToList();
        return Ok(permissions);
    }

    [HttpPost("SaveDonViPermissions")]
    [Permission("he-thong/nguoi-dung", "edit")]
    public IActionResult SaveDonViPermissions([FromBody] SaveDonViPermissionsModel model)
    {
        var existing = _uow.GetRepository<PhanQuyen_DonVi>()
            .GetAll(x => x.User_Id == model.UserId)
            .ToList();
        if (existing.Any())
        {
            _uow.GetRepository<PhanQuyen_DonVi>().RemoveRange(existing);
        }
        _uow.Complete();

        if (model.DonViIds != null)
        {
            foreach (var dvId in model.DonViIds)
            {
                _uow.GetRepository<PhanQuyen_DonVi>().Add(new PhanQuyen_DonVi
                {
                    User_Id = model.UserId,
                    DonVi_Id = dvId,
                    IsFull = true,
                });
            }
            _uow.Complete();
        }

        return Ok(new { message = "Lưu phân quyền đơn vị thành công" });
    }

    [HttpPut]
    [Permission("he-thong/nguoi-dung", "edit")]
    public async Task<IActionResult> Put(UserInfoModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null || user.IsDeleted)
            return NotFound(new { message = "Không tìm thấy tài khoản" });

        // Check duplicate email
        if (!string.IsNullOrEmpty(model.Email))
        {
            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null && existingEmail.Id != user.Id)
                return StatusCode(StatusCodes.Status409Conflict, "Email đã tồn tại");
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.IsActive = model.IsActive;
        user.GhiChu = model.GhiChu;
        user.UpdatedDate = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(string.Join(",", result.Errors.Select(e => e.Description)));

        // Update roles
        if (model.RoleNames != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            foreach (var role in model.RoleNames)
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        return Ok(new { message = "Cập nhật thành công" });
    }

    [HttpDelete]
    [Permission("he-thong/nguoi-dung", "del")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        user.IsDeleted = true;
        user.DeletedDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return StatusCode(StatusCodes.Status204NoContent);
    }

    [HttpPost]
    [Permission("he-thong/nguoi-dung", "add")]
    public async Task<IActionResult> Post(RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var existingUser = await _userManager.FindByNameAsync(model.UserName);
        var existingEmail = string.IsNullOrEmpty(model.Email)
            ? null
            : await _userManager.FindByEmailAsync(model.Email);

        // Kiểm tra tài khoản, email có tồn tại không
        // Nếu tài khoản không tồn tại -- Thêm mới
        var password = _config.GetValue<string>("DefaultPass");
        if (existingUser == null && existingEmail == null)
        {
            var user = new ApplicationUser()
            {
                UserName = model.UserName,
                FullName = model.FullName,
                Email = model.Email,
                IsActive = model.IsActive,
                MustChangePass = true,
                CreatedDate = DateTime.UtcNow,
                GhiChu = model.GhiChu,
            };
            IdentityResult result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                foreach (string roleName in model.RoleNames)
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
                return StatusCode(StatusCodes.Status201Created);
            }
            return BadRequest(string.Join(",", result.Errors));
        }
        else
        {
            if (
                existingUser != null
                && existingUser.IsDeleted
                && (existingEmail == null || (existingEmail != null && existingUser.Id == existingEmail.Id))
            )
            {
                existingUser.Id = Guid.NewGuid();
                existingUser.UpdatedDate = DateTime.UtcNow;
                existingUser.GhiChu = model.GhiChu;
                existingUser.Email = model.Email;
                existingUser.DeletedDate = null;
                existingUser.IsDeleted = false;
                existingUser.IsActive = model.IsActive;
                existingUser.MustChangePass = true;
                existingUser.PasswordHash = _userManager.PasswordHasher.HashPassword(existingUser, password);
                var result = await _userManager.UpdateAsync(existingUser);
                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(existingUser);
                    foreach (string itemRemove in roles)
                    {
                        await _userManager.RemoveFromRoleAsync(existingUser, itemRemove);
                    }
                    foreach (string roleName in model.RoleNames)
                    {
                        await _userManager.AddToRoleAsync(existingUser, roleName);
                    }
                    if (model.ListDonVi != null)
                    {
                        foreach (var it in model.ListDonVi)
                        {
                            var item = new PhanQuyen_DonVi();
                            item.DonVi_Id = it;
                            item.User_Id = existingUser.Id;
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
                existingUser != null ? "Thông tin Tài khoản đã tồn tại" : "Thông tin Email đã tồn tại"
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
        var appUser = await _userManager.FindByIdAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        if (appUser == null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản" });
        }
        appUser.MustChangePass = false;
        appUser.UpdatedDate = DateTime.UtcNow;
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

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private const long MaxFileSize = 2 * 1024 * 1024; // 2MB

    [HttpPost("ChangeAvatar")]
    public async Task<IActionResult> ChangeAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn file ảnh" });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = "File ảnh không được vượt quá 2MB" });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng: jpg, jpeg, png, gif, webp" });
        }

        var appUser = await _userManager.FindByIdAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        if (appUser == null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản" });
        }

        // Tạo thư mục uploads/avatars nếu chưa có
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        // Xóa avatar cũ nếu có
        if (!string.IsNullOrEmpty(appUser.AvatarUrl))
        {
            var oldFilePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                appUser.AvatarUrl.TrimStart('/')
            );
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }
        }

        // Lưu file mới: {userId}_{timestamp}{ext}
        var fileName = $"{appUser.Id}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Cập nhật AvatarUrl trong database
        var avatarUrl = $"uploads/avatars/{fileName}";
        appUser.AvatarUrl = avatarUrl;
        appUser.UpdatedDate = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(appUser);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} changed avatar successfully", appUser.Id);
            return Ok(new { avatarUrl = $"/{avatarUrl}" });
        }

        return BadRequest(new { message = "Cập nhật avatar thất bại" });
    }
}
