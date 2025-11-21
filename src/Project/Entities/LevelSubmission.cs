using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("LevelSubmissions")]
    public class LevelSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlayerId { get; set; }

        [Required]
        public int LeaderboardLevelId { get; set; }

        [Required]
        public double Time {  get; set; } = 0;

        [Required]
        public int NodeCount { get; set; } = 0;

        [Required]
        public int ConnectionCount { get; set; } = 0;

    }
}
