namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels
{
    public class EntityWithNullableProperty
    {
        public int Id { get; set; }
        
        public string RecordId { get; set; }
        
        public string SensorId { get; set; }
        
        public int? Value { get; set; }
    }
}