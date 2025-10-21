using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class Machine : WorkshopItem
    {
        [Required]
        public int MachineId { get; set; }

        [Required]
        public string MachineData { get; set; } = "";
    }
}
