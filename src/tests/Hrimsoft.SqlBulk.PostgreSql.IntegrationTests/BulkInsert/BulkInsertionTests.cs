using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.Core.ValueTypes;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkInsert
{
    public class BulkInsertIntegrationTests
    {
        private readonly TestConfiguration _configuration;

        private BulkServiceOptions     _bulkServiceOptions;
        private IPostgreSqlBulkService _testService;

        public BulkInsertIntegrationTests()
        {
            _configuration = new TestConfiguration();
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncate1      = "truncate \"unit_tests\".\"bulk_test_entity\";";
            var truncate2      = "truncate \"unit_tests\".\"entity_with_dates\";";
            var resetIdSeqCmd1 = "ALTER SEQUENCE \"unit_tests\".\"bulk_test_entity_id_seq\" RESTART WITH 1;";
            var resetIdSeqCmd2 = "ALTER SEQUENCE \"unit_tests\".\"entity_with_dates_id_seq\" RESTART WITH 1;";

            await using var connection              = new NpgsqlConnection(_configuration.ConnectionString);
            await using var commandSimpleTestEntity = new NpgsqlCommand($"{truncate1}{truncate2}{resetIdSeqCmd1}{resetIdSeqCmd2}", connection);
            await connection.OpenAsync();
            await commandSimpleTestEntity.ExecuteNonQueryAsync();

            _bulkServiceOptions = new BulkServiceOptions();
            _bulkServiceOptions.AddEntityProfile<TestEntity>(new BulkEntityProfile());

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
        public async Task Should_insert_date_types()
        {
            _bulkServiceOptions.AddEntityProfile<EntityWithDates>(new EntityWithDatesProfile());
            var elements = new List<EntityWithDates>
                           {
                               new EntityWithDates {Date = new Date(2021, 02, 01)},
                               new EntityWithDates {Date = new Date(2021, 04, 05)},
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual(new Date(2021, 02, 01), elements[0].Date);

            Assert.AreEqual(2, elements[1].Id);
            Assert.AreEqual(new Date(2021, 04, 05), elements[1].Date);
        }

        [Test]
        public async Task Insert_should_split_correctly_even_number_of_elements()
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
        public async Task Insert_should_split_correctly_odd_number_of_elements()
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

        [Test]
        [Ignore("Should run manually")]
        public void Insert_should_execute_with_more_than_65K_params()
        {
            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<TestEntity>(new SimpleEntityProfile());

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

            var elements = new List<TestEntity>(70_000);
            for (var i = 0; i < 70_000; i++) {
                elements.Add(new TestEntity {RecordId = $"rec-{i}", SensorId = $"sens-{i}", Value = i});
            }
            Assert.DoesNotThrowAsync(async () =>
            {
                await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
                await testService.InsertAsync(connection, elements, CancellationToken.None);
            });
        }
    }
}