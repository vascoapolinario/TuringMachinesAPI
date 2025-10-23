using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class Review
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int WorkshopItemId { get; set; }

        [Required]
        public int Rating { get; set; }

    }
}
