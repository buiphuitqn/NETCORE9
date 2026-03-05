using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CORE_BE.Data;

namespace CORE_BE.Models
{
    public class Auditable
    {
        public DateTime CreatedDate { get; set; }

        [ForeignKey("NguoiTao")]
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
