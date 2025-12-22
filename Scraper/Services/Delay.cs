namespace Scraper.Services
{
    /// <summary>
    /// Provides methods to calculate delays for scheduling tasks.
    /// </summary>
    public static class Delay
    {
        /// <summary>
        /// CalculateSeconds random delay length between 3-5 sec
        /// </summary>
        /// <returns>Delay lenght in ms</returns>
        public static int CalculateSeconds()
        {
            return Random.Shared.Next(3000, 5000);
        }


        /// <summary>
        /// Calculates the delay until the next scheduled run at 01:00
        /// </summary>
        /// <returns></returns>
        public static TimeSpan CalculateNextRun()
        {
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddDays(1).AddHours(1);

            var delay = nextRun - now;

            // Safety check: if delay is negative or too small, schedule for next day
            if (delay.TotalHours < 1)
            {
                nextRun = nextRun.AddDays(1);
                delay = nextRun - now;
            }

            return delay;
        }
    }
}
