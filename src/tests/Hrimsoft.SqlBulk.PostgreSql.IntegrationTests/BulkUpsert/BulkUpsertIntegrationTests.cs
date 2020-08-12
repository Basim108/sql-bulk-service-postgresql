using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkUpsert
{
    public class BulkUpsertIntegrationTests
    {
        private readonly TestConfiguration _configuration;

        public BulkUpsertIntegrationTests()
        {
            // As upsert command was implemented only in postgre version of 9.5+ 
            _configuration = new TestConfiguration("Postgres_higher_than_9_4");
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncateTableCmd   = "truncate \"unit_tests\".\"entity_with_unique_columns\";";
            var resetIdSequenceCmd = "ALTER SEQUENCE \"unit_tests\".\"entity_with_unique_columns_id_seq\" RESTART WITH 1;";
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            using (var command = new NpgsqlCommand($"{truncateTableCmd}{resetIdSequenceCmd}", connection)) {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        [Test]
        public async Task Should_insert_nullable()
        {
            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<TestEntity>(new UpsertEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
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
            };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString)) {
                await testService.UpsertAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual("rec-01", elements[0].RecordId);
            Assert.AreEqual("sens-01", elements[0].SensorId);
            Assert.AreEqual(127, elements[0].Value);
            Assert.IsNull(elements[0].NullableValue);
        }

        [Test]
        public async Task Should_update_nullable()
        {
            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<TestEntity>(new UpsertEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
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
                new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127, NullableValue = 12},
            };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString)) {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
                elements.First().NullableValue = null; // this item should be updated
                // This item should be inserted
                await testService.UpsertAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual("rec-01", elements[0].RecordId);
            Assert.AreEqual("sens-01", elements[0].SensorId);
            Assert.AreEqual(127, elements[0].Value);
            Assert.IsNull(elements[0].NullableValue);
        }

        [Test]
        public async Task Should_insert_and_update()
        {
            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<TestEntity>(new UpsertEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
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
            };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString)) {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
                elements.First().Value = 128; // this item should be updated
                // This item should be inserted
                elements.Add(new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 1});
                await testService.UpsertAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual("rec-01", elements[0].RecordId);
            Assert.AreEqual("sens-01", elements[0].SensorId);
            Assert.AreEqual(128, elements[0].Value);

            // id = 3 because upsert calls a sequence then has a constraint violation and start doing update.
            // Then it processes the second element and again calls sequence so gets 3;
            Assert.AreEqual(3, elements[1].Id);
            Assert.AreEqual("rec-02", elements[1].RecordId);
            Assert.AreEqual("sens-01", elements[1].SensorId);
            Assert.AreEqual(1, elements[1].Value);
        }

        [Test]
        public async Task Should_update_autogenerated_fields()
        {
            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<TestEntity>(new UpsertEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
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
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString)) {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual("rec-01", elements[0].RecordId);
            Assert.AreEqual("sens-01", elements[0].SensorId);
            Assert.AreEqual(127, elements[0].Value);

            Assert.AreEqual(2, elements[1].Id);
            Assert.AreEqual("rec-02", elements[1].RecordId);
            Assert.AreEqual("sens-01", elements[1].SensorId);
            Assert.AreEqual(128, elements[1].Value);

            Assert.AreEqual(3, elements[2].Id);
            Assert.AreEqual("rec-01", elements[2].RecordId);
            Assert.AreEqual("sens-02", elements[2].SensorId);
            Assert.AreEqual(227, elements[2].Value);

            Assert.AreEqual(4, elements[3].Id);
            Assert.AreEqual("rec-02", elements[3].RecordId);
            Assert.AreEqual("sens-02", elements[3].SensorId);
            Assert.AreEqual(228, elements[3].Value);
        }

        [Test]
        public async Task Should_split_correctly_even_number_of_elements()
        {
            var bulkServiceOptions = new BulkServiceOptions(2);
            bulkServiceOptions.AddEntityProfile<TestEntity>(new UpsertEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
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
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString)) {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual("rec-01", elements[0].RecordId);
            Assert.AreEqual("sens-01", elements[0].SensorId);
            Assert.AreEqual(127, elements[0].Value);

            Assert.AreEqual(2, elements[1].Id);
            Assert.AreEqual("rec-02", elements[1].RecordId);
            Assert.AreEqual("sens-01", elements[1].SensorId);
            Assert.AreEqual(128, elements[1].Value);

            Assert.AreEqual(3, elements[2].Id);
            Assert.AreEqual("rec-01", elements[2].RecordId);
            Assert.AreEqual("sens-02", elements[2].SensorId);
            Assert.AreEqual(227, elements[2].Value);

            Assert.AreEqual(4, elements[3].Id);
            Assert.AreEqual("rec-02", elements[3].RecordId);
            Assert.AreEqual("sens-02", elements[3].SensorId);
            Assert.AreEqual(228, elements[3].Value);
        }

        [Test]
        public async Task Upsert_should_split_correctly_odd_number_of_elements()
        {
            var bulkServiceOptions = new BulkServiceOptions(2);
            bulkServiceOptions.AddEntityProfile<TestEntity>(new UpsertEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
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
                new TestEntity {RecordId = "rec-02", SensorId = "sens-02", Value = 228},
                new TestEntity {RecordId = "rec-03", SensorId = "sens-02", Value = 229}
            };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString)) {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual("rec-01", elements[0].RecordId);
            Assert.AreEqual("sens-01", elements[0].SensorId);
            Assert.AreEqual(127, elements[0].Value);

            Assert.AreEqual(2, elements[1].Id);
            Assert.AreEqual("rec-02", elements[1].RecordId);
            Assert.AreEqual("sens-01", elements[1].SensorId);
            Assert.AreEqual(128, elements[1].Value);

            Assert.AreEqual(3, elements[2].Id);
            Assert.AreEqual("rec-01", elements[2].RecordId);
            Assert.AreEqual("sens-02", elements[2].SensorId);
            Assert.AreEqual(227, elements[2].Value);

            Assert.AreEqual(4, elements[3].Id);
            Assert.AreEqual("rec-02", elements[3].RecordId);
            Assert.AreEqual("sens-02", elements[3].SensorId);
            Assert.AreEqual(228, elements[3].Value);

            Assert.AreEqual(5, elements[4].Id);
            Assert.AreEqual("rec-03", elements[4].RecordId);
            Assert.AreEqual("sens-02", elements[4].SensorId);
            Assert.AreEqual(229, elements[4].Value);
        }
    }
}