using Microsoft.Extensions.Caching.Memory;

namespace TuringMachinesAPI.Utils
{
    public static class CacheHelper
    {

        public static Dtos.Player? GetPlayerFromCacheById(IMemoryCache cache, int playerId)
        {
            if (cache.TryGetValue($"Players", out IEnumerable<Dtos.Player>? cachedPlayers))
            {
                return cachedPlayers?.FirstOrDefault(p => p.Id == playerId);
            }
            return null;
        }

        public static Dtos.Player? GetPlayerFromCacheByUsername(IMemoryCache cache, string username)
        {
            if (cache.TryGetValue($"Players", out IEnumerable<Dtos.Player>? cachedPlayers))
            {
                return cachedPlayers?.FirstOrDefault(p => p.Username == username);
            }
            return null;
        }

        public static Dtos.LevelSubmission? GetLevelSubmissionFromCacheById(IMemoryCache cache, int submissionId)
        {
            if (cache.TryGetValue($"LevelSubmissions", out IEnumerable<Dtos.LevelSubmission>? cachedSubmissions))
            {
                return cachedSubmissions?.FirstOrDefault(s => s.Id == submissionId);
            }
            return null;
        }

        public static object? GetWorkshopItemFromCacheById(IMemoryCache cache, int itemId)
        {
            if (cache.TryGetValue("WorkshopItems", out IEnumerable<object>? cachedItems))
            {
                return cachedItems!.Where(x =>
                {
                    if (x is Dtos.WorkshopItem w) return w.Id == itemId;
                    if (x is Dtos.LevelWorkshopItem lw) return lw.Id == itemId;
                    if (x is Dtos.MachineWorkshopItem mw) return mw.Id == itemId;
                    return false;
                }).FirstOrDefault();
            }
            return null;
        }
    }
}
