using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests
{
    public class TestConfiguration
    {
        private readonly string _environment;

        // In secrets.json it is recommended to set environment fields first e.g.
        // { "Staging": { "DbHost": "...", ... }, "Postgres_9_5": { "DbHost": "...", ... }, "Postgres_9_4": {...} }
        public TestConfiguration(string environment="Postgres_higher_than_9_4")
        {
            _environment = environment;
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<TestConfiguration>()
                .Build();
        }

        public IConfiguration Configuration { get; private set; }

        private string _connectionString;

        public string ConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    var commonOptions = this.Configuration.GetConnectionString("postgres");
                    var builder = new NpgsqlConnectionStringBuilder(commonOptions)
                    {
                        // In secrets.json it is recommended to set environment fields first e.g.
                        // { "Staging": { "DbHost": "...", ... }, "Postgre_9_5": { "DbHost": "...", ... }, "Postgre_9_4": {...} }
                        Host = Configuration[$"{_environment}:DbHost"],
                        Database = Configuration[$"{_environment}:Database"],
                        Username = Configuration[$"{_environment}:DbUsername"],
                        Password = Configuration[$"{_environment}:DbPassword"]
                    };
                    _connectionString = builder.ConnectionString;
                }

                return _connectionString;
            }
        } 
    }
}