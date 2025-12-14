using Microsoft.Extensions.Logging;
using MySqlConnector;
using Scraper.Models;

namespace Scraper.Services.DB
{
    public interface IEventRemover
    {
        public Task CleanUpOldEvents(List<Event> events);
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

        public async Task CleanUpOldEvents(List<Event> events)
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
