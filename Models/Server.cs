using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CORE_BE.Models
{
    public class Server : Auditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [StringLength(50)]
        [Required(ErrorMessage = "Mã bắt buộc")]
        public string MaServer { get; set; }

        [StringLength(250)]
        [Required(ErrorMessage = "Tên bắt buộc")]
        public string TenServer { get; set; }
        public string? DiaChiIP { get; set; }
        public string Username { get; set; } // iDRAC user
        public string Password { get; set; } // nên mã hoá

        public string IDRACVersion { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid? DonVi_Id { get; set; }
        [ForeignKey("DonVi_Id")]
        public DonVi? DonVi { get; set; }
        [JsonIgnore]
        public List<StatusModule>? StatusModules { get; set; }
        [JsonIgnore]
        public ICollection<IdracLog>? IdracLog { get; set; }
    }
}
