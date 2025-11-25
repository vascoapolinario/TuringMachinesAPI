using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class LevelSubmission
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string LevelName { get; set; } = string.Empty;

        [Required]
        public string PlayerName { get; set; } = string.Empty;

        [Required]
        public double Time { get; set; } = 0;

        [Required]
        public int NodeCount { get; set; } = 0;

        [Required]
        public int ConnectionCount { get; set; } = 0;

    }
}
