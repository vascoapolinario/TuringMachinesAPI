using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class Post
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string AuthorName { get; set; } = "Deleted Author";

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public bool IsEdited { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; }

        [Required]
        public int LikeCount { get; set; } = 0;

        [Required]
        public int DislikeCount { get; set; } = 0;
    }
}
