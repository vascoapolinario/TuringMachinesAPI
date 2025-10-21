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
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        public List<string> Alphabet = new List<string>();

        [Required]
        public string MachineData { get; set; } = "";
    }
}
