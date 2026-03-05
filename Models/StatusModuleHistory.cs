using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CORE_BE.Models
{
    public class StatusModuleHistory : Auditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid ServerId { get; set; }
        public string ModuleName { get; set; }
        public float? ValueMonitor { get; set; }
        public string Status { get; set; }
        public DateTime RecordedAt { get; set; } // thời điểm ghi nhận trạng thái

        [JsonIgnore]
        [ForeignKey("ServerId")]
        public Server Server { get; set; }
    }
}
