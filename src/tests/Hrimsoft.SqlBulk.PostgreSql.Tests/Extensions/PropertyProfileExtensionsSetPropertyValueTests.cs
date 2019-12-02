using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Extensions
{
    public class PropertyProfileExtensionsSetPropertyValueTests
    {
        [Test]
        public void Should_set_int_value_to_int_property()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo = entityProfile.HasProperty<TestEntity, int>(x => x.Id);
            Assert.NotNull(propInfo);
            
            var item = new TestEntity();
            propInfo.SetPropertyValue(item, 10);
            
            Assert.AreEqual(10, item.Id);
        }
        
        [Test]
        public void Should_set_string_value_to_string_property()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo = entityProfile.HasProperty<TestEntity, string>(x => x.RecordId);
            Assert.NotNull(propInfo);
            
            var item = new TestEntity();
            propInfo.SetPropertyValue(item, "rec-01");
            
            Assert.AreEqual("rec-01", item.RecordId);
        }
    }
}