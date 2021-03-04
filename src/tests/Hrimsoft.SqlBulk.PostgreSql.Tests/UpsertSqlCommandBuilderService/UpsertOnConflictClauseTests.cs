using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.UpsertSqlCommandBuilderService
{
    public class UpsertOnConflictClauseTests
    {
        private UpsertSqlCommandBuilder _testService;

        [SetUp]
        public void SetUp()
        {
            _testService = new PostgreSql.UpsertSqlCommandBuilder(NullLoggerFactory.Instance);
        }
        
        [Test]
        public void Should_match_column_constraint_in_on_conflict_clause()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey();
            entityProfile.HasPropertyAsPartOfUniqueConstraint<TestEntity, string>(x => x.RecordId);
                
            entityProfile.HasProperty<TestEntity, string>(x => x.SensorId);
            entityProfile.ToTable("test_entity", "custom");

            var elements = new List<TestEntity>
            {
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);

            var onConflictClausePattern = "on\\s+conflict\\s+(\\(\\s*\"\\w+\"\\s*\\)|on\\s+constraint\\s+\"\\w+\")";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, onConflictClausePattern, RegexOptions.IgnoreCase));
        }
        
        [Test]
        public void Should_match_constraint_name_in_on_conflict_clause()
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
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", IntValue = 127 },
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);

            var onConflictClausePattern = "on\\s+conflict\\s+(\\(\\s*\"\\w+\"\\s*\\)|on\\s+constraint\\s+\"\\w+\")";
            Assert.IsTrue(Regex.IsMatch(commandResult.Command, onConflictClausePattern, RegexOptions.IgnoreCase));
        }
    }
}