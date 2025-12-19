using MySqlConnector;
using Scraper.Services.DB;

namespace Scraper.Services
{
    /// <summary>
    /// Repository for managing city data in the database.
    /// </summary>
    public interface ICityRepository
    {
        /// <summary>
        /// Gets the city ID from database by name. 
        /// </summary>
        /// <remarks>If the city does not exist, it creates a new entry.</remarks>
        /// <param name="name">The name of the city.</param>
        /// <returns>The ID of the city.</returns>
        public Task<int> GetCityIdAsync(string name);

        /// <summary>
        /// Creates a new city in the database. 
        /// </summary>
        /// <param name="name">The name of the city.</param>
        /// <returns>The ID of the newly created city.</returns>
        public Task<int> CreateCityAsync(string name);
    }

    public class CityRepository : ICityRepository
    {
        private readonly DBManager _dbManager;

        public CityRepository(DBManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<int> GetCityIdAsync(string name)
        {
            int cityId = 0;

            // Open connection into database
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            // Query the city by name
            using var cmd = new MySqlCommand(@"
                SELECT city_id FROM cities
                WHERE city_name = @name", connection);
            cmd.Parameters.AddWithValue("@name", name);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cityId = reader.GetInt32("city_id");
            }

            // If db query returned null (city do not exists), create it
            if (cityId == 0)
            {
                cityId = await CreateCityAsync(name);
            }

            return cityId;
        }

        public async Task<int> CreateCityAsync(string name)
        {
            // Open connection into database
            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            // Call stored procedure to create new city
            using var cmd = new MySqlCommand("CALL add_city(@name);", connection);
            cmd.Parameters.AddWithValue("name", name);

            // Execute the command and get the newly created city ID
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
