namespace Scraper.Services
{
    public static class Delay
    {
        /// <summary>
        /// Calculate random delay length between 3-5 sec
        /// </summary>
        /// <returns>Delay lenght in ms</returns>
        public static int Calculate()
        {
            return Random.Shared.Next(3000, 5000);
        }
    }
}
