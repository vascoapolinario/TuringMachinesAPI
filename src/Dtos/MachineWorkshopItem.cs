using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class MachineWorkshopItem : WorkshopItem
    {
        [Required]
        public int MachineId { get; set; }

        [Required]
        public string MachineData { get; set; } = "";
    }
}
