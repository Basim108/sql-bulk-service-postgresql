namespace Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels
{
    public class EntityWithNullableProperty
    {
        public int Id { get; set; }
        
        public string RecordId { get; set; }
        
        public string SensorId { get; set; }
        
        public bool? Value { get; set; }
    }
}