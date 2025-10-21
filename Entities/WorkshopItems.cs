using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("WorkshopItems")]
    public class WorkshopItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ItemTypeId { get; set; }

        [Required]
        public int AuthorId { get; set; }

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = "";

        [Required]
        public double Rating { get; set; } = 0.0;   

        public ICollection<int>? Subscribers { get; set; }
    }
}
