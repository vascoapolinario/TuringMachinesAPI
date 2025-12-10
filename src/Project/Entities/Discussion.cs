using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.Entities
{
    [Table("Discussions")]
    public class Discussion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string Title { get; set; } = string.Empty;

        [ForeignKey(nameof(Author))]
        public int? AuthorId { get; set; }

        public Player? Author { get; set; }

        [ForeignKey(nameof(InitialPost))]
        public int? InitialPostId { get; set; }

        public Post? InitialPost { get; set; }

        [ForeignKey(nameof(AnswerPost))]
        public int? AnswerPostId { get; set; }
        public Post? AnswerPost { get; set; }

        [Required]
        public DiscussionCategory Category { get; set; }

        public bool IsClosed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
