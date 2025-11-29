using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Scraper.Models
{
    public class Event
    {
        public string? Name { get; set; }
        public DateTime Date { get; set; }
        public double? PriceAsDouble { get; set; }
        public string? PriceAsString { get; set; }
        public string? Location { get; set; }


        public override bool Equals(object? obj)
        {
            if (obj is not Event other) return false;
            return string.Equals(Name?.Trim(), other.Name?.Trim(), StringComparison.OrdinalIgnoreCase)
                && Date == other.Date
                && string.Equals(Location?.Trim(), other.Location?.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(PriceAsString?.Trim(), other.PriceAsString?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name?.Trim().ToLowerInvariant(),
                Date,
                Location?.Trim().ToLowerInvariant(),
                PriceAsString?.Trim().ToLowerInvariant()
            );
        }
    }
}
