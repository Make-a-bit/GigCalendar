using MySqlConnector;
using Scraper.Models;
using Scraper.Services.DB;

namespace Scraper.Repositories
{
    /// <summary>
    /// Repository for managing event data in the database.
    /// </summary>
    public interface IEventRepository
    {
        /// <summary>
        /// Fetches all events from the database.
        /// </summary>
        /// <returns>List of events</returns>
        public Task<List<Event>> GetEventsAsync();
    }

    public class EventRepository : IEventRepository
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
                var ev = new Event();

                ev.EventId = reader.GetInt32("event_id");
                ev.EventVenue.Id = reader.IsDBNull(reader.GetOrdinal("venue_id"))
                    ? 0
                    : reader.GetInt32("venue_id");
                ev.EventVenue.Name = reader.IsDBNull(reader.GetOrdinal("venue_name"))
                    ? ""
                    : reader.GetString("venue_name");
                ev.EventCity.Name = reader.IsDBNull(reader.GetOrdinal("city_name"))
                    ? ""
                    : reader.GetString("city_name");
                ev.EventCity.Id = reader.IsDBNull(reader.GetOrdinal("city_id"))
                    ? 0
                    : reader.GetInt32("city_id");
                ev.Artist = reader.IsDBNull(reader.GetOrdinal("event_artist"))
                    ? ""
                    : reader.GetString("event_artist");
                ev.Showtime = reader.GetDateTime("event_date");
                ev.Price = reader.IsDBNull(reader.GetOrdinal("event_price"))
                    ? string.Empty
                    : reader.GetString("event_price");
                ev.HasShowtime = reader.GetBoolean("event_has_showtime");

                events.Add(ev);
            }
            return events;
        }
    }
}
