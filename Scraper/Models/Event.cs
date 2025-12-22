namespace Scraper.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public string? Artist { get; set; }
        public DateTime Showtime { get; set; }
        public string? Price { get; set; }
        public City EventCity { get; set; } = new City();
        public Venue EventVenue { get; set; } = new Venue();
        public bool HasShowtime { get; set; } = true;

        /// <summary>
        /// Compares if two events are the same based on Artist, Showtime, and Venue Name.
        /// </summary>
        /// <param name="other">The other event to compare with.</param>
        /// <returns>True if the events are the same; otherwise, false.</returns>
        public bool IsSameEvent(Event other)
        {
            if (other == null) return false;
            return string.Equals(Artist?.Trim(), other.Artist?.Trim(), StringComparison.OrdinalIgnoreCase)
                && Showtime == other.Showtime
                && string.Equals(EventVenue.Name?.Trim(), other.EventVenue.Name?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Event other) return false;
            return string.Equals(Artist?.Trim(), other.Artist?.Trim(), StringComparison.OrdinalIgnoreCase)
                && Showtime == other.Showtime
                && string.Equals(EventVenue.Name?.Trim(), other.EventVenue.Name?.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(Price?.Trim(), other.Price?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Artist?.Trim().ToLowerInvariant(),
                Showtime,
                EventVenue.Name?.Trim().ToLowerInvariant(),
                Price?.Trim().ToLowerInvariant()
            );
        }

        public override string ToString()
        {
            return (Artist ?? "") + " " +
               Showtime.ToString("yyyy-MM-dd HH:mm") + " " +
               (EventVenue.Name ?? "") + " " +
               (Price ?? "");
        }
    }
}
