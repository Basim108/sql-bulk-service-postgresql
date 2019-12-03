using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkUpdate
{
    public class BulkUpdateIntegrationTests
    {
        private readonly TestConfiguration _configuration;

        public BulkUpdateIntegrationTests()
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

        [Test]
        public async Task Update_should_update_autogenerated_fields()
        {
            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<TestEntity>(new SimpleEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new UpdateSqlCommandBuilder(NullLoggerFactory.Instance);

            var testService = new NpgsqlCommandsBulkService(bulkServiceOptions, NullLoggerFactory.Instance, insertCommandBuilder, updateCommandBuilder, deleteCommandBuilder);

            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01", SensorId = "sens-01", Value = 127},
                new TestEntity {RecordId = "rec-02", SensorId = "sens-01", Value = 128}
            };
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.InsertAsync(connection, elements, CancellationToken.None);
            }
            
            var firstId = elements[0].Id;
            elements[0].Value = 1;
            var secondId = elements[1].Id;
            elements[1].Value = 2;

            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.UpdateAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(firstId, elements[0].Id);
            Assert.AreEqual(1, elements[0].Value);
            
            Assert.AreEqual(secondId, elements[1].Id);
            Assert.AreEqual(2, elements[1].Value);
        }
        
        [Test]
        public async Task Update_should_split_correctly_even_number_of_elements()
        {
            var bulkServiceOptions = new BulkServiceOptions(2);
            bulkServiceOptions.AddEntityProfile<TestEntity>(new SimpleEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new UpdateSqlCommandBuilder(NullLoggerFactory.Instance);

            var testService = new NpgsqlCommandsBulkService(bulkServiceOptions, NullLoggerFactory.Instance, insertCommandBuilder, updateCommandBuilder, deleteCommandBuilder);

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

            var firstId = elements[0].Id;
            elements[0].Value = 1;
            var secondId = elements[1].Id;
            elements[1].Value = 2;
            var thirdId = elements[2].Id;
            elements[2].Value = 3;
            var fourthId = elements[3].Id;
            elements[3].Value = 4;
            
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.UpdateAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(firstId, elements[0].Id);
            Assert.AreEqual(1, elements[0].Value);
            
            Assert.AreEqual(secondId, elements[1].Id);
            Assert.AreEqual(2, elements[1].Value);
            
            Assert.AreEqual(thirdId, elements[2].Id);
            Assert.AreEqual(3, elements[2].Value);
            
            Assert.AreEqual(fourthId, elements[3].Id);
            Assert.AreEqual(4, elements[3].Value);        
        }
        
        [Test]
        public async Task Update_should_split_correctly_odd_number_of_elements()
        {
            var bulkServiceOptions = new BulkServiceOptions(2);
            bulkServiceOptions.AddEntityProfile<TestEntity>(new SimpleEntityProfile());

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new UpdateSqlCommandBuilder(NullLoggerFactory.Instance);

            var testService = new NpgsqlCommandsBulkService(bulkServiceOptions, NullLoggerFactory.Instance, insertCommandBuilder, updateCommandBuilder, deleteCommandBuilder);

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


            var firstId = elements[0].Id;
            elements[0].Value = 1;
            var secondId = elements[1].Id;
            elements[1].Value = 2;
            var thirdId = elements[2].Id;
            elements[2].Value = 3;
            
            using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
            {
                await testService.UpdateAsync(connection, elements, CancellationToken.None);
            }

            Assert.AreEqual(firstId, elements[0].Id);
            Assert.AreEqual(1, elements[0].Value);
            
            Assert.AreEqual(secondId, elements[1].Id);
            Assert.AreEqual(2, elements[1].Value);
            
            Assert.AreEqual(thirdId, elements[2].Id);
            Assert.AreEqual(3, elements[2].Value);
        }
    }
}