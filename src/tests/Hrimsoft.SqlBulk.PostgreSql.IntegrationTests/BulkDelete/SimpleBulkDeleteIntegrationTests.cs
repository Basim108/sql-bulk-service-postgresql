using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkDelete
{
    public class SimpleBulkDeleteIntegrationTests
    {
        private readonly TestConfiguration _configuration;

        public SimpleBulkDeleteIntegrationTests()
        {
            _configuration = new TestConfiguration();
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncateTableCmd = "truncate \"unit_tests\".\"simple_test_entity\";";
            var resetIdSequenceCmd = "ALTER SEQUENCE \"unit_tests\".\"simple_test_entity_id_seq\" RESTART WITH 1;";
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            using (var command = new NpgsqlCommand($"{truncateTableCmd}{resetIdSequenceCmd}", connection))
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }
        
        private async Task<long> HowManyRowsWithIdsAsync(int[] ids)
        {
            var whereClause = $"where \"id\" in ({string.Join(",", ids)})";
            var query = $"select count(\"id\") from \"unit_tests\".\"simple_test_entity\" {whereClause};";
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            using (var command = new NpgsqlCommand(query, connection))
            {
                await connection.OpenAsync();
                var result = (long) await command.ExecuteScalarAsync();
                return result;
            }
        }
        
        [Test]
        public async Task SimpleDelete_should_update_autogenerated_fields()
        {
            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<TestEntity>(new SimpleEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
            var deleteCommandBuilder = new SimpleDeleteSqlCommandBuilder(NullLoggerFactory.Instance);
            var upsertCommandBuilder = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);

            var testService = new NpgsqlCommandsBulkService(
                bulkServiceOptions, 
                NullLoggerFactory.Instance, 
                insertCommandBuilder, 
                updateCommandBuilder, 
                deleteCommandBuilder, 
                upsertCommandBuilder);

            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 128}
            };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
            }

            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.DeleteAsync(connection, elements, CancellationToken.None);
            }

            var countOfRows = await HowManyRowsWithIdsAsync(new[] {elements[0].Id, elements[1].Id});
            Assert.AreEqual(0, countOfRows);
        }

        [Test]
        public async Task SimpleDelete_should_split_correctly_even_number_of_elements()
        {
            var bulkServiceOptions = new BulkServiceOptions(2);
            bulkServiceOptions.AddEntityProfile<TestEntity>(new SimpleEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
            var deleteCommandBuilder = new SimpleDeleteSqlCommandBuilder(NullLoggerFactory.Instance);
            var upsertCommandBuilder = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);

            var testService = new NpgsqlCommandsBulkService(
                bulkServiceOptions, 
                NullLoggerFactory.Instance, 
                insertCommandBuilder, 
                updateCommandBuilder, 
                deleteCommandBuilder, 
                upsertCommandBuilder);

            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 128},
                new TestEntity {RecordId = "rec-01", SensorId = "sens-02", Value = 227},
                new TestEntity {RecordId = "rec-02", SensorId = "sens-02", Value = 228}
            };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
            }
            
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.DeleteAsync(connection, elements, CancellationToken.None);
            }

            var countOfRows = await HowManyRowsWithIdsAsync(new[] {elements[0].Id, elements[1].Id, elements[2].Id, elements[3].Id});
            Assert.AreEqual(0, countOfRows);
        }

        [Test]
        public async Task SimpleDelete_should_split_correctly_odd_number_of_elements()
        {
            var bulkServiceOptions = new BulkServiceOptions(2);
            bulkServiceOptions.AddEntityProfile<TestEntity>(new SimpleEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
            var deleteCommandBuilder = new SimpleDeleteSqlCommandBuilder(NullLoggerFactory.Instance);
            var upsertCommandBuilder = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);

            var testService = new NpgsqlCommandsBulkService(
                bulkServiceOptions, 
                NullLoggerFactory.Instance, 
                insertCommandBuilder, 
                updateCommandBuilder, 
                deleteCommandBuilder, 
                upsertCommandBuilder);

            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 128},
                new TestEntity {RecordId = "rec-01", SensorId = "sens-02", Value = 227},
            };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
            }

            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.DeleteAsync(connection, elements, CancellationToken.None);
            }
            
            var countOfRows = await HowManyRowsWithIdsAsync(new[] {elements[0].Id, elements[1].Id, elements[2].Id });
            Assert.AreEqual(0, countOfRows);

        }
    }
}