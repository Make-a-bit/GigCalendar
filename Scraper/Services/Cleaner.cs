using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Scraper.Services
{
    public class Cleaner : ICleaner
    {
        public string Clean(string input)
        {
            var cleanedString = input
            .Replace("&euro;", "€")
            .Replace("&#8364;", "€")
            .Replace("&#x20AC;", "€")
            .Replace("&#8211;", "–")
            .Replace("&amp;", "&")
            .Replace("&nbsp;", " ")
            .Replace("&#038;", "&");

            return cleanedString.Trim();
        }

        public string CleanPrice(HtmlNodeCollection nodes)
        {
            var priceString = string.Empty;

            if (nodes == null)
            {
                return priceString;
            }

            foreach (var node in nodes)
            {
                if (priceString != string.Empty)
                {
                    priceString += " / ";
                }

                var cleanedPrice = Clean(node.InnerText);
                cleanedPrice = cleanedPrice.Replace(" €", "€");
                priceString += cleanedPrice;
            }

            return priceString;
        }

        public string CleanPrice(string[] prices)
        {
            var priceString = string.Empty;

            if (prices == null)
            {
                return priceString;
            }

            foreach (var price in prices)
            {
                if (priceString != string.Empty)
                {
                    priceString += " / ";
                }

                var cleanedPrice = Clean(price);
                cleanedPrice = cleanedPrice
                    .Replace(" €", "€")
                    .Replace("Liput", "")
                    .Replace("Loppuunmyyty", "SOLD OUT!")
                    .Trim();
                priceString += cleanedPrice;
            }

            return priceString;
        }


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
