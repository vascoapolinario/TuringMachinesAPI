using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("Players")]
    public class Player
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "User";

        [Required]
        public string? Password { get; set; }
    }
}
