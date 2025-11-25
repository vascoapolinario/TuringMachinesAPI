using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class AdminLog
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ActorName { get; set; } = String.Empty;

        [Required]
        [StringLength(50)]
        public string ActorRole { get; set; } = "User";

        [Required]
        public string Action { get; set; } = String.Empty;

        [Required]
        public string TargetEntityType { get; set; } = String.Empty;

        public int? TargetEntityId { get; set; } = null;

        [Required]
        public string TargetEntityName { get; set; } = String.Empty;

        [Required]

        public DateTime DoneAt { get; set; } = DateTime.UtcNow;
    }
}
