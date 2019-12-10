using Hrimsoft.SqlBulk.PostgreSql;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using NpgsqlTypes;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Options
{
    public class DataAnnotationMappingTests
    {
        private AnnotatedEntityProfile _entityProfile;

        [SetUp]
        public void SetUp()
        {
            _entityProfile = new AnnotatedEntityProfile();   
        }
        
        [Test]
        public void Should_set_schema_and_table_name()
        {
            Assert.AreEqual("\"unit_tests\".\"annotated_entities\"", _entityProfile.TableName);
        }
        
        [Test]
        public void Should_create_property_profile_foreach_property()
        {
            Assert.IsTrue(_entityProfile.Properties.ContainsKey(nameof(AnnotatedEntity.Id)));
            Assert.IsTrue(_entityProfile.Properties.ContainsKey(nameof(AnnotatedEntity.Record)));
            Assert.IsTrue(_entityProfile.Properties.ContainsKey(nameof(AnnotatedEntity.SensorId)));
            Assert.IsTrue(_entityProfile.Properties.ContainsKey(nameof(AnnotatedEntity.Value)));
            Assert.IsFalse(_entityProfile.Properties.ContainsKey(nameof(AnnotatedEntity.AbstractValue)));
        }
        
        [Test]
        public void Should_ignore_not_mapped_properties()
        {
            Assert.IsFalse(_entityProfile.Properties.ContainsKey(nameof(AnnotatedEntity.AbstractValue)));
        }
        
        [Test]
        public void Should_set_correct_column_name()
        {
            var propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            Assert.AreEqual("id", propertyProfile.DbColumnName);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            Assert.AreEqual("record", propertyProfile.DbColumnName);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.SensorId)];
            Assert.AreEqual("sensor_id", propertyProfile.DbColumnName);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Value)];
            Assert.AreEqual("value", propertyProfile.DbColumnName);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnInsert)];
            Assert.AreEqual("value_generated_on_insert", propertyProfile.DbColumnName);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnUpdate)];
            Assert.AreEqual("value_generated_on_update", propertyProfile.DbColumnName);
        }
        
        [Test]
        public void Should_set_correct_column_types()
        {
            var propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            Assert.AreEqual(NpgsqlDbType.Integer, propertyProfile.DbColumnType);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            Assert.AreEqual(NpgsqlDbType.Text, propertyProfile.DbColumnType);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.SensorId)];
            Assert.AreEqual(NpgsqlDbType.Text, propertyProfile.DbColumnType);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Value)];
            Assert.AreEqual(NpgsqlDbType.Bigint, propertyProfile.DbColumnType);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnInsert)];
            Assert.AreEqual(NpgsqlDbType.Bigint, propertyProfile.DbColumnType);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnUpdate)];
            Assert.AreEqual(NpgsqlDbType.Bigint, propertyProfile.DbColumnType);

        }
        
        [Test]
        public void Should_set_private_key()
        {
            var propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            Assert.IsTrue(propertyProfile.IsPrivateKey);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            Assert.IsFalse(propertyProfile.IsPrivateKey);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.SensorId)];
            Assert.IsFalse(propertyProfile.IsPrivateKey);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Value)];
            Assert.IsFalse(propertyProfile.IsPrivateKey);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnInsert)];
            Assert.IsFalse(propertyProfile.IsPrivateKey);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnUpdate)];
            Assert.IsFalse(propertyProfile.IsPrivateKey);
        }
        
        [Test]
        public void Should_set_auto_generated_flag()
        {
            var propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            Assert.IsTrue(propertyProfile.IsAutoGenerated);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            Assert.IsFalse(propertyProfile.IsAutoGenerated);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.SensorId)];
            Assert.IsFalse(propertyProfile.IsAutoGenerated);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Value)];
            Assert.IsFalse(propertyProfile.IsAutoGenerated);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnInsert)];
            Assert.IsFalse(propertyProfile.IsAutoGenerated);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnUpdate)];
            Assert.IsFalse(propertyProfile.IsAutoGenerated);
        }
        
        [Test]
        public void Should_set_update_after_insert_flag()
        {
            var propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            Assert.IsTrue(propertyProfile.IsUpdatedAfterInsert);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnInsert)];
            Assert.IsTrue(propertyProfile.IsUpdatedAfterInsert);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterInsert);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.SensorId)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterInsert);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Value)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterInsert);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnUpdate)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterInsert);
        }
        
        [Test]
        public void Should_set_update_after_update_flag()
        {
            var propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterUpdate);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnInsert)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterUpdate);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterUpdate);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.SensorId)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterUpdate);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Value)];
            Assert.IsFalse(propertyProfile.IsUpdatedAfterUpdate);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnUpdate)];
            Assert.IsTrue(propertyProfile.IsUpdatedAfterUpdate);
        }
        
        [Test]
        public void Should_set_part_of_unique_constraint_flag()
        {
            var propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Record)];
            Assert.IsTrue(propertyProfile.IsPartOfUniqueConstraint);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.SensorId)];
            Assert.IsTrue(propertyProfile.IsPartOfUniqueConstraint);

            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Id)];
            Assert.IsFalse(propertyProfile.IsPartOfUniqueConstraint);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnInsert)];
            Assert.IsFalse(propertyProfile.IsPartOfUniqueConstraint);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.Value)];
            Assert.IsFalse(propertyProfile.IsPartOfUniqueConstraint);
            
            propertyProfile = _entityProfile.Properties[nameof(AnnotatedEntity.ValueGeneratedOnUpdate)];
            Assert.IsFalse(propertyProfile.IsPartOfUniqueConstraint);
        }
    }
}