using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests
{
    public class TestConfiguration
    {
        private readonly string _environment;

        public TestConfiguration(string environment="Staging")
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
                        // { "Staging": { "DbHost": "...", ... }, "Development": { "DbHost": "...", ... } }
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