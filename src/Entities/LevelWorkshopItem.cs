using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.Entities
{
    [Table("LevelWorkshopItems")]
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
        [StringLength(150)]
        public string DetailedDescription { get; set; } = "";

        [Required]
        [StringLength(60)]
        public string Objective { get; set; } = "";

        [Required]
        public LevelMode Mode { get; set; } = LevelMode.accept;

        [Required]
        public string AlphabetJson { get; set; } = "[_]";


        public string? TransformTestsJson { get; set; } = null;
        public string? CorrectExamplesJson { get; set; } = null;
        public string? WrongExamplesJson { get; set; } = null;


    }
}
