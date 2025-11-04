using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class WorkshopItem
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
        public string Author { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = "Undefined";

        [Required]
        public double Rating { get; set; } = 0.0;

        [Required]
        public int UserRating { get; set; } = 0;

        [Required]
        public bool UserIsSubscribed { get; set; } = false;

        public int Subscribers { get; set; }
    }
}
