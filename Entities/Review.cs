using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int WorkshopItemId { get; set; }

        [Required]

        public int Rating { get; set; }

    }
}
