using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Dtos;

namespace TuringMachinesAPI.Services
{
    public class LarpService
    {
        private readonly TuringMachinesDbContext dbContext;
        private readonly IConfiguration configuration;

        public LarpService(TuringMachinesDbContext _dbContext, IConfiguration _configuration)
        {
            dbContext = _dbContext;
            configuration = _configuration;
        }

        public  Dtos.Larp? CreateLarp(string reason, int authorNumber)
        {
            var larp = new Entities.Larp
            {
                Reason = reason,
                NoteAuthor = configuration.GetValue<string>($"AuthorNames:{authorNumber}") ?? "Unknown"
            };
            if (larp.NoteAuthor == "Unknown")
            {
                return null;
            }
            else
            {
                dbContext.Larps.Add(larp);
                dbContext.SaveChanges();
                return new Dtos.Larp
                {
                    Id = larp.Id,
                    Reason = larp.Reason,
                    NoteAuthor = larp.NoteAuthor
                };
            }
        }

        public int GetLarpCount()
        {
            return dbContext.Larps.Count();
        }

        public IEnumerable<Dtos.Larp> GetAllLarps()
        {
            return dbContext.Larps.Select(l => new Dtos.Larp
            {
                Id = l.Id,
                Reason = l.Reason,
                NoteAuthor = l.NoteAuthor
            }).ToList();
        }
    }
}
