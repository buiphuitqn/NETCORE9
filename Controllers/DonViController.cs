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
public class DonViController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DonViController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public DonViController(IUnitOfWork uow, ILogger<DonViController> logger, UserManager<ApplicationUser> userManager)
    {
        _uow = uow;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet]
    public ActionResult Get(int page = 1, int pageSize = 20, string keyword = null)
    {
        if (keyword == null)
        {
            keyword = "";
        }
        else
        {
            keyword = keyword.ToLower();
        }

        // Get current user's allowed DonVi
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var allowedDonViIds = _uow.GetRepository<PhanQuyen_DonVi>()
            .GetAll(x => x.User_Id == userId)
            .Select(x => x.DonVi_Id)
            .ToList();

        // If no permissions assigned, return empty
        if (allowedDonViIds.Count == 0)
        {
            return Ok(new { list = new List<object>(), totalPage = 0, totalRow = 0 });
        }

        // Include parent DonVi for hierarchy display
        var allDonVi = _uow.GetRepository<DonVi>()
            .GetAll(x => !x.IsDeleted)
            .ToList();
        var allowedSet = new HashSet<Guid>(allowedDonViIds);
        foreach (var dvId in allowedDonViIds)
        {
            var dv = allDonVi.FirstOrDefault(d => d.Id == dvId);
            while (dv?.Parent_Id != null)
            {
                allowedSet.Add(dv.Parent_Id.Value);
                dv = allDonVi.FirstOrDefault(d => d.Id == dv.Parent_Id.Value);
            }
        }

        int totalPage = 0,
            totalRow = 0;
        string[] includes = { "Servers", "Servers.StatusModules" };

        var list = _uow.GetRepository<DonVi>()
            .GetAllPaging(
                page,
                pageSize,
                out totalRow,
                out totalPage,
                x =>
                    !x.IsDeleted
                    && allowedSet.Contains(x.Id)
                    && (
                        x.MaDonVi.ToLower().Contains(keyword)
                        || x.TenDonVi.ToLower().Contains(keyword)
                        || x.KhuVuc.ToLower().Contains(keyword)
                    ),
                x => x.OrderBy(d => d.MaDonVi),
                includes
            )
            .Select(x => new
            {
                x.Id,
                x.MaDonVi,
                x.TenDonVi,
                x.DiaChi,
                x.KhuVuc,
                x.TenDayDu,
                x.LogoUrl,
                x.Parent_Id,
                x.Level,
                Lst_Servers = x.Servers != null && x.Servers.Count() > 0 ?
                    x.Servers.Where(s => !s.IsDeleted).Select(
                        s => new
                        {
                            s.Id,
                            s.MaServer,
                            s.TenServer,
                            s.DiaChiIP,
                            s.Username,
                            s.IDRACVersion,
                            s.IsActive,
                            Lst_Status = s.StatusModules.Where(sm => !sm.IsDeleted).Select(
                                    sm => new
                                    {
                                        sm.ModuleName,
                                        sm.Status,
                                        sm.ValueMonitor
                                    }
                                )
                        }
                    ) : []
            });
        return Ok(
            new
            {
                list,
                totalPage,
                totalRow,
            }
        );
    }

    [HttpGet("GetAll")]
    public ActionResult GetAll()
    {
        // Get current user's allowed DonVi
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var allowedDonViIds = _uow.GetRepository<PhanQuyen_DonVi>()
            .GetAll(x => x.User_Id == userId)
            .Select(x => x.DonVi_Id)
            .ToList();

        // If no permissions assigned, return empty list
        if (allowedDonViIds.Count == 0)
        {
            return Ok(new List<object>());
        }

        // Include parent DonVi for hierarchy display
        var allDonVi = _uow.GetRepository<DonVi>()
            .GetAll(x => !x.IsDeleted)
            .ToList();
        var allowedSet = new HashSet<Guid>(allowedDonViIds);
        foreach (var dvId in allowedDonViIds)
        {
            var dv = allDonVi.FirstOrDefault(d => d.Id == dvId);
            while (dv?.Parent_Id != null)
            {
                allowedSet.Add(dv.Parent_Id.Value);
                dv = allDonVi.FirstOrDefault(d => d.Id == dv.Parent_Id.Value);
            }
        }

        var list = _uow.GetRepository<DonVi>()
            .GetAll(
                x => !x.IsDeleted && allowedSet.Contains(x.Id),
                x => x.OrderBy(d => d.TenDonVi)
            )
            .Select(x => new
            {
                x.Id,
                x.MaDonVi,
                x.TenDonVi,
                x.Parent_Id,
                x.Level
            })
            .ToList();
        return Ok(list);
    }

    [HttpPost]
    [Permission("he-thong/don-vi", "add")]
    public IActionResult Post(DonVi model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        return CreateDonVi(model, null);
    }

    [HttpPost("ThemDonVi")]
    [Permission("he-thong/don-vi", "add")]
    public async Task<IActionResult> ThemDonVi([FromForm] DonVi model, IFormFile? file)
    {
        string imageUrl = "";
        if (file != null)
        {
            // Validate file extension
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return BadRequest(new { message = $"Chỉ chấp nhận file ảnh: {string.Join(", ", AllowedExtensions)}" });
            }

            // Validate file size
            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { message = "Dung lượng file tối đa 5MB" });
            }

            var folder = Path.Combine("uploads", "donvi");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + ext;
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            imageUrl = "/uploads/donvi/" + fileName;
        }

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        model.LogoUrl = imageUrl;
        return CreateDonVi(model, null);
    }

    /// <summary>
    /// Shared logic for creating a DonVi entity
    /// </summary>
    private IActionResult CreateDonVi(DonVi model, string logoUrl)
    {
        if (
            _uow.GetRepository<DonVi>()
                .Exists(x =>
                    x.MaDonVi == model.MaDonVi && !x.IsDeleted
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

        model.CreatedDate = DateTime.UtcNow;
        model.CreatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (!string.IsNullOrEmpty(logoUrl))
            model.LogoUrl = logoUrl;

        _uow.GetRepository<DonVi>().Add(model);
        _uow.Complete();
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpDelete]
    [Permission("he-thong/don-vi", "del")]
    public IActionResult Delete(Guid id)
    {
        var item = _uow.GetRepository<DonVi>().GetById(id);
        if (item == null)
            return NotFound();
        item.IsDeleted = true;
        item.DeletedDate = DateTime.UtcNow;
        _uow.Complete();
        return StatusCode(StatusCodes.Status204NoContent);
    }

    [HttpPut("SuaDonVi")]
    [Permission("he-thong/don-vi", "edit")]
    public async Task<IActionResult> SuaDonVi([FromForm] DonVi model, IFormFile? file)
    {
        var existing = _uow.GetRepository<DonVi>().GetById(model.Id);
        if (existing == null || existing.IsDeleted)
            return NotFound(new { message = "Không tìm thấy đơn vị" });

        // Check duplicate MaDonVi (exclude self)
        if (_uow.GetRepository<DonVi>()
                .Exists(x => x.MaDonVi == model.MaDonVi && x.Id != model.Id && !x.IsDeleted))
        {
            return StatusCode(StatusCodes.Status409Conflict, "Mã đơn vị đã tồn tại");
        }

        // Handle logo file upload
        if (file != null)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return BadRequest(new { message = $"Chỉ chấp nhận file ảnh: {string.Join(", ", AllowedExtensions)}" });
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { message = "Dung lượng file tối đa 5MB" });
            }

            // Delete old logo file if exists
            if (!string.IsNullOrEmpty(existing.LogoUrl))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), existing.LogoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            var folder = Path.Combine("uploads", "donvi");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + ext;
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            existing.LogoUrl = "/uploads/donvi/" + fileName;
        }

        // Update fields
        existing.MaDonVi = model.MaDonVi;
        existing.TenDonVi = model.TenDonVi;
        existing.DiaChi = model.DiaChi;
        existing.DienThoai = model.DienThoai;
        existing.SoFax = model.SoFax;
        existing.MaNhaSanXuat = model.MaNhaSanXuat;
        existing.QuocGia = model.QuocGia;
        existing.MaNoiSanXuat = model.MaNoiSanXuat;
        existing.TenDayDu = model.TenDayDu;
        existing.KhuVuc = model.KhuVuc;
        existing.ThuTu = model.ThuTu;
        existing.Parent_Id = model.Parent_Id;

        // Recalculate Level if Parent_Id changed
        if (model.Parent_Id != null)
        {
            var parent = _uow.GetRepository<DonVi>().GetById(model.Parent_Id.Value);
            if (parent != null)
            {
                existing.Level = parent.Level + 1;
            }
        }
        else
        {
            existing.Level = 0;
        }

        existing.UpdatedDate = DateTime.UtcNow;
        existing.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        _uow.Complete();
        return Ok(new { message = "Cập nhật đơn vị thành công" });
    }
}
