using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class Level : WorkshopItem
    {
        [Required]
        public int LevelId { get; set; }

        [Required]
        [StringLength(30)]
        public string LevelType { get; set; } = "Workshop";

        [Required]
        public string LevelData { get; set; } = "";
    }
}
