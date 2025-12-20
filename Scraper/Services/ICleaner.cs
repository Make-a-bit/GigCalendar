using HtmlAgilityPack;

namespace Scraper.Services
{
    public interface ICleaner
    {
        /// <summary>
        /// Cleans a string by replacing HTML entities with their corresponding characters.
        /// </summary>
        /// <param name="input">The input string to clean.</param>
        /// <returns>The cleaned string.</returns>
        string Clean(string input);

        /// <summary>
        /// Parses event prices from the given HTML nodes.
        /// </summary>
        /// <param name="nodes">HTML nodes containing price information</param>
        /// <returns>Concatenated string of event prices</returns>
        string CleanPrice(HtmlNodeCollection nodes);

        /// <summary>
        /// Parses event prices from the given string array.
        /// </summary>
        /// <param name="prices"></param>
        /// <returns>Concatenated string of event prices</returns>
        string CleanPrice(string[] prices);
    }
}
