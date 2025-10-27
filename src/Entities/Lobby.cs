using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TuringMachinesAPI.Entities
{
    [Table("Lobbies")]
    public class Lobby
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = "";

        public string? Password { get; set; }

        [Required]
        public int HostPlayerId { get; set; }

        [Required]
        public int SelectedLevelId { get; set; }

        [Required]
        public bool HasStarted { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public ICollection<int>? LobbyPlayers { get; set; }
    }
}