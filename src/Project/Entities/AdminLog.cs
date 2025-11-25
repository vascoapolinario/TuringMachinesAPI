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

        [Required]
        public int ActorId { get; set; }

        [Required]
        public ActionType Action { get; set; }

        [Required]
        public TargetEntityType TargetEntityType { get; set; }

        [Required]
        public int TargetEntityId { get; set; }

        [Required]
        public DateTime DoneAtUtc { get; set; } = DateTime.UtcNow;
    }
}