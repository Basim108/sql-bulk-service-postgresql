using System.Collections.Generic;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.UpsertSqlCommandBuilderService
{
    public class UpsertColumnClauseTests
    {
        private UpsertSqlCommandBuilder _testService;

        [SetUp]
        public void SetUp()
        {
            _testService = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);
        }
        
        [Test]
        public void Should_not_put_column_that_was_not_mapped()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id)
                .ThatIsPrivateKey();
            entityProfile.HasPropertyAsPartOfUniqueConstraint<TestEntity, string>(x => x.RecordId);
            entityProfile.HasUniqueConstraint("business_identity");
            entityProfile.ToTable("test_entity", "custom");

            var elements = new List<TestEntity>
            {
                new TestEntity{ Id=12, RecordId = "rec-01", SensorId = "sens-01", Value = 127 },
            };
            var commandResult = _testService.Generate(elements, entityProfile, CancellationToken.None);
            Assert.NotNull(commandResult.Command);

            Assert.IsFalse(commandResult.Command.Contains("sensor_id"));
        }
    }
}