using Microsoft.Extensions.Logging;
using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    /// <summary>
    /// Interface for removing old events from the database.
    /// </summary>
    public interface IEventRemover
    {
        /// <summary>
        /// Cleans up old events from the database that are before the current date.
        /// </summary>
        /// <param name="events">List of events to consider for cleanup</param>
        /// <returns></returns>
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
