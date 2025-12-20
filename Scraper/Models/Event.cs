namespace Scraper.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public string? Artist { get; set; }
        public DateTime Date { get; set; }
        public string? Price { get; set; }
        public City EventCity { get; set; } = new City();
        public Venue EventVenue { get; set; } = new Venue();
        public bool HasShowtime { get; set; } = true;


        public bool IsSameEvent(Event other)
        {
            if (other == null) return false;
            return string.Equals(Artist?.Trim(), other.Artist?.Trim(), StringComparison.OrdinalIgnoreCase)
                && Date == other.Date
                && string.Equals(EventVenue.Name?.Trim(), other.EventVenue.Name?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Event other) return false;
            return string.Equals(Artist?.Trim(), other.Artist?.Trim(), StringComparison.OrdinalIgnoreCase)
                && Date == other.Date
                && string.Equals(EventVenue.Name?.Trim(), other.EventVenue.Name?.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(Price?.Trim(), other.Price?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Artist?.Trim().ToLowerInvariant(),
                Date,
                EventVenue.Name?.Trim().ToLowerInvariant(),
                Price?.Trim().ToLowerInvariant()
            );
        }

        public override string ToString()
        {
            return (Artist ?? "") + " " +
               Date.ToString("yyyy-MM-dd HH:mm") + " " +
               (EventVenue.Name ?? "") + " " +
               (Price ?? "");
        }
    }
}
