using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("Machines")]
    public class Machine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkshopItemId { get; set; }

        [Required]
        public string MachineData { get; set; } = "";
    }
}
