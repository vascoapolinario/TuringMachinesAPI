using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("MachineWorkshopItems")]
    public class MachineWorkshopItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkshopItemId { get; set; }

        [Required]
        public string AlphabetJson { get; set; } = "[_]";

        [Required]

        public string NodesJson { get; set; } = "[]";

        [Required]

        public string ConnectionsJson { get; set; } = "[]";
    }
}
