using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Discussion))]
        public int DiscussionId { get; set; }

        [Required]
        public Discussion Discussion { get; set; } = null!;

        [ForeignKey(nameof(Author))]
        public int? AuthorId { get; set; }

        public Player? Author { get; set; }

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public bool IsEdited { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
