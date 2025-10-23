using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Entities
{
    [Table("Levels")]
    public class LevelWorkshopItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkshopItemId { get; set; }

        [Required]
        [StringLength(30)]
        public string LevelType { get; set; } = "Workshop";

        [Required]
        public string LevelData { get; set; } = "";

    }
}
