using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NpgsqlTypes;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests
{
    public class InsertSqlCommandBuilderTests
    {
        private InsertSqlCommandBuilder _testService;

        private const string INSERT_PATTERN =
            @"insert\s+into\s+(""\w+"".)?""\w+""\s*\(\s*""\w+""(,\s*""\w+"")*\s*\)\s*values\s*\(\s*@param_\w+_\d+\s*,\s*@param_\w+_\d+,\s*@param_\w+_\d+\s*\)\s*(,\s*\(\s*@param_\w+_\d+\s*,\s*@param_\w+_\d+,\s*@param_\w+_\d+\s*\))*\s*(returning\s+""\w+""\s*(,\s*""\w+"")*)?;";

        private const string INSERT_ID_PATTERN =
            @"insert\s+into\s+(""\w+"".)?""\w+""\s*\(\s*""\w+""(,\s*""\w+"")*\s*\)\s*values\s*\(\s*\d+\s*,\s*@param_\w+_\d+,\s*@param_\w+_\d+\s*\)\s*(,\s*\(\s*\d+\s*,\s*@param_\w+_\d+,\s*@param_\w+_\d+\s*\))*\s*(returning\s+""\w+""\s*(,\s*""\w+"")*)?;";

        private const string INSERT_VALUE_PATTERN =
            @"insert\s+into\s+(""\w+"".)?""\w+""\s*\(\s*""\w+""(,\s*""\w+"")*\s*\)\s*values\s*\(\s*@param_\w+_\d+,\s*@param_\w+_\d+,\s*\d+\s*\)\s*(,\s*\(\s*@param_\w+_\d+,\s*@param_\w+_\d+,\s*\d+\s*\))*\s*(returning\s+""\w+""\s*(,\s*""\w+"")*)?;";

        [SetUp]
        public void SetUp()
        {
            _testService = new InsertSqlCommandBuilder(NullLoggerFactory.Instance);
        }

        [Test]
        public void Should_not_put_column_that_was_not_mapped()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId);
            entityProfile.ToTable("test_entity", "custom");

            var elements = new List<TestEntity>
                           {
                               new TestEntity {Id = 12, RecordId = "rec-01", SensorId = "sens-01", IntValue = 127},
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);


            Assert.IsFalse(commandResult.Command.Contains("sensor_id"));
        }

        [Test]
        public void Should_match_insert_cmd_without_returning_clause()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId);
            entityProfile.HasProperty<TestEntity, string>(x => x.SensorId);
            entityProfile.ToTable("test_entity", "custom");

            var elements = new List<TestEntity>
                           {
                               new TestEntity {Id = 12, RecordId = "rec-01", SensorId = "sens-01", IntValue = 127},
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, INSERT_ID_PATTERN, RegexOptions.IgnoreCase));
        }

        [Test]
        public void Should_match_insert_cmd_of_one_element()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-01", IntValue = 127},
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, INSERT_VALUE_PATTERN, RegexOptions.IgnoreCase));
        }

        [Test]
        public void Should_match_insert_cmd_of_many_elements()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-01", IntValue = 127},
                               new TestEntity {RecordId = "rec-02", SensorId = "sens-01", IntValue = 128},
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-02", IntValue = 227},
                               new TestEntity {RecordId = "rec-02", SensorId = "sens-02", IntValue = 228}
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, INSERT_VALUE_PATTERN, RegexOptions.IgnoreCase));
        }

        [Test]
        public void Should_exclude_autogenerated_columns()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01"}
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsFalse(commandResult.Command.Contains("(\"id\""));
        }

        [Test]
        public void Should_include_autogenerated_column_into_returning_clause()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01"}
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            // pattern should match "id" whatever builder put the "id" column returning "id";  or returning ""value", "id"; 
            var pattern = @"returning\s+(""\w+""\s*,\s*)*""id""";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));
        }

        [Test]
        public void Should_close_command_with_semicolon()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01"}
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(commandResult.Command.EndsWith(";"));
        }

        [Test]
        public void Should_return_correct_parameters_values()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-02", IntValue = 12}
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);
            Assert.NotNull(commandResult.SqlParameters);

            Assert.AreEqual(2, commandResult.SqlParameters.Count);
            Assert.NotNull(commandResult.SqlParameters.FirstOrDefault(p => p.Value.ToString() == "rec-01"));
            Assert.NotNull(commandResult.SqlParameters.FirstOrDefault(p => p.Value.ToString() == "sens-02"));
        }

        [Test]
        public void Should_return_correct_parameters_types()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01", SensorId = "sens-02", IntValue = 12}
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);
            Assert.NotNull(commandResult.SqlParameters);

            Assert.AreEqual(2, commandResult.SqlParameters.Count);
            Assert.IsTrue(commandResult.SqlParameters.All(p => p.NpgsqlDbType == NpgsqlDbType.Text));
        }

        [Test]
        public void Should_include_table_name()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01"}
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(commandResult.Command.ToLowerInvariant().StartsWith("insert into \"unit_tests\".\"simple_test_entity\""));
        }

        [Test]
        public void Should_include_scheme_and_table_name()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.ToTable("test_entity", "custom");
            var elements = new List<TestEntity>
                           {
                               new TestEntity {RecordId = "rec-01"}
                           };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(commandResult.Command.ToLowerInvariant().StartsWith("insert into \"custom\".\"test_entity\""));
        }

        [Test]
        public void Should_throw_exception_for_empty_elements()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.ToTable("test_entity", "custom");
            var elements = new List<TestEntity>();
            Assert.Throws<ArgumentException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }

        [Test]
        public void Should_throw_exception_when_elements_collection_items_are_all_null()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.ToTable("test_entity", "custom");
            var elements = new List<TestEntity> {null, null};
            Assert.Throws<ArgumentException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }

        [Test]
        public void Should_not_throw_exception_when_at_least_one_item_is_not_null()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.ToTable("test_entity", "custom");
            var elements = new List<TestEntity>
                           {
                               null,
                               new TestEntity {RecordId = "rec-01"},
                               null
                           };
            Assert.DoesNotThrow(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }
    }
}