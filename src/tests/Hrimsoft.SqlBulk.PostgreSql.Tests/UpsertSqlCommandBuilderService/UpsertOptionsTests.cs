using System;
using System.Collections.Generic;
using System.Threading;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.UpsertSqlCommandBuilderService
{
    public class UpsertOptionsTests
    {
        private UpsertSqlCommandBuilder _testService;
        
        [SetUp]
        public void SetUp()
        {
            _testService = new UpsertSqlCommandBuilder(NullLoggerFactory.Instance);
        }      
        
        [Test]
        public void Upsert_should_not_pass_entities_without_unique_constraint()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.ToTable("test_entity", "custom");
            var elements = new List<TestEntity>();
            Assert.Throws<ArgumentException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }
        
        [Test]
        public void Upsert_should_not_pass_entities_without_unique_properties()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            entityProfile.HasUniqueConstraint("business_identity");
            entityProfile.ToTable("test_entity", "custom");
            var elements = new List<TestEntity>();
            Assert.Throws<ArgumentException>(() => _testService.Generate(elements, entityProfile, CancellationToken.None));
        }

    }
}