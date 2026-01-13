using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class Report
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string ReportingUserName { get; set; } = string.Empty;

        [Required]
        public string ReportedItemType { get; set; } = string.Empty;

        [Required]
        public string ReportedUserName { get; set; } = string.Empty;

        [Required]
        public int ReportedItemId { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
