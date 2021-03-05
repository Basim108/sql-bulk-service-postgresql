using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests
{
    public class WhereInDeleteSqlCommandBuilderTests
    {
        private WhereInDeleteSqlCommandBuilder _testService;
        private const string DELETE_PURE_PATTERN = 
            @"(delete\s+from\s+(""\w+"".)?""\w+""\s+where\s+""\w+""\s*in\s*\(\d+\s*(,\s*\d+)*\s*\);)+";
        private const string DELETE_PARAM_PATTERN = 
            @"(delete\s+from\s+(""\w+"".)?""\w+""\s+where\s+""\w+""\s*in\s*\(@param_\w+_\d+\s*(,\s*@param_\w+_\d+)*\s*\);)+";

        [SetUp]
        public void SetUp()
        {
            _testService = new WhereInDeleteSqlCommandBuilder(NullLogger<WhereInDeleteSqlCommandBuilder>.Instance);
        }
      
        [Test]
        public void Should_match_delete_cmd_without_returning_clause()
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

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PURE_PATTERN, RegexOptions.IgnoreCase));
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
            Assert.Throws<ArgumentException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }
        
        [Test]
        public void Should_match_delete_cmd_for_entities_with_returning_clause()
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

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PURE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_delete_cmd_of_one_element()
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

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PURE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_delete_cmd_of_many_elements()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ Id=1,RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
                new TestEntity{ Id=2,RecordId = "rec-02", SensorId = "sens-01", IntValue = 128 },
                new TestEntity{ Id=3,RecordId = "rec-01", SensorId = "sens-02", IntValue = 227 },
                new TestEntity{ Id=4,RecordId = "rec-02", SensorId = "sens-02", IntValue = 228 }
            };
            var commands = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commands);
            Assert.AreEqual(1, commands.Count);
            var commandResult = commands.First();
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PURE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_not_generate_cmd_for_entities_with_multiple_pk()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey();
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId)
                .ThatIsPrivateKey();
            var elements = new List<TestEntity>
            {
                new TestEntity {Id = 12, RecordId = "rec-01", SensorId = "sen-0"}
            };
            Assert.Throws<ArgumentException>(()=> _testService.Generate(elements, 
                                                                        entityProfile, 
                                                                        CancellationToken.None));
        }
        
        [Test]
        public void Should_build_correct_id_property_name()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.IntValue);
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId)
                         .ThatIsPrivateKey();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {IntValue = 12, RecordId = "rec-01", SensorId = "sen-0"},
                               new TestEntity {IntValue = 13, RecordId = "rec-02", SensorId = "sen-1"}
                           };
            var result = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(result);
            var commandResult = result.FirstOrDefault();
            Assert.NotNull(commandResult);
            
            Assert.IsTrue(commandResult.Command.Contains("where \"record_id\" in"));
        }
        
        [Test]
        public void Should_build_params_for_string_id()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.IntValue);
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId)
                         .ThatIsPrivateKey();
            var elements = new List<TestEntity>
                           {
                               new TestEntity {IntValue = 12, RecordId = "rec-01", SensorId = "sen-0"},
                               new TestEntity {IntValue = 13, RecordId = "rec-02", SensorId = "sen-1"}
                           };
            var result = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(result);
            var commandResult = result.FirstOrDefault();
            Assert.NotNull(commandResult);
            Assert.NotNull(commandResult.Parameters);
            Assert.AreEqual(2, commandResult.Parameters.Count);
            
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PARAM_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_not_build_params_for_int_id()
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
            Assert.NotNull(commandResult.Parameters);
            Assert.IsEmpty(commandResult.Parameters);
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
            var pattern = @"delete\s+from\s+""unit_tests"".""simple_test_entity""\s+";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_throw_exception_for_empty_elements()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id).ThatIsPrivateKey();
            entityProfile.ToTable("test_entity", "custom");
            var elements = new List<TestEntity>();
            Assert.Throws<ArgumentException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }
        
        [Test]
        public void Should_throw_exception_when_elements_collection_items_are_all_null()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id).ThatIsPrivateKey();
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
    }
}