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
        /// Parses event price from the given HTML node.
        /// </summary>
        /// <param name="node">HTML node containing price information</param>
        /// <returns>String representation of the event price</returns>
        string CleanPrice(HtmlNode node);

        /// <summary>
        /// Parses event prices from the given string array.
        /// </summary>
        /// <param name="prices"></param>
        /// <returns>Concatenated string of event prices</returns>
        string CleanPrice(string[] prices);

        /// <summary>
        /// Replaces known prefixes in the price string to standardize it.
        /// </summary>
        /// <param name="priceString">The price string to process.</param>
        /// <returns>The price string with known prefixes replaced or removed.</returns>
        string ReplacePrefixes(string priceString);
    }
}
