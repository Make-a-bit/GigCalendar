using MySqlConnector;

namespace Scraper.Services.DB
{
    public class DBManager
    {
        private readonly string _connectionString;

        public DBManager()
        {
            var server = Environment.GetEnvironmentVariable("GIGS_DB_SERVER") ?? "localhost";
            var user = Environment.GetEnvironmentVariable("GIGS_DB_USER") ?? "user";
            var pwd = Environment.GetEnvironmentVariable("GIGS_DB_PASSWORD") ?? "password";
            var db = Environment.GetEnvironmentVariable("GIGS_DB_NAME") ?? "eventsdb";

            var builder = new MySqlConnectionStringBuilder
            {
                Server = server,
                UserID = user,
                Password = pwd,
                Database = db,
            };

            _connectionString = builder.ConnectionString;
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }

    
}
