using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("PostVotes")]
    public class PostVote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Post))]
        public int PostId { get; set; }

        [Required]
        public Post Post { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Player))]
        public int PlayerId { get; set; }

        [Required]
        public Player Player { get; set; } = null!;

        [Required]
        public int Vote { get; set; }
    }
}
