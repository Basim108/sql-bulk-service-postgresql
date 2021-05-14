using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkUpsert
{
    public class CompositePkTests
    {
        private readonly TestConfiguration _configuration;

        private IPostgreSqlBulkService _testService;
        private BulkServiceOptions     _bulkServiceOptions;

        public CompositePkTests()
        {
            // As upsert command was implemented only in postgre version of 9.5+ 
            _configuration = new TestConfiguration("Postgres_higher_than_9_4");
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncateTableCmd   = "truncate \"unit_tests\".\"entity_with_composite_pk\";";

            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await using var command    = new NpgsqlCommand(truncateTableCmd, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            _bulkServiceOptions = new BulkServiceOptions();
            _bulkServiceOptions.AddEntityProfile<TestEntityWithCompositePk>(new EntityWithCompositePkProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
            var upsertCommandBuilder = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);

            _testService = new NpgsqlCommandsBulkService(
                _bulkServiceOptions,
                NullLoggerFactory.Instance,
                insertCommandBuilder,
                updateCommandBuilder,
                deleteCommandBuilder,
                upsertCommandBuilder);
        }

        [Test]
        public async Task Should_insert()
        {
            var elements = new List<TestEntityWithCompositePk>
                           {
                               new TestEntityWithCompositePk {UserId = 1, Column2 = "value-2", Column3 = 3},
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.UpsertAsync(connection, elements, CancellationToken.None);
            var       query   = "select * from unit_tests.entity_with_composite_pk;";
            using var command = new NpgsqlCommand(query, connection);
            using (var reader = command.ExecuteReader()) {
                var count = 0;
                while (reader.Read()) {
                    count++;
                    Assert.AreEqual(1, count);
                    Assert.AreEqual(1, (int)reader["user_id"]);
                    Assert.AreEqual("value-2", (string)reader["column2"]);
                    Assert.AreEqual(3, (int)reader["column3"]);
                }
                await reader.CloseAsync();
            }
        }

        [Test]
        public async Task Should_update()
        {
            var elements = new List<TestEntityWithCompositePk>
                           {
                               new TestEntityWithCompositePk {UserId = 1, Column2 = "value-2", Column3 = 5},
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);
            elements[0].Column3 = 10; // this item should be updated
            // This item should be inserted
            await _testService.UpsertAsync(connection, elements, CancellationToken.None);

            var       query   = "select * from unit_tests.entity_with_composite_pk;";
            using var command = new NpgsqlCommand(query, connection);
            using (var reader = command.ExecuteReader()) {
                var count = 0;
                while (reader.Read()) {
                    count++;
                    Assert.AreEqual(1, count);
                    Assert.AreEqual(1, (int)reader["user_id"]);
                    Assert.AreEqual("value-2", (string)reader["column2"]);
                    Assert.AreEqual(10, (int)reader["column3"]);
                }
                await reader.CloseAsync();
            }
        }
    }
}