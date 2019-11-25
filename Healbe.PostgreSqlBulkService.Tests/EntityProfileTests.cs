using System;
using System.Linq;
using System.Linq.Expressions;
using Healbe.PostgreSqlBulkService.Tests.TestModels;
using Hrimsoft.PostgresSqlBulkService;
using NUnit.Framework;

namespace Healbe.PostgreSqlBulkService.Tests
{
    public class EntityProfileTests
    {
        [Test]
        public void HasProperty_should_not_pass_binary_expressions()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var result = Assert.Throws<ArgumentException>(() => entityProfile.HasProperty<TestEntity, int>("id", x => x.Id + x.Id));

            Assert.AreEqual("propertyExpression", result.ParamName);
            Assert.IsTrue(result.Message.Contains(nameof(BinaryExpression)));
        }

        [Test]
        public void HasProperty_should_not_pass_constant_expressions()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var result = Assert.Throws<ArgumentException>(() => entityProfile.HasProperty<TestEntity, int>("id", x => 10));

            Assert.AreEqual("propertyExpression", result.ParamName);
            Assert.IsTrue(result.Message.Contains(nameof(ConstantExpression)));
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

        [Test]
        public void Test()
        {
            var profile = new SimpleEntityProfile(12);
            var entity = new TestEntity {Id = 12, RecordId = "rec-01", SensorId = "sen-01"};

            var idMemberExpression = profile.Properties.Values.First().PropertyExpresion;
            //var id = Expression.Lambda<Func<TestEntity, int>>(idMemberExpression).Compile().Invoke(entity);
            var convert = Expression.Convert(idMemberExpression, idMemberExpression.Type);
            //var wee = Expression.Lambda<Func<TestEntity, object>>(convert, idMemberExpression.Expression as ParameterExpression);
            var wee = Expression.Lambda(convert, idMemberExpression.Expression as ParameterExpression);
            var id = wee.Compile().DynamicInvoke(entity);    
            
            Assert.AreEqual(12, id);
        }
    }
}