using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CORE_BE.Models
{
    public class IdracLog : Auditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid ServerId { get; set; }
        public string Serverity { get; set; }
        public string LogMessage { get; set; }

        public string ExternalLogId  { get; set; } // iDRAC log entry ID
        public DateTime Timestamp { get; set; }

        [JsonIgnore]
        [ForeignKey("ServerId")]
        public Server Server { get; set; }
    }
}