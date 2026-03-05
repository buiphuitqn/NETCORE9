using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CORE_BE.Models
{
    public class InfoServer : Auditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid ServerId { get; set; }
        public string Manufacturer { get; set; }
        public string SystemMode { get; set; }
        public string ServiceTag { get; set; }
        public string BiosVersion { get; set; }
        public string HostName { get; set; }
        public string OperatingSystem { get; set; }
        public float MemorySize { get; set; }
        public string CpuModel { get; set; }

        [JsonIgnore]
        [ForeignKey("ServerId")]
        public Server Server { get; set; }
    }
}
