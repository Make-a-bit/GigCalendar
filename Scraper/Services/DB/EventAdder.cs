using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    public class EventAdder
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

            using var cmd = new MySqlCommand("CALL add_event(@locationId, @artist, @date, @price);", connection);
            cmd.Parameters.AddWithValue("@locationId", newEvent.LocationId);
            cmd.Parameters.AddWithValue("@artist", newEvent.Artist);
            cmd.Parameters.AddWithValue("@date", newEvent.Date);
            cmd.Parameters.AddWithValue("@price", newEvent.PriceAsString);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
