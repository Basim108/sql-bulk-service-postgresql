using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Extensions.PropertyProfileExtensions
{
    public class CommonGetPropertyValueTests
    {
        [Test]
        public void Should_calculate_value_of_an_int_property()
        {
            var entityProfile = new SimpleEntityProfile();
            var propInfo = entityProfile.Properties[nameof(TestEntity.Id)];
            Assert.NotNull(propInfo);
            
            var item = new TestEntity {Id = 13 };
            var result = propInfo.GetPropertyValue(item);
            
            Assert.AreEqual(13, result);
        }
        
        [Test]
        public void Should_calculate_value_of_a_string_property()
        {
            var entityProfile = new SimpleEntityProfile();
            var propInfo = entityProfile.Properties[nameof(TestEntity.RecordId)];
            Assert.NotNull(propInfo);
            
            var item = new TestEntity {RecordId = "rec-01"};
            var result = propInfo.GetPropertyValue(item);
            
            Assert.AreEqual("rec-01", result);
        }
        
        [Test]
        public void Should_calculate_value_of_a_constant_int_property()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo = entityProfile.HasProperty<TestEntity, int>("value", x => 10);
            Assert.NotNull(propInfo);
            
            var result = propInfo.GetPropertyValue<TestEntity>(null);
            Assert.AreEqual(10, result);
        }
        
        [Test]
        public void Should_calculate_value_of_an_int_binary_expression_property()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo = entityProfile.HasProperty<TestEntity, int>("id", x => x.Id + 10);
            Assert.NotNull(propInfo);
            
            var item = new TestEntity {Id = 13 };
            
            var result = propInfo.GetPropertyValue(item);
            Assert.AreEqual(23, result);
            
            propInfo = entityProfile.HasProperty<TestEntity, int>("value", x => x.Id + x.Id);
            Assert.NotNull(propInfo);
            result = propInfo.GetPropertyValue(item);
            Assert.AreEqual(26, result);
        }
        
        [Test]
        public void Should_calculate_value_of_a_string_binary_expression_property()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo = entityProfile.HasProperty<TestEntity, string>("record_id", x => x.RecordId + "-const-str");
            Assert.NotNull(propInfo);
            
            var item = new TestEntity {RecordId = "rec-01", SensorId = "sens-01"};
            
            var result = propInfo.GetPropertyValue(item);
            Assert.AreEqual("rec-01-const-str", result);
            
            propInfo = entityProfile.HasProperty<TestEntity, string>("value", x => x.RecordId + x.SensorId);
            Assert.NotNull(propInfo);
            result = propInfo.GetPropertyValue(item);
            Assert.AreEqual("rec-01sens-01", result);
        }
        
        [Test]
        public void Should_calculate_value_of_a_string_interpolation_expresion()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo = entityProfile.HasProperty<TestEntity, string>("identity", x => $"{x.RecordId}-{x.SensorId}");
            Assert.NotNull(propInfo);
            
            var item = new TestEntity {RecordId = "rec-01", SensorId = "sens-01"};
            
            var result = propInfo.GetPropertyValue(item);
            Assert.AreEqual("rec-01-sens-01", result);
        }
        
        [Test]
        public void Should_calculate_value_of_a_function_call_expresion()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo = entityProfile.HasProperty<TestEntity, string>("identity", x => x.RecordId.ToUpperInvariant());
            Assert.NotNull(propInfo);
            
            var item = new TestEntity {RecordId = "rec-01", SensorId = "sens-01"};
            
            var result = propInfo.GetPropertyValue(item);
            Assert.AreEqual("REC-01", result);
        }
    }
}