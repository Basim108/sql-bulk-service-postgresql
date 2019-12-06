using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.ErrorManagement
{
    public class IgnoreStrategyTests
    {
        private readonly TestConfiguration _configuration;
        private NpgsqlCommandsBulkService _testService;
        private readonly TestUtils _testUtils;

        public IgnoreStrategyTests()
        {
            // As upsert command was implemented only in postgre version of 9.5+ 
            _configuration = new TestConfiguration("Postgres_higher_than_9_4");
            _testUtils = new TestUtils(_configuration);
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncateTableCmd = "truncate \"unit_tests\".\"entity_with_unique_columns\";";
            var resetIdSequenceCmd = "ALTER SEQUENCE \"unit_tests\".\"entity_with_unique_columns_id_seq\" RESTART WITH 1;";
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            using (var command = new NpgsqlCommand($"{truncateTableCmd}{resetIdSequenceCmd}", connection))
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }

            var bulkServiceOptions = new BulkServiceOptions
            {
                FailureStrategy = FailureStrategies.IgnoreFailure
            };
            bulkServiceOptions.AddEntityProfile<TestEntity>(new UpsertEntityProfile(1));

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new SimpleDeleteSqlCommandBuilder(NullLoggerFactory.Instance);
            var updateCommandBuilder = new UpdateSqlCommandBuilder(NullLoggerFactory.Instance);
            var upsertCommandBuilder = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);

            _testService = new NpgsqlCommandsBulkService(
                bulkServiceOptions,
                NullLoggerFactory.Instance,
                insertCommandBuilder,
                updateCommandBuilder,
                deleteCommandBuilder,
                upsertCommandBuilder);
        }

        [Test]
        public async Task Insert_should_ignore_constraint_conflict_on_first_portion()
        {
            var firstItem = new TestEntity
            {
                RecordId = "for conflict purpose", SensorId = "for conflict purpose", Value = 10
            };
            var elements = new List<TestEntity> {firstItem};

            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                var notOperatedElements = await _testService.InsertAsync(connection, elements, CancellationToken.None);
                Assert.IsEmpty(notOperatedElements);

                var secondItem = new TestEntity
                {
                    RecordId = "rec-0", SensorId = "sens-01", Value = 100
                };
                elements.Add(secondItem);
                notOperatedElements = await _testService.InsertAsync(connection, elements, CancellationToken.None);

                // id = 3 because insert firstly calls a sequence gets 2 and then has a constraint violation on columns RecordId and SensorId.
                // Then it processes the second element and again calls sequence so gets 3;
                Assert.AreEqual(3, secondItem.Id);
                Assert.IsNotEmpty(notOperatedElements);
                Assert.AreEqual(1, notOperatedElements.Count);
                Assert.AreEqual("for conflict purpose", notOperatedElements.First().RecordId);
            }
        }

        /// <summary>
        /// When in one portion which is contains two elements, the first element was successfully inserted
        /// and insertion of the second element violates a constraint, so the first element of that portion should be rollbacked!
        /// </summary>
        [Test(Description = "When in one portion which is contains two elements, the first element was successfully inserted and insertion of the second element violates a constraint, so the first element of that portion should be rollbacked!")]
        public async Task Insert_should_rollback_items_that_were_successfully_operated_in_one_portion()
        {
            var bulkServiceOptions = new BulkServiceOptions
            {
                FailureStrategy = FailureStrategies.IgnoreFailure
            };
            var entityProfile = new UpsertEntityProfile(2);
            bulkServiceOptions.AddEntityProfile<TestEntity>(entityProfile);

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new SimpleDeleteSqlCommandBuilder(NullLoggerFactory.Instance);
            var updateCommandBuilder = new UpdateSqlCommandBuilder(NullLoggerFactory.Instance);
            var upsertCommandBuilder = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);

            _testService = new NpgsqlCommandsBulkService(
                bulkServiceOptions,
                NullLoggerFactory.Instance,
                insertCommandBuilder,
                updateCommandBuilder,
                deleteCommandBuilder,
                upsertCommandBuilder);

            var firstItem = new TestEntity {RecordId = "for conflict purpose", SensorId = "for conflict purpose", Value = 10};
            var secondItem = new TestEntity {RecordId = "for conflict purpose", SensorId = "for conflict purpose", Value = 100};
            var elements = new List<TestEntity> {firstItem, secondItem};

            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                var notOperatedElements = await _testService.InsertAsync(connection, elements, CancellationToken.None);
                Assert.IsNotEmpty(notOperatedElements);
                Assert.AreEqual(2, notOperatedElements.Count);
            }

            var countOfRows = await _testUtils.HowManyRowsInTableAsync(entityProfile);
            Assert.AreEqual(0, countOfRows);
        }
    }
}