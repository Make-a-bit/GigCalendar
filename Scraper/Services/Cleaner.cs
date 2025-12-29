using HtmlAgilityPack;
using System.Net;

namespace Scraper.Services
{
    public class Cleaner : ICleaner
    {
        public string Clean(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var decoded = WebUtility.HtmlDecode(input);
            decoded = HtmlEntity.DeEntitize(decoded);

            var cleanedString = decoded
                .Replace(" €", "€")
                .Replace("\u00A0€", "€")
                .Replace("&euro;", "€")
                .Replace("&#8364;", "€")
                .Replace("&#x20AC;", "€")
                .Replace("&#8211;", "–")
                .Replace("&amp;", "&")
                .Replace("&#038;", "&")
                .Replace('\u00A0', ' ')
                .Replace('\u202F', ' ')
                .Replace('\u2009', ' ')
                .Replace("&nbsp;", " ");

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

                var cleanedPrice = ReplacePrefixes(node.InnerText);
                cleanedPrice = Clean(cleanedPrice);
                priceString += cleanedPrice;
            }

            return priceString;
        }

        public string CleanPrice(HtmlNode node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            var cleanedPrice = ReplacePrefixes(node.InnerText);
            cleanedPrice = Clean(cleanedPrice);

            return cleanedPrice.Trim();
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

                var cleanedPrice = ReplacePrefixes(price);
                cleanedPrice = Clean(cleanedPrice);

                priceString += cleanedPrice;
            }

            return priceString;
        }

        public string ReplacePrefixes(string priceString)
        {
            priceString = priceString
                .Replace("+", "")
                .Replace("(Lippu.fi)", "")
                .Replace("(Ticketmaster)", "")
                .Replace("alakertaan ", "")
                .Replace("alk. ", "")
                .Replace("alkaen ", "")
                .Replace("Ennakot ", "")
                .Replace("ennakkoon ", "")
                .Replace("eteispalvelumaksu", "")
                .Replace("Eventualista ", "")
                .Replace("ja ", "")
                .Replace("kulut", "")
                .Replace("Liput:", "")
                .Replace("Liput\n", "")
                .Replace("Liput ", "")
                .Replace("liput ", "")
                .Replace("lippukaupan ", "")
                .Replace("Loppuunmyyty", "SOLD OUT!")
                .Replace("Loppuunvarattu", "SOLD OUT!")
                .Replace("ovelta ", "")
                .Replace("sis. ", "")
                .Replace("Ticketmasterista ", "")
                .Replace("Tiketistä ", "");

            return priceString.Trim();
        }
    }
}
