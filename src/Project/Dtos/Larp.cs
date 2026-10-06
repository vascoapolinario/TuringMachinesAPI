namespace TuringMachinesAPI.Dtos
{
    public class Larp
    {
        public int Id { get; set; }
        public string? Reason { get; set; }

        public required string NoteAuthor { get; set; }
    }
}
