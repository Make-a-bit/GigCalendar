using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Scraper.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public string? Artist { get; set; }
        public DateTime Date { get; set; }
        public string? PriceAsString { get; set; }
        public string? Location { get; set; }
        public int LocationId { get; set; }
        public DateTime Added { get; set; }


        public override bool Equals(object? obj)
        {
            if (obj is not Event other) return false;
            return string.Equals(Artist?.Trim(), other.Artist?.Trim(), StringComparison.OrdinalIgnoreCase)
                && Date == other.Date
                && string.Equals(Location?.Trim(), other.Location?.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(PriceAsString?.Trim(), other.PriceAsString?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Artist?.Trim().ToLowerInvariant(),
                Date,
                Location?.Trim().ToLowerInvariant(),
                PriceAsString?.Trim().ToLowerInvariant()
            );
        }

        public override string ToString()
        {
            return (Artist ?? "") + " " +
               Date.ToString("yyyy-MM-dd HH:mm") + " " +
               (PriceAsString ?? "") + " " +
               (Location ?? "");
        }
    }
}
