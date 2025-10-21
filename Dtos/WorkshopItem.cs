using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class WorkshopItem
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ItemTypeId { get; set; }

        [Required]
        public Player Author { get; set; }

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = "";

        [Required]
        public double Rating { get; set; } = 0.0;

        public IEnumerable<Player>? Subscribers { get; set; }
    }
}
