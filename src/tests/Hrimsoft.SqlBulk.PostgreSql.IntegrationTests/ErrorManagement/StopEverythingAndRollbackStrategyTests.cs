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
    public class StopEverythingAndRollbackStrategyTests
    {
        private readonly TestConfiguration _configuration;
        private NpgsqlCommandsBulkService _testService;
        private readonly TestUtils _testUtils;
        private EntityProfile _entityProfile;
        
        public StopEverythingAndRollbackStrategyTests()
        {
            // As upsert command was implemented only in postgre version of 9.5+ 
            _configuration = new TestConfiguration("Postgres_higher_than_9_4");
            _testUtils = new TestUtils(_configuration);
        }

        [SetUp]
        public async Task SetUp()
        {
            _entityProfile = new UpsertEntityProfile(1);
            var truncateTableCmd = $"truncate {_entityProfile.TableName};";
            var resetIdSequenceCmd = "ALTER SEQUENCE \"unit_tests\".\"entity_with_unique_columns_id_seq\" RESTART WITH 1;";
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            using (var command = new NpgsqlCommand($"{truncateTableCmd}{resetIdSequenceCmd}", connection))
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }

            var bulkServiceOptions = new BulkServiceOptions
            {
                FailureStrategy = FailureStrategies.StopEverythingAndRollback
            };
            bulkServiceOptions.AddEntityProfile<TestEntity>(_entityProfile);

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
        public async Task Insert_should_raise_an_exception_on_constraint_conflict()
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
                Assert.ThrowsAsync<SqlBulkExecutionException<TestEntity>>(() => _testService.InsertAsync(connection, elements, CancellationToken.None));
            }
        }
        
        [Test]
        public async Task Exception_should_have_all_collections_properties_null()
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
                var ex = Assert.ThrowsAsync<SqlBulkExecutionException<TestEntity>>(() => _testService.InsertAsync(connection, elements, CancellationToken.None));
                Assert.IsNull(ex.OperatedElements);
                Assert.IsNull(ex.NotOperatedElements);
                Assert.IsNull(ex.ProblemElements);
            }
        }
        
        [Test]
        public async Task Exception_should_have_only_problem_elements()
        {
            var bulkServiceOptions = new BulkServiceOptions
            {
                FailureStrategy = FailureStrategies.StopEverythingAndRollback,
                IsProblemElementsEnabled = true
            };
            bulkServiceOptions.AddEntityProfile<TestEntity>(_entityProfile);

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
                var ex = Assert.ThrowsAsync<SqlBulkExecutionException<TestEntity>>(() => _testService.InsertAsync(connection, elements, CancellationToken.None));
                Assert.IsNull(ex.OperatedElements);
                Assert.IsNull(ex.NotOperatedElements);
                Assert.NotNull(ex.ProblemElements);
                Assert.IsNotEmpty(ex.ProblemElements);
                Assert.NotNull(ex.ProblemElements.First());
                Assert.AreEqual("for conflict purpose", ex.ProblemElements.First().RecordId);
            }
        }

        [Test]
        public async Task Exception_should_have_only_not_operated_elements()
        {
            var bulkServiceOptions = new BulkServiceOptions
            {
                FailureStrategy = FailureStrategies.StopEverythingAndRollback,
                IsNotOperatedElementsEnabled = true
            };
            bulkServiceOptions.AddEntityProfile<TestEntity>(_entityProfile);

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
                var ex = Assert.ThrowsAsync<SqlBulkExecutionException<TestEntity>>(() => _testService.InsertAsync(connection, elements, CancellationToken.None));
                
                Assert.IsNull(ex.OperatedElements);
                Assert.NotNull(ex.NotOperatedElements);
                Assert.IsNull(ex.ProblemElements);
                
                Assert.IsNotEmpty(ex.NotOperatedElements);
                Assert.AreEqual(2, ex.NotOperatedElements.Count);
                Assert.AreEqual("for conflict purpose", ex.NotOperatedElements.First().RecordId);
                Assert.AreEqual("rec-0", ex.NotOperatedElements.Skip(1).Take(1).First().RecordId);
            }
        }
        
        [Test]
        public async Task Exception_should_have_all_collections_not_null()
        {
            var bulkServiceOptions = new BulkServiceOptions
            {
                FailureStrategy = FailureStrategies.StopEverythingAndRollback,
                IsNotOperatedElementsEnabled = true,
                IsOperatedElementsEnabled = true,
                IsProblemElementsEnabled = true
            };
            bulkServiceOptions.AddEntityProfile<TestEntity>(_entityProfile);

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
                var ex = Assert.ThrowsAsync<SqlBulkExecutionException<TestEntity>>(() => _testService.InsertAsync(connection, elements, CancellationToken.None));
                
                Assert.NotNull(ex.OperatedElements);
                Assert.NotNull(ex.NotOperatedElements);
                Assert.NotNull(ex.ProblemElements);
                
                Assert.IsEmpty(ex.OperatedElements);
                
                Assert.IsNotEmpty(ex.NotOperatedElements);
                Assert.AreEqual(2, ex.NotOperatedElements.Count);
                Assert.AreEqual("for conflict purpose", ex.NotOperatedElements.First().RecordId);
                Assert.AreEqual("rec-0", ex.NotOperatedElements.Skip(1).Take(1).First().RecordId);
                
                Assert.IsNotEmpty(ex.ProblemElements);
                Assert.AreEqual(1, ex.ProblemElements.Count);
                Assert.AreEqual("for conflict purpose", ex.ProblemElements.First().RecordId);
            }
        }
        
        [Test]
        public async Task Insert_should_rollback_previous_successfully_operated_portions()
        {
            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "for conflict purpose", SensorId = "for conflict purpose", Value = 1},
                new TestEntity {RecordId = "for conflict purpose", SensorId = "for conflict purpose", Value = 2},
                new TestEntity {RecordId = "should not violate a constraint", SensorId = "but should not be in the storage", Value = 3}
            };

            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                // As bulkServiceOptions.MaximumSentElements = 1, insert will split elements on three portions
                // the first portion will be successfully inserted
                // the second portion will violate a constraint
                // the third portion should not be operated as well
                Assert.ThrowsAsync<SqlBulkExecutionException<TestEntity>>( () => _testService.InsertAsync(connection, elements, CancellationToken.None));
            }

            var countOfRows = await _testUtils.HowManyRowsInTableAsync(_entityProfile);
            Assert.AreEqual(0, countOfRows);
        }
    }
}