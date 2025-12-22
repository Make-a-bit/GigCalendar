using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    /// <summary>
    /// Adds new events into the database.
    /// </summary>
    public interface IEventAdder
    {
        /// <summary>
        /// Adds a new event into the database.
        /// </summary>
        /// <param name="newEvent">The event to add</param>
        /// <returns>True if the addition was successful, otherwise false</returns>
        public Task<bool> AddIntoDatabase(Event newEvent);
    }

    public class EventAdder : IEventAdder
    {
        private readonly DBManager _dbManager;

        public EventAdder(DBManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<bool> AddIntoDatabase(Event newEvent)
        {
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand("CALL add_event(@venueId, @artist, @date, @price, @showtime);", connection);
            cmd.Parameters.AddWithValue("@venueId", newEvent.EventVenue.Id);
            cmd.Parameters.AddWithValue("@locationId", newEvent.EventVenue.Id);
            cmd.Parameters.AddWithValue("@artist", newEvent.Artist);
            var dateParam = new MySqlParameter("@date", MySqlDbType.DateTime)
            {
                Value = newEvent.Showtime
            };
            cmd.Parameters.Add(dateParam); cmd.Parameters.AddWithValue("@price", newEvent.Price);
            cmd.Parameters.AddWithValue("@showtime", newEvent.HasShowtime);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
