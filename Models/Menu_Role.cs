using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CORE_BE.Data;

namespace CORE_BE.Models
{
    public class Menu_Role : Auditable
    {
        [ForeignKey("Menu")]
        public Guid Menu_Id { get; set; }
        public Menu Menu { get; set; }

        [ForeignKey("Role")]
        public Guid Role_Id { get; set; }
        public ApplicationRole Role { get; set; }
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Del { get; set; }
        public bool Cof { get; set; }
        public bool Print { get; set; }
    }
}
