using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuringMachinesAPI.Entities
{
    [Table("Larps")]
    public class Larp
    {
        [Key]
        public int Id { get; set; }
        public string? Reason { get; set; }

        public required string NoteAuthor { get; set; }
    }
}
