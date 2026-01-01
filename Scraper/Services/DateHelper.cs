namespace Scraper.Services
{
    /// <summary>
    /// DateHelper class for date adjustments.
    /// </summary>
    public static class DateHelper
    {
        private static readonly string[] _weekdays = {"ma", "ti", "ke", "to", "pe", "la", "su" };

        /// <summary>
        /// Adjusts the year based on the provided month and day.
        /// </summary>
        /// <param name="month">The month of the date to adjust.</param>
        /// <param name="day">The day of the date to adjust.</param>
        /// <returns>The adjusted year.</returns>
        public static int AdjustYear(int month, int day)
        {
            var now = DateTime.Now;

            // Adjust year if the date has already passed this year
            if (month < now.Month || month == now.Month && day < now.Day)
            {
                return now.Year + 1;
            }

            // Otherwise, return the current year
            return now.Year;
        }

        /// <summary>
        /// Checks if the input string starts with a weekday prefix.
        /// </summary>
        /// <param name="input">The input string to check for a weekday prefix.</param>
        /// <returns>True if the input starts with a weekday prefix; otherwise, false.</returns>
        public static bool IsWeekdayPrefix(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 2)
            {
                return false;
            }
            var prefix = input.Substring(0, 2).ToLower();
            return Array.Exists(_weekdays, day => day == prefix);
        }

        /// <summary>
        /// Get the month number from the Finnish month name.
        /// </summary>
        /// <param name="month">The Finnish name of the month.</param>
        /// <returns>The month number (1-12).</returns>
        /// <exception cref="ArgumentException">Thrown when the month name is invalid.</exception>
        public static int ConvertMonthName(string month)
        {
            return month.Trim().ToLowerInvariant() switch
            {
                "tammi" => 1,
                "helmi" => 2,
                "maalis" => 3,
                "huhti" => 4,
                "touko" => 5,
                "kesä" => 6,
                "heinä" => 7,
                "elo" => 8,
                "syys" => 9,
                "loka" => 10,
                "marras" => 11,
                "joulu" => 12,
                _ => throw new ArgumentException($"Invalid month name: {month}")
            };
        }
    }
}