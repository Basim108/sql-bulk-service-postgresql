namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels
{
    public class TestEntityWithEnum
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