using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.Entities
{
    [Table("Reports")]
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReportingUserId { get; set; }

        [Required]
        public string ReportingUserName { get; set; } = string.Empty;

        [Required]
        public ReportType ReportedItemType { get; set; }

        [Required]
        public ReportStatus Status { get; set; } = ReportStatus.Open;

        [Required]
        public int ReportedUserId { get; set; }

        [Required]
        public int ReportedItemId { get; set; }

        [Required]
        public string ReportedUserName { get; set; } = string.Empty;

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
