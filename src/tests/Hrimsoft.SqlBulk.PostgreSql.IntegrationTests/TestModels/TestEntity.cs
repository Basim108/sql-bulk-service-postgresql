namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels
{
    public class TestEntity
    {
        public int Id { get; set; }

        public string RecordId { get; set; }

        public string SensorId { get; set; }

        public int Value { get; set; }

        public int? NullableValue { get; set; }
    }
}