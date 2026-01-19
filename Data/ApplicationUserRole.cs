using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CORE_BE.Data;

public class ApplicationUserRole : IdentityUserRole<Guid>
{
    [ForeignKey("User")]
    public Guid User_Id { get; set; }
    public ApplicationUser? User { get; set; }

    [ForeignKey("Role")]
    public Guid Role_Id { get; set; }
    public ApplicationRole? Role { get; set; }
}
