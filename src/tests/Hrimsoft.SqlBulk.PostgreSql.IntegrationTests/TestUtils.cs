using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests
{
    /// <summary>
    /// Contains helper methods
    /// </summary>
    public class TestUtils
    {
        private readonly TestConfiguration _configuration;
        
        public TestUtils(TestConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Calculate number of rows with defined ids 
        /// </summary>
        public async Task<long> HowManyRowsInTableAsync(EntityProfile entityProfile)
        {
            return await HowManyRowsInTableAsync(entityProfile, "");
        }
        
        /// <summary>
        /// Calculate number of rows with defined ids 
        /// </summary>
        public async Task<long> HowManyRowsWithIdsAsync(EntityProfile entityProfile, int[] ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            
            var whereClause = $"where \"id\" in ({string.Join(",", ids)})";
            return await HowManyRowsInTableAsync(entityProfile, whereClause);
        }
        
        /// <summary>
        /// Calculate number of rows with defined ids 
        /// </summary>
        private async Task<long> HowManyRowsInTableAsync(EntityProfile entityProfile, string whereClause)
        {
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));
            if (whereClause == null)
                whereClause = "";
            
            var query = $"select count(1) from {entityProfile.TableName} {whereClause};";
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            using (var command = new NpgsqlCommand(query, connection))
            {
                await connection.OpenAsync();
                var result = (long) await command.ExecuteScalarAsync();
                return result;
            }
        }
    }
}