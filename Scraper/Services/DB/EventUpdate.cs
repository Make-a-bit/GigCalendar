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
        /// Updates the price of an event in the database.
        /// </summary>
        /// <param name="eventToUpdate">The event with the updated price</param>
        /// <returns>True if the update was successful, otherwise false</returns>
        Task<bool> UpdatePriceAsync(Event eventToUpdate);
    }

    public class EventUpdate : IEventUpdate
    {
        private readonly DBManager _dbManager;

        public EventUpdate(DBManager dbmanager)
        {
            _dbManager = dbmanager;
        }

        public async Task<bool> UpdatePriceAsync(Event eventToUpdate)
        {
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(@"
                UPDATE events SET event_price = @price
                WHERE event_id = @id;", connection);
            cmd.Parameters.AddWithValue("@price", eventToUpdate.Price);
            cmd.Parameters.AddWithValue("@id", eventToUpdate.EventId);

            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }
    }
}
