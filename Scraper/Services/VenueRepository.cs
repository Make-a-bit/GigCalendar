using MySqlConnector;
using Scraper.Services.DB;

namespace Scraper.Services
{
    public class VenueRepository
    {
        private readonly DBManager _dbManager;

        public VenueRepository(DBManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<int> GetVenueByNameAsync(string name)
        {
            int venueId = 0;

            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT * FROM venues 
                WHERE venue_name = @name", connection);
            cmd.Parameters.AddWithValue("@name", name);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                venueId = reader.GetInt32("venue_id");
            }
            
            if (venueId == 0)
            {
                venueId = await CreateVenueByNameAsync(name);
            }

            return venueId;
        }
        
        public async Task<int> CreateVenueByNameAsync(string name)
        {
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(@"
                INSERT INTO venues (venue_name) 
                VALUES (@name); 
                SELECT LAST_INSERT_ID();", connection);
            cmd.Parameters.AddWithValue("@name", name);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
