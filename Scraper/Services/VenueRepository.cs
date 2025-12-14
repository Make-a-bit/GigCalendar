using MySqlConnector;
using Scraper.Services.DB;

namespace Scraper.Services
{
    public interface IVenueRepository
    {
        public Task<int> GetVenueIdAsync(string name);
        public Task<int> CreateVenueAsync(string name);
    }

    public class VenueRepository : IVenueRepository
    {
        private readonly DBManager _dbManager;

        public VenueRepository(DBManager dbManager)
        {
            _dbManager = dbManager;
        }


        /// <summary>
        /// Gets the venue ID by name. 
        /// </summary>
        /// <param name="name"></param>
        /// <remarks>If the venue does not exist, it creates a new entry.</remarks>
        /// <returns></returns>
        public async Task<int> GetVenueIdAsync(string name)
        {
            int venueId = 0;

            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT venue_id FROM venues 
                WHERE venue_name = @name", connection);
            cmd.Parameters.AddWithValue("@name", name);
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                venueId = reader.GetInt32("venue_id");
            }

            // If venue does not exist, create it
            if (venueId == 0)
            {
                venueId = await CreateVenueAsync(name);
            }

            return venueId;
        }


        /// <summary>
        /// Creates a new venue and returns its ID.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<int> CreateVenueAsync(string name)
        {
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand("CALL add_venue(@name);", connection);
            cmd.Parameters.AddWithValue("@name", name);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
