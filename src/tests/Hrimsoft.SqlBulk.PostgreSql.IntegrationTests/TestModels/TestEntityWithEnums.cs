namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels
{
    public class TestEntityWithIntEnum
    {
        public int Id { get; set; }
        
        public SomeEnum SomeEnumValue { get; set; }
    }
    
    public class TestEntityWithStrEnum
    {
        public int Id { get; set; }
        
        public SomeEnum SomeEnumValue { get; set; }
    }
    
    public enum SomeEnum
    {
        SomeValue = 1,
        AnotherValue = 16
    }
}