using HtmlAgilityPack;
using Scraper.Models;
using System.Net;

namespace Scraper.Services.Scrapers
{
    public class BaseScraper
    {
        public Venue Venue { get; set; }
        public City City { get; set; }
        public List<Event> Events { get; set; }
        public HtmlDocument Doc { get; set; }

        public BaseScraper()
        {
            Venue = new Venue();
            City = new City();
            Events = new List<Event>();
            Doc = new HtmlDocument();
        }

        /// <summary>
        /// Validates that a URL is safe to request (prevents SSRF attacks).
        /// </summary>
        /// <param name="url">The URL to validate.</param>
        /// <returns>True if the URL is safe to request, false otherwise.</returns>
        protected bool IsValidUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != "http" && uri.Scheme != "https")
                return false;

            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return false;

            if (IPAddress.TryParse(uri.Host, out var ip) && IsPrivateIP(ip))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if an IP address is in a private range.
        /// </summary>
        /// <param name="ip">The IP address to check.</param>
        /// <returns>True if the IP is private, false otherwise.</returns>
        private static bool IsPrivateIP(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();

            // Check for IPv4 private ranges
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // 10.0.0.0/8
                if (bytes[0] == 10)
                    return true;

                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return true;

                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;

                // 127.0.0.0/8 (loopback)
                if (bytes[0] == 127)
                    return true;

                // 169.254.0.0/16 (link-local)
                if (bytes[0] == 169 && bytes[1] == 254)
                    return true;
            }

            return false;
        }
    }
}
