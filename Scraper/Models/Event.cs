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

            return sameVenue && sameDay && ArtistMatch(Artist, other.Artist);
        }

        private static bool IsTimeAnnotation(string part)
        {
            var s = part.TrimStart('(').TrimEnd(')');
            var sep = s.IndexOfAny(new[] { '.', ':' });
            if (sep < 1 || sep > 2) return false;
            var minutePart = s[(sep + 1)..];
            return minutePart.Length == 2
                && int.TryParse(s[..sep], out int h) && h is >= 0 and <= 23
                && int.TryParse(minutePart, out int m) && m is >= 0 and <= 59;
        }

        private static string StripTimeAnnotations(string token)
        {
            var parts = token.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts.Where(p => !IsTimeAnnotation(p)));
        }

        private static string NormalizeArtist(string? artist)
        {
            if (string.IsNullOrWhiteSpace(artist)) return "";
            var s = artist
                .Replace("+", ",")
                .Replace("&", ",")
                .Replace("!", "")
                .Replace(":", "");
            var tokens = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join(",", tokens.Select(StripTimeAnnotations)).ToLowerInvariant();
        }

        private static bool ArtistMatch(string? a1, string? a2)
        {
            var n1 = NormalizeArtist(a1);
            var n2 = NormalizeArtist(a2);

            if (n1.Contains(n2) || n2.Contains(n1)) return true;

            var tokens1 = n1.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
            var tokens2 = n2.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

            if (tokens1.Count == 0 || tokens2.Count == 0) return false;

            var smaller = tokens1.Count <= tokens2.Count ? tokens1 : tokens2;
            var larger  = tokens1.Count <= tokens2.Count ? tokens2 : tokens1;

            return smaller.All(t => larger.Any(l => l.Contains(t) || t.Contains(l)));
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
