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
    public class WhereOrDeleteCompositeKeyTests
    {
        private readonly TestConfiguration _configuration;
        private readonly TestUtils         _testUtils;

        private EntityWithCompositePkProfile _entityProfile;
        private NpgsqlCommandsBulkService    _testService;

        public WhereOrDeleteCompositeKeyTests()
        {
            _configuration = new TestConfiguration();
            _testUtils     = new TestUtils(_configuration);
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncateTableCmd = "truncate \"unit_tests\".\"entity_with_composite_pk\";";

            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await using var command    = new NpgsqlCommand(truncateTableCmd, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            var bulkServiceOptions = new BulkServiceOptions();
            _entityProfile = new EntityWithCompositePkProfile();
            bulkServiceOptions.AddEntityProfile<TestEntityWithCompositePk>(_entityProfile);

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
            var deleteCommandBuilder = new WhereOrDeleteSqlCommandBuilder(NullLogger<WhereOrDeleteSqlCommandBuilder>.Instance);
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
        public async Task Should_delete_single_item()
        {
            var elements = new List<TestEntityWithCompositePk>
                           {
                               new TestEntityWithCompositePk {UserId = 1, Column2 = "value-2"}
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);
            await _testService.DeleteAsync(connection, elements, CancellationToken.None);

            var countOfRows = await _testUtils.HowManyRowsInTableAsync(_entityProfile);
            Assert.AreEqual(0, countOfRows);
        }

        [Test]
        public async Task Should_delete_several_items()
        {
            var elements = new List<TestEntityWithCompositePk>
                           {
                               new TestEntityWithCompositePk {UserId = 1, Column2 = "value-2"},
                               new TestEntityWithCompositePk {UserId = 2, Column2 = "value-2"},
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);
            await _testService.DeleteAsync(connection, elements, CancellationToken.None);

            var countOfRows = await _testUtils.HowManyRowsInTableAsync(_entityProfile);
            Assert.AreEqual(0, countOfRows);
        }

        [Test]
        public async Task Should_delete_70K_items()
        {
            var elements = new List<TestEntityWithCompositePk>(70_000);
            for (var i = 0; i < 70_000; i++) {
                elements.Add(new TestEntityWithCompositePk
                             {
                                 UserId  = i + 1,
                                 Column2 = $"value-{i}",
                                 Column3 = i + 10
                             });
            }
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);
            await _testService.DeleteAsync(connection, elements, CancellationToken.None);

            var countOfRows = await _testUtils.HowManyRowsInTableAsync(_entityProfile);
            Assert.AreEqual(0, countOfRows);
        }
    }
}