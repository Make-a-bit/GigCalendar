using System.Text.RegularExpressions;

namespace Scraper.Services
{
    public class StringCleaner : ICleaner
    {
        /// <summary>
        /// Cleans a string by replacing HTML entities with their corresponding characters.
        /// </summary>
        /// <param name="input">The input string to clean.</param>
        /// <returns>The cleaned string.</returns>
        public string EventCleaner(string input)
        {
            var cleanedString = input
            .Replace("&euro;", "€")
            .Replace("&#8364;", "€")
            .Replace("&#x20AC;", "€")
            .Replace("&#8211;", "–")
            .Replace("&amp;", "&")
            .Replace("&#038;", "&");

            return cleanedString.Trim();
        }

        /// <summary>
        /// Cleans a price string by extracting price values or indicating if sold out.
        /// </summary>
        /// <param name="input">The input string to clean.</param>
        /// <returns>The cleaned price string.</returns>
        public string PriceCleaner(string input)
        {
            if (input.ToLower().Contains("loppuunmyyty") || input.ToLower().Contains("loppuunvarattu"))
            {
                return "SOLD OUT!";
            }

            // Remove "Liput" and trim whitespace
            string pricesRaw = input.Replace("Liput", "").Trim();

            // Use regex to extract all price values (e.g., "32,50 €", "35 €")
            var matches = Regex.Matches(pricesRaw, @"\d{1,3}(?:[.,]\d{2})?\s*€");

            // Join the prices with " / " if there are multiple
            string prices = string.Join(" / ", matches.Select(m => m.Value.Replace(" ", "")));

            return prices;
        }
    }
}
