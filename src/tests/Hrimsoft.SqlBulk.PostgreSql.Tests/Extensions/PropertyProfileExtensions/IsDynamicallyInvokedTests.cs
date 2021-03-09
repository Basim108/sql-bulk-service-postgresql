using System;
using Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Extensions.PropertyProfileExtensions
{
    public class IsDynamicallyInvokedTests
    {
        [Test]
        public void Should_return_true_given_int_member()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo = entityProfile.HasProperty<TestEntity, int>(entity => entity.IntValue);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_nullable_int_member()
        {
            var entityProfile = new EntityProfile(typeof(EntityWithNullableProperty));
            var propInfo = entityProfile.HasProperty<EntityWithNullableProperty, int?>(entity => entity.NullableInt);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_bool_member()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo      = entityProfile.HasProperty<TestEntity, bool>(entity => entity.BoolValue);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_nullable_bool_member()
        {
            var entityProfile = new EntityProfile(typeof(EntityWithNullableProperty));
            var propInfo      = entityProfile.HasProperty<EntityWithNullableProperty, bool?>(entity => entity.NullableBool);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        [Test]
        public void Should_return_true_given_float_member()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo      = entityProfile.HasProperty<TestEntity, float>(entity => entity.FloatValue);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_nullable_float_member()
        {
            var entityProfile = new EntityProfile(typeof(EntityWithNullableProperty));
            var propInfo      = entityProfile.HasProperty<EntityWithNullableProperty, float?>(entity => entity.NullableFloat);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_short_member()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo      = entityProfile.HasProperty<TestEntity, short>(entity => entity.ShortValue);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_nullable_short_member()
        {
            var entityProfile = new EntityProfile(typeof(EntityWithNullableProperty));
            var propInfo      = entityProfile.HasProperty<EntityWithNullableProperty, short?>(entity => entity.NullableShort);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_double_member()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo      = entityProfile.HasProperty<TestEntity, double>(entity => entity.DoubleValue);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_nullable_double_member()
        {
            var entityProfile = new EntityProfile(typeof(EntityWithNullableProperty));
            var propInfo      = entityProfile.HasProperty<EntityWithNullableProperty, double?>(entity => entity.NullableDouble);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_decimal_member()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo      = entityProfile.HasProperty<TestEntity, decimal>(entity => entity.DecimalValue);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_nullable_decimal_member()
        {
            var entityProfile = new EntityProfile(typeof(EntityWithNullableProperty));
            var propInfo      = entityProfile.HasProperty<EntityWithNullableProperty, decimal?>(entity => entity.NullableDecimal);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_simple_enum_member()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo      = entityProfile.HasProperty<TestEntity, SomeEnum>(entity => entity.Enumeration);
            Assert.IsFalse(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_converted_enum()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo      = entityProfile.HasProperty<TestEntity, int?>(entity => (int?)entity.Enumeration);
            Assert.IsTrue(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_string_member()
        {
            var entityProfile = new EntityProfile(typeof(TestEntity));
            var propInfo      = entityProfile.HasProperty<TestEntity, string>(entity => entity.RecordId);
            Assert.IsTrue(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_datetime_member()
        {
            var entityProfile = new EntityProfile(typeof(EntityWithDateTime));
            var propInfo      = entityProfile.HasProperty<EntityWithDateTime, DateTime>(entity => entity.DateAndTime);
            Assert.IsTrue(propInfo.IsDynamicallyInvoked());
        }
        
        [Test]
        public void Should_return_true_given_datetime_offset_member()
        {
            var entityProfile = new EntityProfile(typeof(EntityWithDateTime));
            var propInfo      = entityProfile.HasProperty<EntityWithDateTime, DateTimeOffset>(entity => entity.DateTimeAndOffset);
            Assert.IsTrue(propInfo.IsDynamicallyInvoked());
        }
    }
}