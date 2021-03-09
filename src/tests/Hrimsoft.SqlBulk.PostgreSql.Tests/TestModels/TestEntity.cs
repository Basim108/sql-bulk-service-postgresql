namespace Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels
{
    public class TestEntity
    {
        public int Id { get; set; }
        
        public string RecordId { get; set; }
        
        public string SensorId { get; set; }
        
        public int IntValue { get; set; }
        public short ShortValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
        public bool BoolValue { get; set; }
        
        public SomeEnum Enumeration { get; set; }
    }
}