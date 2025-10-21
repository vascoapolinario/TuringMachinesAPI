using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class Player
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = "";
        public string? Password { get; set; }
    }
}
