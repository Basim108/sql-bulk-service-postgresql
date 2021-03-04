using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.UpsertSqlCommandBuilderService
{
    public class UpsertReturningClauseTests
    {
        private UpsertSqlCommandBuilder _testService;


        [SetUp]
        public void SetUp()
        {
            _testService = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);
        }

        [Test]
        public void Should_match_insert_cmd_without_returning_clause()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey();
            entityProfile.HasPropertyAsPartOfUniqueConstraint<TestEntity, string>(x => x.RecordId);
            entityProfile.HasUniqueConstraint("business_identity");

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
            
            // Check that the whole command still matches the upsert pattern
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, UpsertConsts.UPSERT_PATTERN, RegexOptions.IgnoreCase));
            
            // Check that there is no returning clause in the result command
            Assert.IsFalse(commandResult.Command.Contains("returning"));
        }

        [Test]
        public void Should_match_insert_cmd_with_returning_clause()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey();
            entityProfile.HasPropertyAsPartOfUniqueConstraint<TestEntity, string>(x => x.RecordId);
            entityProfile.HasUniqueConstraint("business_identity");

            entityProfile.HasProperty<TestEntity, int>(x => x.IntValue)
                .MustBeUpdatedAfterInsert();

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

            // Check that the whole command still matches the upsert pattern
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, UpsertConsts.UPSERT_PATTERN, RegexOptions.IgnoreCase));

            // Check that there is a returning clause in the result command
            var returningClausePattern = @"returning\s*""int_value""\s*;";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, returningClausePattern, RegexOptions.IgnoreCase));
        }

        [Test]
        public void Should_not_duplicate_columns_in_returning_clause()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey();
            entityProfile.HasPropertyAsPartOfUniqueConstraint<TestEntity, string>(x => x.RecordId);
            entityProfile.HasUniqueConstraint("business_identity");

            entityProfile.HasProperty<TestEntity, int>(x => x.IntValue)
                .MustBeUpdatedAfterInsert()
                .MustBeUpdatedAfterUpdate();

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

            // Check that the whole command still matches the upsert pattern
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, UpsertConsts.UPSERT_PATTERN, RegexOptions.IgnoreCase));

            // Check that there is a returning clause in the result command
            var returningClausePattern = @"returning\s*""int_value""\s*;";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, returningClausePattern, RegexOptions.IgnoreCase));
        }
    }
}