namespace Scraper.Services
{
    public interface ICleaner
    {
        string Clean(string input);
        string PriceCleaner(string input);
    }
}
