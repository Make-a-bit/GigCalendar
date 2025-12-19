using MySqlConnector;
using Scraper.Services.DB;

namespace Scraper.Repositories
{
    /// <summary>
    /// Repository for managing venue data in the database.
    /// </summary>
    public interface IVenueRepository
    {
        /// <summary>
        /// Gets the venue ID from database by name. 
        /// </summary>
        /// <remarks>If the venue does not exist, it creates a new entry.</remarks>
        /// <param name="name">The name of the venue.</param>
        /// <param name="cityId">The ID of the city where the venue is located.</param>
        /// <returns>The ID of the venue.</returns>
        public Task<int> GetVenueIdAsync(string name, int cityId);

        /// <summary>
        /// Creates a new venue in the database.
        /// </summary>
        /// <param name="name">The name of the venue.</param>
        /// <param name="cityId">The ID of the city where the venue is located.</param>
        /// <returns>The ID of the newly created venue.</returns>
        public Task<int> CreateVenueAsync(string name, int cityId);
    }

    public class VenueRepository : IVenueRepository
    {
        private readonly DBManager _dbManager;

        public VenueRepository(DBManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<int> GetVenueIdAsync(string name, int cityId)
        {
            int venueId = 0;

            // Open connection into database
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            // Query the venue by name
            using var cmd = new MySqlCommand(@"
                SELECT venue_id FROM venues 
                WHERE venue_name = @name", connection);
            cmd.Parameters.AddWithValue("@name", name);
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                venueId = reader.GetInt32("venue_id");
            }

            // If db query returned null (venue do not exists), create it
            if (venueId == 0)
            {
                venueId = await CreateVenueAsync(name, cityId);
            }

            return venueId;
        }

        public async Task<int> CreateVenueAsync(string name, int cityId)
        {
            // Open connection into database
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            // Call stored procedure to create new venue
            using var cmd = new MySqlCommand("CALL add_venue(@name, @id);", connection);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@id", cityId);

            // Execute and return the new venue ID
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
