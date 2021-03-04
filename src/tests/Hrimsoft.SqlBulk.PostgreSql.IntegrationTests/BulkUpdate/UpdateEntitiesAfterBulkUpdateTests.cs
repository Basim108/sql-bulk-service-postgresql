using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkUpdate.Model;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkUpdate
{
    public class UpdateEntitiesAfterBulkUpdateTests
    {
        private readonly TestConfiguration         _configuration;
        private          NpgsqlCommandsBulkService _testService;

        public UpdateEntitiesAfterBulkUpdateTests()
        {
            _configuration = new TestConfiguration("Postgres_higher_than_9_4");
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncateTableCmd   = "truncate \"unit_tests\".\"after_update_tests\";";
            var resetIdSequenceCmd = "alter sequence \"unit_tests\".\"after_update_tests_id_seq\" restart with 1;";

            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await using var command    = new NpgsqlCommand($"{truncateTableCmd}{resetIdSequenceCmd}", connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<AfterUpdateEntity>(new AfterUpdateEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var updateCommandBuilder = new UpdateSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var upsertCommandBuilder = new Mock<IUpsertSqlCommandBuilder>().Object;

            _testService = new NpgsqlCommandsBulkService(
                bulkServiceOptions,
                NullLoggerFactory.Instance,
                insertCommandBuilder,
                updateCommandBuilder,
                deleteCommandBuilder,
                upsertCommandBuilder);
        }

        [Test]
        public async Task Properties_marked_as_must_update_should_be_updated()
        {
            var elements = new List<AfterUpdateEntity>
                           {
                               new AfterUpdateEntity {Record = "rec-01", Sensor = "sens-01", Value = 127}
                           };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await _testService.InsertAsync(connection, elements, CancellationToken.None);

                elements[0].Value = 20;

                await _testService.UpdateAsync(connection, elements, CancellationToken.None);

                Assert.AreEqual("new-value-changed-by-trigger", elements[0].Record);
                Assert.AreEqual(19, elements[0].Value);
            }
        }
    }
}