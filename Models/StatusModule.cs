using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CORE_BE.Models
{
    public class StatusModule : Auditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid ServerId { get; set; }
        public string ModuleName { get; set; }
        public float? ValueMonitor { get; set; }
        public string Status { get; set; }

        [JsonIgnore]
        [ForeignKey("ServerId")]
        public Server Server { get; set; }
    }
}