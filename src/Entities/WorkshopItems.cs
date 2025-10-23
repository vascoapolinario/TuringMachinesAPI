using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.Entities
{
    [Table("WorkshopItems")]
    public class WorkshopItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = "";

        [Required]
        public int AuthorId { get; set; }

        [Required]
        [StringLength(30)]
        public WorkshopItemType Type { get; set; } = WorkshopItemType.Undefined;

        [Required]
        public double Rating { get; set; } = 0.0;   

        public ICollection<int>? Subscribers { get; set; }
    }
}
