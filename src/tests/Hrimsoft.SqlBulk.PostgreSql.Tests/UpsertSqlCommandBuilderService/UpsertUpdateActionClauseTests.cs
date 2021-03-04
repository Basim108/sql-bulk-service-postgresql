using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.UpsertSqlCommandBuilderService
{
    public class UpsertUpdateActionClauseTests
    {
        private UpsertSqlCommandBuilder _testService;

        [SetUp]
        public void SetUp()
        {
            _testService = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);
        }
        
        [Test]
        public void Should_not_put_constraint_columns_into_update_set_clause()
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

            var upsertSetPattern = "(set\\s+\"record_id\"\\s*=\\s*(\"\\w+\".)?\"\\w+\".\"\\w+\"\\s*)|,\\s*\"record_id\"\\s*=";
            Assert.IsFalse(Regex.IsMatch(commandResult.Command, upsertSetPattern, RegexOptions.IgnoreCase));
        }
    }
}