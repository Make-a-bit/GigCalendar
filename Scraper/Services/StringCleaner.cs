namespace Scraper.Services
{
    public class StringCleaner : ICleaner
    {
        /// <summary>
        /// Cleans a string by replacing HTML entities with their corresponding characters.
        /// </summary>
        /// <param name="input">The input string to clean.</param>
        /// <returns>The cleaned string.</returns>
        public string Cleaner(string input)
        {
            var cleanedString = input
            .Replace("&euro;", "€")
            .Replace("&#8364;", "€")
            .Replace("&#x20AC;", "€")
            .Replace("&#8211;", "–")
            .Replace("&#038;", "&");

            return cleanedString;
        }

    }

    public interface ICleaner
    {
        string Cleaner(string input);
    }
}
