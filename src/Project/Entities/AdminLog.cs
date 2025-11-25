using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.Entities
{
    [Table("AdminLogs")]
    public class AdminLog
    {
        [Key]
        public int Id { get; set; }

        public int? ActorId { get; set; } = null;

        [Required]
        public string ActorName { get; set; } = string.Empty;

        [Required]
        public string ActorRole { get; set; } = "User";

        [Required]
        public ActionType Action { get; set; }

        [Required]
        public TargetEntityType TargetEntityType { get; set; }

        public int? TargetEntityId { get; set; } = null;

        [Required]
        public string TargetEntityName { get; set; } = string.Empty;

        [Required]
        public DateTime DoneAtUtc { get; set; } = DateTime.UtcNow;
    }
}