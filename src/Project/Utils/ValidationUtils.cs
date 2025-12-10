using System.Text.RegularExpressions;
using System.Text.Json;

namespace TuringMachinesAPI.Utils
{
    public static class ValidationUtils
    {
        /// <summary>
        /// Returns true if the string contains disallowed characters or spammy patterns
        /// (URLs, control chars, or anything outside letters, digits, spaces, underscores, or hyphens).
        /// </summary>
        public static bool ContainsDisallowedContent(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (Regex.IsMatch(input, @"https?://|www\.|\.com|\.net|\.org|\.io|\.gg|\.xyz|@", RegexOptions.IgnoreCase))
                return true;

            if (Regex.IsMatch(input, @"[^a-zA-Z0-9\s_\-.,!?()\[\]{}\<>""'\/\\]", RegexOptions.IgnoreCase))
                return true;

            if (input.Any(ch => char.IsControl(ch) && ch != '\n' && ch != '\r'))
                return true;

            var filter = new ProfanityFilter.ProfanityFilter();

            if (filter.ContainsProfanity(input))
            {
                var detected = filter.DetectAllProfanities(input, removePartialMatches: true);

                return detected.Count > 0;
            }

            return false;
        }

        /// <summary>
        /// Checks if the input is a valid JSON string.
        /// </summary>
        public static bool IsValidJson(string? str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;

            try
            {
                JsonDocument.Parse(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Censorses disallowed words in the input string.
        /// </summary>
        public static string CensorDisallowedWords(string input)
        {
            var filter = new ProfanityFilter.ProfanityFilter();
            if (!filter.ContainsProfanity(input))
                return input;

            return filter.CensorString(input);
        }
    }
}
