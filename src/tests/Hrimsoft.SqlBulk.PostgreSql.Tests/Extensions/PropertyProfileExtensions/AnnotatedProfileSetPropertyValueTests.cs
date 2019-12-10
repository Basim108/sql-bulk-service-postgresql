using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Extensions.PropertyProfileExtensions
{
    public class AnnotatedProfileSetPropertyValueTests
    {
        [Test]
        public void Should_set_int_value_to_int_property()
        {
            var entityProfile = new AnnotatedEntityProfile();
            
            var item = new AnnotatedEntity();
            var propInfo = entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            propInfo.SetPropertyValue(item, 10);
            
            Assert.AreEqual(10, item.Id);
        }
        
        [Test]
        public void Should_set_string_value_to_string_property()
        {
            var entityProfile = new AnnotatedEntityProfile();
            
            var item = new AnnotatedEntity();
            var propInfo = entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            propInfo.SetPropertyValue(item, "rec-01");
            
            Assert.AreEqual("rec-01", item.Record);
        }
    }
}