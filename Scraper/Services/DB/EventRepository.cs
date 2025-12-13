using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    public class EventRepository
    {
        private readonly DBManager _dbManager;

        public EventRepository(DBManager dBManager)
        {
            _dbManager = dBManager;
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            var events = new List<Event>();

            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand("SELECT * FROM view_events", connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var ev = new Event
                {
                    EventId = reader.GetInt32("event_id"),
                    Artist = reader.GetString("event_artist"),
                    Date = reader.GetDateTime("event_date"),
                    PriceAsString = reader.GetString("event_price"),
                    Location = reader.GetString("venue_name"),
                    Added = reader.GetDateTime("event_added")
                };
                events.Add(ev);
            }
            return events;
        }
    }
}
