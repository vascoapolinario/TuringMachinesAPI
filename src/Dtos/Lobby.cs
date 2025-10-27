using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TuringMachinesAPI.Dtos
{
    public class Lobby
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = "";

        public string? Password { get; set; }

        [Required]
        public string HostPlayer { get; set; }

        [Required]
        public string LevelName { get; set; }

        [Required]
        public bool HasStarted { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool PasswordProtected { get; set; } = false;

        [Required]
        public ICollection<string>? LobbyPlayers { get; set; }
    }
}