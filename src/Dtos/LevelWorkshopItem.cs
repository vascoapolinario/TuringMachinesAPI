using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class LevelWorkshopItem : WorkshopItem
    {
        [Required]
        public int LevelId { get; set; }

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
        public string AlphabetJson { get; set; } = "[_]";

        [Required]
        public string Mode { get; set; } = "accept";

        public string? TransformTestsJson { get; set; } = null;

        public string? CorrectExamplesJson { get; set; } = null;

        public string? WrongExamplesJson { get; set; } = null;
    }
}
