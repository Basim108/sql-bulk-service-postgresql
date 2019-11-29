using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Services
{
    public class InsertSqlCommandBuilderTests
    {
        private InsertSqlCommandBuilder _testService;
        
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
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);

            Assert.IsFalse(command.Contains("sensor_id"));
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
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);

            var pattern =
                @"insert\s+into\s+(""\w+"".)?""\w+""\s*\(\s*""\w+""(,\s*""\w+"")*\s*\)\s*values\s*\(\s*@param\d+(,\s*@param\d+)*\s*\)\s*(,\s*\(\s*@param\d+(,\s*@param\d+)*\s*\))*\s*(returning\s+""\w+""\s*(,\s*""\w+"")*)?;";
            Assert.IsTrue(Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_insert_cmd_of_one_element()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);

            var pattern =
                @"insert\s+into\s+(""\w+"".)?""\w+""\s*\(\s*""\w+""(,\s*""\w+"")*\s*\)\s*values\s*\(\s*@param\d+(,\s*@param\d+)*\s*\)\s*(,\s*\(\s*@param\d+(,\s*@param\d+)*\s*\))*\s*(returning\s+""\w+""\s*(,\s*""\w+"")*)?;";
            Assert.IsTrue(Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_insert_cmd_of_many_elements()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
                new TestEntity{ RecordId = "rec-02", SensorId = "sens-01", Value = 128 },
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-02", Value = 227 },
                new TestEntity{ RecordId = "rec-02", SensorId = "sens-02", Value = 228 }
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);

            var pattern =
@"insert\s+into\s+(""\w+"".)?""\w+""\s*\(\s*""\w+""(,\s*""\w+"")*\s*\)\s*values\s*\(\s*@param\d+(,\s*@param\d+)*\s*\)\s*(,\s*\(\s*@param\d+(,\s*@param\d+)*\s*\))*\s*(returning\s+""\w+""\s*(,\s*""\w+"")*)?;";
            Assert.IsTrue(Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_exclude_autogenerated_columns()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01"}
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);

            Assert.IsFalse(command.Contains("(\"id\""));
        }
        
        [Test]
        public void Should_include_autogenerated_column_into_returning_clause()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01"}
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);
            
            Assert.IsTrue(command.EndsWith("returning \"id\";"));
        }
        
        [Test]
        public void Should_close_command_with_semicolon()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01"}
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);
            
            Assert.IsTrue(command.EndsWith(";"));
        }
        
        [Test]
        public void Should_return_correct_parameters_values()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01", SensorId = "sens-02", Value = 12}
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);
            Assert.NotNull(parameters);
            
            Assert.AreEqual(3, parameters.Count);
            Assert.NotNull(parameters.FirstOrDefault(p => p.Value.ToString() == "rec-01"));
            Assert.NotNull(parameters.FirstOrDefault(p => p.Value.ToString() == "sens-02"));
            Assert.NotNull(parameters.FirstOrDefault(p => p.DbType == DbType.Int32 && (int)p.Value == 12));
        }
        [Test]
        public void Should_return_correct_parameters_types()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01", SensorId = "sens-02", Value = 12}
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);
            Assert.NotNull(parameters);
            
            Assert.AreEqual(3, parameters.Count);
            Assert.AreEqual(2, parameters.Where(p => p.DbType == DbType.String).ToList().Count);
            Assert.NotNull(parameters.FirstOrDefault(p => p.DbType == DbType.Int32));
        }
        
        [Test]
        public void Should_include_table_name()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01"}
            };
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);
            
            Assert.IsTrue(command.ToLowerInvariant().StartsWith("insert into \"unit_tests\".\"simple_test_entity\""));
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
            var (command, parameters) = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(command);
            
            Assert.IsTrue(command.ToLowerInvariant().StartsWith("insert into \"custom\".\"test_entity\""));
        }
    }
}