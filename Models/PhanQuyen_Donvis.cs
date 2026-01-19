using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CORE_BE.Data;

namespace CORE_BE.Models
{
    public class PhanQuyen_DonVi
    {
        [ForeignKey("User")]
        public Guid User_Id { get; set; }
        public ApplicationUser User { get; set; }

        [ForeignKey("DonVi")]
        public Guid DonVi_Id { get; set; }
        public DonVi DonVi { get; set; }
        public bool IsFull { get; set; }
    }
}
