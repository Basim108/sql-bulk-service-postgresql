using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkInsert
{
    public class NullablePropertiesTests
    {
        private readonly TestConfiguration _configuration;

        private IPostgreSqlBulkService    _testService;
        private EntityWithNullableProfile _profile;

        public NullablePropertiesTests()
        {
            _configuration = new TestConfiguration();
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncateNullableTestEntity = "truncate \"unit_tests\".\"nullable_test_entity\";";
            var resetIdSequenceCmd         = "ALTER SEQUENCE \"unit_tests\".\"nullable_test_entity_id_seq\" RESTART WITH 1;";

            await using var connection              = new NpgsqlConnection(_configuration.ConnectionString);
            await using var commandSimpleTestEntity = new NpgsqlCommand($"{truncateNullableTestEntity}{resetIdSequenceCmd}", connection);
            await connection.OpenAsync();
            await commandSimpleTestEntity.ExecuteNonQueryAsync();

            var bulkServiceOptions = new BulkServiceOptions();
            _profile = new EntityWithNullableProfile();
            bulkServiceOptions.AddEntityProfile<TestEntityWithNullables>(_profile);

            var insertCommandBuilder = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
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
        public async Task Insert_should_work_with_nullable_int_fields()
        {
            _profile.HasProperty<TestEntityWithNullables, int?>(entity => entity.NullableInt);
            var elements = new List<TestEntityWithNullables>
                           {
                               new TestEntityWithNullables()
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.IsNull(elements[0].NullableInt);
        }

        [Test]
        public async Task Insert_should_work_with_nullable_bool_fields()
        {
            _profile.HasProperty<TestEntityWithNullables, bool?>(entity => entity.NullableBool);
            var elements = new List<TestEntityWithNullables>
                           {
                               new TestEntityWithNullables()
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.IsNull(elements[0].NullableBool);
        }

        [Test]
        public async Task Insert_should_work_with_nullable_float_fields()
        {
            _profile.HasProperty<TestEntityWithNullables, float?>(entity => entity.NullableFloat);
            var elements = new List<TestEntityWithNullables>
                           {
                               new TestEntityWithNullables()
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.IsNull(elements[0].NullableFloat);
        }

        [Test]
        public async Task Insert_should_work_with_nullable_double_fields()
        {
            _profile.HasProperty<TestEntityWithNullables, double?>(entity => entity.NullableDouble);
            var elements = new List<TestEntityWithNullables>
                           {
                               new TestEntityWithNullables()
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.IsNull(elements[0].NullableDouble);
        }

        [Test]
        public async Task Insert_should_work_with_nullable_decimal_fields()
        {
            _profile.HasProperty<TestEntityWithNullables, short?>(entity => entity.NullableShort);
            _profile.HasProperty<TestEntityWithNullables, decimal?>(entity => entity.NullableDecimal);
            var elements = new List<TestEntityWithNullables>
                           {
                               new TestEntityWithNullables()
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.IsNull(elements[0].NullableDecimal);
        }

        [Test]
        public async Task Insert_should_work_with_nullable_short_fields()
        {
            _profile.HasProperty<TestEntityWithNullables, short?>(entity => entity.NullableShort);
            var elements = new List<TestEntityWithNullables>
                           {
                               new TestEntityWithNullables()
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await _testService.InsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.IsNull(elements[0].NullableShort);
        }
    }
}