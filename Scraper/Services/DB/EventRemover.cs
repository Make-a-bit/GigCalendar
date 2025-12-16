using Microsoft.Extensions.Logging;
using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    public interface IEventRemover
    {
        public Task CleanupOldEvents(List<Event> events);
    }

    public class EventRemover : IEventRemover
    {
        private readonly ILogger<EventRemover> _logger;
        private readonly DBManager _dbManager;

        public EventRemover(DBManager dbManager, ILogger<EventRemover> logger)
        {
            _dbManager = dbManager;
            _logger = logger;
        }


        /// <summary>
        /// Cleans up old events from the database.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task CleanupOldEvents(List<Event> events)
        {
            var tempEvents = events;

            using var connection = _dbManager.GetConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(@"
              DELETE FROM events WHERE event_date < CURDATE()", connection);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Removed {Count} old events from database", rowsAffected);
        }
    }
}
