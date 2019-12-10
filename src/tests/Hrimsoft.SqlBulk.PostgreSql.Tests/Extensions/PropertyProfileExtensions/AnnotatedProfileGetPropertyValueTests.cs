using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Extensions.PropertyProfileExtensions
{
    public class AnnotatedProfileGetPropertyValueTests
    {
        [Test]
        public void Should_calculate_value_of_an_int_property()
        {
            var entityProfile = new AnnotatedEntityProfile();
            var propInfo = entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            Assert.NotNull(propInfo);
            
            var item = new AnnotatedEntity {Id = 13 };
            var result = propInfo.GetPropertyValue(item);
            
            Assert.AreEqual(13, result);
        }
        
        [Test]
        public void Should_calculate_value_of_a_string_property()
        {
            var entityProfile = new AnnotatedEntityProfile();
            var propInfo = entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            Assert.NotNull(propInfo);
            
            var item = new AnnotatedEntity {Record = "rec-01"};
            var result = propInfo.GetPropertyValue(item);
            
            Assert.AreEqual("rec-01", result);
        }
    }
}