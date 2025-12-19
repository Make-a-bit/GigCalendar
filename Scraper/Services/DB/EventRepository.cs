using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    public interface IEventRepository
    {
        public Task<List<Event>> GetEventsAsync();
    }

    public class EventRepository : IEventRepository
    {
        private readonly DBManager _dbManager;

        public EventRepository(DBManager dBManager)
        {
            _dbManager = dBManager;
        }


        /// <summary>
        /// Fetches all events from the database.
        /// </summary>
        /// <returns></returns>
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
                    Artist = reader.IsDBNull(reader.GetOrdinal("event_artist"))
                    ? ""
                    : reader.GetString("event_artist"),
                    Date = reader.GetDateTime("event_date"),
                    PriceAsString = reader.IsDBNull(reader.GetOrdinal("event_price"))
                    ? "Ei hintatietoja"
                    : reader.GetString("event_price"),
                    Added = reader.GetDateTime("event_added")
                };

                ev.EventVenue.Name = reader.IsDBNull(reader.GetOrdinal("venue_name"))
                    ? ""
                    : reader.GetString("venue_name");
                ev.EventCity.Name = reader.IsDBNull(reader.GetOrdinal("city_name"))
                    ? ""
                    : reader.GetString("city_name");

                events.Add(ev);
            }
            return events;
        }
    }
}
