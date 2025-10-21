using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class Level
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = "Workshop";

        [Required]
        public string LevelData { get; set; } = "";
    }
}
