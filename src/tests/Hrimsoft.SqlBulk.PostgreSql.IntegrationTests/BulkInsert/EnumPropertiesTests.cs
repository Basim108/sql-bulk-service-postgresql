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
    public class EnumPropertiesTests
    {
        private readonly TestConfiguration _configuration;

        public EnumPropertiesTests()
        {
            _configuration = new TestConfiguration();
        }

        [SetUp]
        public async Task SetUp()
        {
            var truncateIntegerEnum   = "truncate \"unit_tests\".\"entity_with_int_enum\";";
            // var truncateStringEnum    = "truncate \"unit_tests\".\"entity_with_str_enum\";";
            var resetIntIdSequenceCmd = "ALTER SEQUENCE \"unit_tests\".\"entity_with_int_enum_id_seq\" RESTART WITH 1;";
            // var resetStrIdSequenceCmd = "ALTER SEQUENCE \"unit_tests\".\"entity_with_str_enum_id_seq\" RESTART WITH 1;";

            await using var connection         = new NpgsqlConnection(_configuration.ConnectionString);
            await using var commandIntegerEnum = new NpgsqlCommand($"{truncateIntegerEnum}{resetIntIdSequenceCmd}", connection);
            // await using var commandStringEnum  = new NpgsqlCommand($"{truncateStringEnum}{resetStrIdSequenceCmd}", connection);
            await connection.OpenAsync();
            await commandIntegerEnum.ExecuteNonQueryAsync();
            // await commandStringEnum.ExecuteNonQueryAsync();
        }

        [Test]
        public async Task Insert_should_set_enum_value_as_integer()
        {
            var bulkServiceOptions = new BulkServiceOptions();
            bulkServiceOptions.AddEntityProfile<TestEntityWithIntEnum>(new EntityWithIntegerEnumProfile());

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

            var elements = new List<TestEntityWithIntEnum>
                           {
                               new TestEntityWithIntEnum {SomeEnumValue = SomeEnum.SomeValue},
                               new TestEntityWithIntEnum {SomeEnumValue = SomeEnum.AnotherValue},
                               new TestEntityWithIntEnum {SomeEnumValue = SomeEnum.SomeValue},
                               new TestEntityWithIntEnum {SomeEnumValue = SomeEnum.AnotherValue}
                           };
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await testService.InsertAsync(connection, elements, CancellationToken.None);

            Assert.AreEqual(1, elements[0].Id);
            Assert.AreEqual(SomeEnum.SomeValue, elements[0].SomeEnumValue);

            Assert.AreEqual(2, elements[1].Id);
            Assert.AreEqual(SomeEnum.AnotherValue, elements[1].SomeEnumValue);

            Assert.AreEqual(3, elements[2].Id);
            Assert.AreEqual(SomeEnum.SomeValue, elements[2].SomeEnumValue);

            Assert.AreEqual(4, elements[3].Id);
            Assert.AreEqual(SomeEnum.AnotherValue, elements[3].SomeEnumValue);
        }
    }
}