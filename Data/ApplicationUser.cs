using CORE_BE.Models;
using Microsoft.AspNetCore.Identity;

namespace CORE_BE.Data;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public string? GhiChu { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePass { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }

    public ICollection<ApplicationUserRole>? UserRoles { get; set; }
    public ICollection<PhanQuyen_DonVi>? PhanQuyen_DonVis { get; set; }
}
