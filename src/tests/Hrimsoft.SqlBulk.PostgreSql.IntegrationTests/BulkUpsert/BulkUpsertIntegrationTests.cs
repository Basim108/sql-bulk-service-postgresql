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

        private IPostgreSqlBulkService _testService;
        private BulkServiceOptions     _bulkServiceOptions;

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

            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await using var command    = new NpgsqlCommand($"{truncateTableCmd}{resetIdSequenceCmd}", connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            _bulkServiceOptions = new BulkServiceOptions();
            _bulkServiceOptions.AddEntityProfile<TestEntity>(new UpsertEntityProfile());

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
        public async Task Should_insert_nullable()
        {
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.UpsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual("rec-01", elements[0].RecordId);
            Assert.AreEqual("sens-01", elements[0].SensorId);
            Assert.AreEqual(127, elements[0].Value);
            Assert.IsNull(elements[0].NullableValue);
        }

        [Test]
        public async Task Should_update_nullable()
        {
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127, NullableValue = 12},
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);
            elements.First().NullableValue = null; // this item should be updated
            // This item should be inserted
            await _testService.UpsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual("rec-01", elements[0].RecordId);
            Assert.AreEqual("sens-01", elements[0].SensorId);
            Assert.AreEqual(127, elements[0].Value);
            Assert.IsNull(elements[0].NullableValue);
        }

        [Test]
        public async Task Should_insert_and_update()
        {
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);
            elements.First().Value = 128; // this item should be updated
            // This item should be inserted
            elements.Add(new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 1});
            await _testService.UpsertAsync(connection, elements, CancellationToken.None);

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
        public async Task Should_insert_and_update_more_than_65K()
        {
            const int EXISTED_ITEMS_COUNT = 10_000;
            const int UPSERT_ITEMS_COUNT = 70_000;

            var elements = new List<TestEntity>(EXISTED_ITEMS_COUNT);
            
            for (var i = 0; i < EXISTED_ITEMS_COUNT; i++)
            {
                elements.Add(new TestEntity {RecordId = $"rec-{i}", SensorId = $"sens-{i}", Value = 100 + i});
            }
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

            // these items should be updated
            for (var i = 0; i < EXISTED_ITEMS_COUNT; i++)
            {
                elements[i].Value = 200 + i;
            }
            // these should be inserted
            for (var i = EXISTED_ITEMS_COUNT; i < UPSERT_ITEMS_COUNT; i++)
            {
                elements.Add(new TestEntity {RecordId = $"rec-{i}", SensorId = $"sens-{i}", Value = 200 + i});
            }
            
            await _testService.UpsertAsync(connection, elements, CancellationToken.None);
          
            for (var i = 0; i < UPSERT_ITEMS_COUNT; i++)
            {
                Assert.AreEqual(200 + i, elements[i].Value);
            }
        }

        [Test]
        public async Task Should_update_autogenerated_fields()
        {
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                               new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 128},
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-02", Value = 227},
                               new TestEntity {RecordId = "rec-02", SensorId = "sens-02", Value = 228}
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

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
            _bulkServiceOptions.MaximumSentElements = 2;

            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                               new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 128},
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-02", Value = 227},
                               new TestEntity {RecordId = "rec-02", SensorId = "sens-02", Value = 228}
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

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
            _bulkServiceOptions.MaximumSentElements = 2;

            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                               new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 128},
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-02", Value = 227},
                               new TestEntity {RecordId = "rec-02", SensorId = "sens-02", Value = 228},
                               new TestEntity {RecordId = "rec-03", SensorId = "sens-02", Value = 229}
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

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