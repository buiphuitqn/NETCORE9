using CORE_BE.Models;
using Microsoft.AspNetCore.Identity;

namespace CORE_BE.Data;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }

    public ICollection<ApplicationUserRole>? UserRoles { get; set; }
    public ICollection<Menu_Role>? Menu_Roles { get; set; }
}
