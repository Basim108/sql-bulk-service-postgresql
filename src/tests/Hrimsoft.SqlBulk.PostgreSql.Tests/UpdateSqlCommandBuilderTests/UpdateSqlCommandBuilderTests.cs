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
    public class UpdateSqlCommandBuilderTests
    {
        private UpdateSqlCommandBuilder _testService;
        private const string UPDATE_PATTERN = 
@"(update\s+(""\w+"".)?""\w+""\s+set\s+""\w+""\s*=\s*@param_\w+_\d+(,""\w+""\s*=\s*@param_\w+_\d+)*\s*(,""\w+""\s*=\s*\d+)*\s+where\s+""\w+""\s*=\s*\d+(\s+returning\s+""\w+""\s*(,\s*""\w+"")*)?\s*;?)*";

        [SetUp]
        public void SetUp()
        {
            _testService = new UpdateSqlCommandBuilder(NullLoggerFactory.Instance);
        }

        [Test]
        public void Should_not_put_column_that_was_not_mapped()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                         .ThatIsPrivateKey();
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId);
            entityProfile.ToTable("test_entity", "custom");

            var elements = new List<TestEntity>
            {
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
            };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsFalse(commandResult.Command.Contains("sensor_id"));
        }
        
        [Test]
        public void Should_match_update_cmd_without_returning_clause()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                         .ThatIsPrivateKey();
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId);
            entityProfile.HasProperty<TestEntity, string>(x => x.SensorId);
            entityProfile.ToTable("test_entity", "custom");

            var elements = new List<TestEntity>
            {
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
            };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, UPDATE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_not_allow_generate_cmd_without_private_key()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId);
            entityProfile.HasProperty<TestEntity, string>(x => x.SensorId);
            entityProfile.ToTable("test_entity", "custom");

            var elements = new List<TestEntity>
            {
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
            };
            Assert.Throws<SqlGenerationException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }
        
        [Test]
        public void Should_match_update_cmd_with_returning_clause()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
            };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, UPDATE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_update_cmd_of_one_element()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
            };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, UPDATE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_update_cmd_of_many_elements()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
                new TestEntity{ RecordId = "rec-02", SensorId = "sens-01", IntValue = 128 },
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-02", IntValue = 227 },
                new TestEntity{ RecordId = "rec-02", SensorId = "sens-02", IntValue = 228 }
            };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, UPDATE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_exclude_autogenerated_columns_from_set_clause()
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
            var pattern = @"set\s+(""\w+""\s*=\s*@param\d+\s*,\s*)*""id""\s*=";
            Assert.IsFalse(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_exclude_autogenerated_columns_from_returning_clause()
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
            
            // pattern should not match "id" whatever builder put the "id" column returning "id";  or returning ""value", "id";
            var pattern = @"returning\s+(""\w+""\s*,\s*)*""id""";
            Assert.IsFalse(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));

        }
        
        [Test]
        public void Should_put_private_key_into_where_clause()
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
            
            // pattern should match "id" whatever builder put the "id" column in any order where "id" = @param1;  or where ""value"=@param1, "id"=@param2;
            var pattern = @"where\s+(""\w+""\s*=\s*@param\d+\s*,\s*)*""id""\s*=";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_put_only_private_keys_into_where_clause()
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
            
            // pattern should match "id" whatever builder put the "id" column in any order where "id" = @param1;  or where ""value"=@param1, "id"=@param2;
            var pattern = @"where\s+(""\w+""\s*=\s*@param\d+\s*,\s*)*""{0}""\s*=";
            
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, string.Format(pattern, "id"), RegexOptions.IgnoreCase));
            Assert.IsFalse(Regex.IsMatch(commandResult.Command, string.Format(pattern, "value"), RegexOptions.IgnoreCase));
            Assert.IsFalse(Regex.IsMatch(commandResult.Command, string.Format(pattern, "record_id"), RegexOptions.IgnoreCase));
            Assert.IsFalse(Regex.IsMatch(commandResult.Command, string.Format(pattern, "sensor_id"), RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_not_include_after_insert_columns_into_update_returning_clause()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey()
                .MustBeUpdatedAfterInsert();
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
            Assert.IsFalse(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_return_correct_parameters_values()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {Id=10, RecordId = "rec-01", SensorId = "sens-02", IntValue = 12}
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
            Assert.AreEqual(0, commandResult.SqlParameters.Where(p => p.NpgsqlDbType == NpgsqlDbType.Integer).ToList().Count);
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
            Assert.AreEqual(2, commandResult.SqlParameters.Where(p => p.NpgsqlDbType == NpgsqlDbType.Text).ToList().Count);
            Assert.AreEqual(0, commandResult.SqlParameters.Where(p => p.NpgsqlDbType == NpgsqlDbType.Integer).ToList().Count);
        }
        
        [Test]
        public void Should_include_scheme_and_table_name()
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
            var pattern = @"update\s+""unit_tests"".""simple_test_entity""\s+set";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));
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
            var elements = new List<TestEntity> { null, null };
            Assert.Throws<ArgumentException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }
        
        [Test]
        public void Should_not_throw_exception_when_at_least_one_item_is_not_null()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                         .ThatIsPrivateKey();
            entityProfile.ToTable("test_entity", "custom");
            var elements = new List<TestEntity>
            {
                null,
                new TestEntity {RecordId = "rec-01"},
                null
            };
            Assert.DoesNotThrow(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }
        
        [Test]
        public void Should_put_all_private_keys_into_where_clause()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey();
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId)
                .ThatIsPrivateKey();
            var elements = new List<TestEntity>
            {
                new TestEntity {Id = 12, RecordId = "rec-01", SensorId = "sens-01"}
            };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);
            
            // pattern should match "id" whatever builder put the "id" column in any order where "id" = @param1;  or where ""value"=@param1, "id"=@param2;
            var pattern = "where\\s+(\"id\"\\s*=\\s*\\d+|\\s*\"record_id\"\\s*=\\s*@param_\\w+_\\d+)\\s*(,\\s*\"id\"\\s*=\\s*\\d+|,\\s*\"record_id\"\\s*=\\s*@param_\\w+_\\d+)";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));
        }
    }
}