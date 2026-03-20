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
        /// Compares if two events are the same based on Venue, date (day only), and Artist (fuzzy match).
        /// Time, price, and HasShowtime are intentionally excluded — they may change for the same event.
        /// </summary>
        /// <param name="other">The other event to compare with.</param>
        /// <returns>True if the events are the same; otherwise, false.</returns>
        public bool IsSameEvent(Event other)
        {
            if (other == null) return false;

            bool sameVenue = string.Equals(
                EventVenue.Name?.Trim(), other.EventVenue.Name?.Trim(),
                StringComparison.OrdinalIgnoreCase);

            bool sameDay = Showtime.Date == other.Showtime.Date;

            string a1 = Artist?.Trim() ?? "";
            string a2 = other.Artist?.Trim() ?? "";
            bool artistMatch = a1.Contains(a2, StringComparison.OrdinalIgnoreCase)
                            || a2.Contains(a1, StringComparison.OrdinalIgnoreCase);

            return sameVenue && sameDay && artistMatch;
        }

        /// <summary>
        /// Overrides the default Equals method to compare events based on Artist, Showtime, Venue Name, and Price.
        /// </summary>
        /// <remarks>This method considers two events equal if they have the same Artist, Showtime, Venue Name, 
        /// and Price, ignoring case and leading/trailing whitespace.</remarks>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>True if the events are equal; otherwise, false.</returns>
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
