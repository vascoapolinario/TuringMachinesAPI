using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class MachineWorkshopItem : WorkshopItem
    {
        [Required]
        public int MachineId { get; set; }

        [Required]
        public string AlphabetJson { get; set; } = "[_]";

        [Required]
        public string NodesJson { get; set; } = "[]";

        [Required]
        public string ConnectionsJson { get; set; } = "[]";
    }
}
