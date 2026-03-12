using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CORE_BE.Data;

public class ApplicationUserRole : IdentityUserRole<Guid>
{
    public ApplicationUser? User { get; set; }
    public ApplicationRole? Role { get; set; }
}
