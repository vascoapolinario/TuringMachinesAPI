using System.ComponentModel.DataAnnotations;

namespace TuringMachinesAPI.Dtos
{
    public class IncomingReport
    {
        [Required]
        public string ReportType { get; set; } = string.Empty;

        [Required]
        public string ReportedPlayerName { get; set; } = string.Empty;

        public int? ReportedItemId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

    }
}
