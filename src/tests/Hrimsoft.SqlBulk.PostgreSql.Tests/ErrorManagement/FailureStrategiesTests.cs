using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.ErrorManagement
{
    public class FailureStrategiesTests
    {
        [Test]
        public void Should_get_option_from_bulk_service_options()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var bulkServiceOptions = new BulkServiceOptions
            {
                FailureStrategy = FailureStrategies.StopEverything
            };
            bulkServiceOptions.AddEntityProfile<TestEntity>(entityProfile);

            var insertCommandBuilder = new Mock<IInsertSqlCommandBuilder>().Object;
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
            var upsertCommandBuilder = new Mock<IUpsertSqlCommandBuilder>().Object;

            var testService = new NpgsqlCommandsBulkService(
                bulkServiceOptions,
                NullLoggerFactory.Instance,
                insertCommandBuilder,
                updateCommandBuilder,
                deleteCommandBuilder,
                upsertCommandBuilder);

            var currentStrategy = testService.GetCurrentFailureStrategy(entityProfile);
            Assert.AreEqual(FailureStrategies.StopEverything, currentStrategy);
        }
        
        [Test]
        public void Should_get_option_from_entity_profile()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity))
            {
                FailureStrategy = FailureStrategies.IgnoreFailure
            };

            var bulkServiceOptions = new BulkServiceOptions
            {
                FailureStrategy = FailureStrategies.StopEverything
            };
            bulkServiceOptions.AddEntityProfile<TestEntity>(entityProfile);

            var insertCommandBuilder = new Mock<IInsertSqlCommandBuilder>().Object;
            var deleteCommandBuilder = new Mock<IDeleteSqlCommandBuilder>().Object;
            var updateCommandBuilder = new Mock<IUpdateSqlCommandBuilder>().Object;
            var upsertCommandBuilder = new Mock<IUpsertSqlCommandBuilder>().Object;

            var testService = new NpgsqlCommandsBulkService(
                bulkServiceOptions,
                NullLoggerFactory.Instance,
                insertCommandBuilder,
                updateCommandBuilder,
                deleteCommandBuilder,
                upsertCommandBuilder);
       
            var currentStrategy = testService.GetCurrentFailureStrategy(entityProfile);
            Assert.AreEqual(FailureStrategies.IgnoreFailure, currentStrategy);

        }
    }
}