using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    /// <summary>
    /// Updates existing events in the database.
    /// </summary>
    public interface IEventUpdate
    {
        /// <summary>
        /// Updates all mutable fields (artist, date, has_showtime, price) of an event in the database.
        /// </summary>
        /// <param name="eventToUpdate">The event with updated values</param>
        /// <returns>True if the update was successful, otherwise false</returns>
        Task<bool> UpdateEventAsync(Event eventToUpdate);
    }

    public class EventUpdate : IEventUpdate
    {
        private readonly DBManager _dbManager;

        public EventUpdate(DBManager dbmanager)
        {
            _dbManager = dbmanager;
        }

        public async Task<bool> UpdateEventAsync(Event eventToUpdate)
        {
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(@"
                UPDATE events
                SET event_artist = @artist,
                    event_date = @date,
                    event_has_showtime = @hasShowtime,
                    event_price = @price
                WHERE event_id = @id;", connection);

            cmd.Parameters.AddWithValue("@artist", eventToUpdate.Artist);
            var dateParam = new MySqlParameter("@date", MySqlDbType.DateTime)
            {
                Value = eventToUpdate.Showtime
            };
            cmd.Parameters.Add(dateParam);
            cmd.Parameters.AddWithValue("@hasShowtime", eventToUpdate.HasShowtime);
            cmd.Parameters.AddWithValue("@price", eventToUpdate.Price);
            cmd.Parameters.AddWithValue("@id", eventToUpdate.EventId);

            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }
    }
}
