using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Entities
{
    [Table("LeaderboardLevels")]
    public class LeaderboardLevel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = String.Empty;

        [Required]
        [StringLength(30)]
        public string Category { get; set; } = "Starter";

        public int? WorkshopItemId { get; set; } = null;
    }
}
