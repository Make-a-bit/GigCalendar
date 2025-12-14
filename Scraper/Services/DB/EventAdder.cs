using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    public interface IEventAdder
    {
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

            using var cmd = new MySqlCommand("CALL add_event(@locationId, @artist, @date, @price);", connection);
            cmd.Parameters.AddWithValue("@locationId", newEvent.LocationId);
            cmd.Parameters.AddWithValue("@artist", newEvent.Artist);
            var dateParam = new MySqlParameter("@date", MySqlDbType.DateTime)
            {
                Value = newEvent.Date
            };
            cmd.Parameters.Add(dateParam); cmd.Parameters.AddWithValue("@price", newEvent.PriceAsString);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
