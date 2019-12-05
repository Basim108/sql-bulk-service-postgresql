using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NpgsqlTypes;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Services
{
    public class SimpleDeleteSqlCommandBuilderTests
    {
        private SimpleDeleteSqlCommandBuilder _testService;
        private const string DELETE_PATTERN = 
@"(delete\s+from\s+(""\w+"".)?""\w+""\s+where\s+""\w+""\s*=\s*@param\d+\s*(,\s*""\w+""\s*=\s*@param\d+)*\s*;)+";

        [SetUp]
        public void SetUp()
        {
            _testService = new SimpleDeleteSqlCommandBuilder(NullLoggerFactory.Instance);
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
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PATTERN, RegexOptions.IgnoreCase));
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
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
            };
            Assert.Throws<SqlGenerationException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }
        
        [Test]
        public void Should_match_delete_cmd_for_entities_with_returning_clause()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_delete_cmd_of_one_element()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PATTERN, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_update_cmd_of_many_elements()
        {
            var entityProfile = new ReturningEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
                new TestEntity{ RecordId = "rec-02", SensorId = "sens-01", Value = 128 },
                new TestEntity{ RecordId = "rec-01", SensorId = "sens-02", Value = 227 },
                new TestEntity{ RecordId = "rec-02", SensorId = "sens-02", Value = 228 }
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);

            Assert.IsTrue(Regex.IsMatch(commandResult.Command, DELETE_PATTERN, RegexOptions.IgnoreCase));
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
                new TestEntity {Id = 12, RecordId = "rec-01", SensorId = "sen-0"}
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);
            
            // pattern should match "id" whatever builder put the "id" column in any order where "id" = @param1;  or where ""value"=@param1, "id"=@param2;
            var pattern = "where\\s+(\"id\"\\s*=\\s*@param\\d+|\\s*\"record_id\"\\s*=\\s*@param\\d+)\\s*(,\\s*\"id\"\\s*=\\s*@param\\d+|,\\s*\"record_id\"\\s*=\\s*@param\\d+)";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, pattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_return_one_parameters_for_id()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {Id=10, RecordId = "rec-01", SensorId = "sens-02", Value = 12}
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);
            Assert.NotNull(commandResult.Parameters);
            
            Assert.AreEqual(1, commandResult.Parameters.Count);
            Assert.NotNull(commandResult.Parameters.FirstOrDefault(p => p.NpgsqlDbType == NpgsqlDbType.Integer && (int)p.Value == 10));
        }
        
        [Test]
        public void Should_return_two_parameters_for_id_and_value()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey();
            entityProfile.HasProperty<TestEntity, int>(x => x.Value)
                .ThatIsPrivateKey();

            var elements = new List<TestEntity>
            {
                new TestEntity {Id=10, RecordId = "rec-01", SensorId = "sens-02", Value = 12}
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);
            Assert.NotNull(commandResult.Parameters);
            
            Assert.AreEqual(2, commandResult.Parameters.Count);
            Assert.NotNull(commandResult.Parameters.FirstOrDefault(p => p.NpgsqlDbType == NpgsqlDbType.Integer && (int)p.Value == 10));
            Assert.NotNull(commandResult.Parameters.FirstOrDefault(p => p.NpgsqlDbType == NpgsqlDbType.Integer && (int)p.Value == 12));
        }
        
        [Test]
        public void Should_include_scheme_and_table_name()
        {
            var entityProfile = new SimpleEntityProfile();
            var elements = new List<TestEntity>
            {
                new TestEntity {RecordId = "rec-01"}
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
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