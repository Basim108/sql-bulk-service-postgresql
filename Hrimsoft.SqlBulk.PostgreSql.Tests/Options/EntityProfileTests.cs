using System;
using System.Linq;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Options
{
    public class EntityProfileTests
    {
        [Test]
        public void HasProperty_should_pass_binary_expressions()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            
            Assert.DoesNotThrow(() => entityProfile.HasProperty<TestEntity, int>("id", x => x.Id + x.Value));
            Assert.DoesNotThrow(() => entityProfile.HasProperty<TestEntity, int>("value", x => x.Id + 10));
        }

        [Test]
        public void HasProperty_should_pass_constant_expressions()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            Assert.DoesNotThrow(() 
                => entityProfile.HasProperty<TestEntity, int>("id", x => 10));
        }
        
        [Test]
        public void HasProperty_should_not_pass_constant_expressions_without_column()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var ex = Assert.Throws<ArgumentException>(() => entityProfile.HasProperty<TestEntity, int>(x => 10));
            Assert.AreEqual("column", ex.ParamName);
        }

        [Test]
        public void HasProperty_should_not_pass_duplicates()
        {
            var i = Math.Round((decimal)5 / 2, MidpointRounding.ToPositiveInfinity);
            var entityProfile = new EntityProfile(typeof(TestEntity));
            Assert.DoesNotThrow(() => entityProfile.HasProperty<TestEntity, int>(x => x.Id));
            Assert.Throws<SqlBulkServiceException>(() => entityProfile.HasProperty<TestEntity, int>(x => x.Id));
        }

        
        [Test]
        public void HasProperty_should_pass_member_expressions()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            Assert.DoesNotThrow(() => entityProfile.HasProperty<TestEntity, int>("id", x => x.Id));
        }

        [Test]
        public void HasProperty_should_map_to_snake_cased_column_name()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, string>(x => x.RecordId);

            Assert.IsNotEmpty(entityProfile.Properties);
            Assert.IsNotNull(entityProfile.Properties.First().Value);
            Assert.AreEqual("record_id", (string) entityProfile.Properties.First().Value.DbColumnName);
        }

        [Test]
        public void HasProperty_should_map_to_custom_column_name()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            entityProfile.HasProperty<TestEntity, string>("RecordID", x => x.RecordId);

            Assert.IsNotEmpty(entityProfile.Properties);
            Assert.IsNotNull(entityProfile.Properties.First().Value);
            Assert.AreEqual("RecordID", (string) entityProfile.Properties.First().Value.DbColumnName);
        }
    }
}