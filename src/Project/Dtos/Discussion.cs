using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class Discussion
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string AuthorName { get; set; } = "Deleted Author";

        [Required]
        public Post InitialPost { get; set; } = null!;

        public Post? AnswerPost { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public bool IsClosed { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public int PostCount { get; set; }
    }
}
