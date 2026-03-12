using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CORE_BE.Models
{
    public class DonVi : Auditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [StringLength(50)]
        [Required(ErrorMessage = "Mã bắt buộc")]
        public string MaDonVi { get; set; }

        [StringLength(250)]
        [Required(ErrorMessage = "Tên bắt buộc")]
        public string TenDonVi { get; set; }
        public Guid? Parent_Id { get; set; }
        public int ThuTu { get; set; }
        public int Level { get; set; }
        public string? DiaChi { get; set; }
        public string? DienThoai { get; set; }
        public string? SoFax { get; set; }
        public string? MaNhaSanXuat { get; set; }
        public string? QuocGia { get; set; }
        public string? MaNoiSanXuat { get; set; }
        public string? TenDayDu { get; set; }
        public string? LogoUrl { get; set; }
        public string? KhuVuc { get; set; }

        [JsonIgnore]
        public virtual ICollection<PhanQuyen_DonVi>? PhanQuyen_DonVis { get; set; }
        [JsonIgnore]
        public List<Server>? Servers { get; set; }

        // [NotMapped]
        // public List<DonVi>? children { get; set; }
    }
}
