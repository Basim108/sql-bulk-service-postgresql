namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels
{
    public class TestEntityWithNullables
    {
        public int Id { get; set; }
        
        public string RecordId { get; set; }
        
        public string SensorId { get; set; }
        
        public bool?    NullableBool    { get; set; }
        public int?     NullableInt     { get; set; }
        public short?   NullableShort   { get; set; }
        public float?   NullableFloat   { get; set; }
        public double?  NullableDouble  { get; set; }
        public decimal? NullableDecimal { get; set; }
    }
}