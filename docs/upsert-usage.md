# Upsert usage #
Upsert is an operation that allows in one command insert or update rows.
In order to make upsert possible a table must have a constraint or unique column. 
The idea is upsert command will try to insert data and when insert violates a constraint or unique column it will update data instead.

For example, declare the table that has two columns with unique values.
```sql
create table "unit_tests"."entity_with_unique_columns"
(
	id serial not null
		constraint entity_with_unique_columns_pk primary key,
	record_id text,
	sensor_id text,
    value integer,
    constraint business_identity unique (record_id, sensor_id)
);
```
It means we can't have rows where record_id and sensor_id has the same values like
record_id  sensor_id
   1          2
   1          1
   2          2
   1          2  -- this row violates a unique constraint

The idea is that if insert operation violates this unique constraint than upsert will perform an update operation.

## Declaring a constraint ##

During the entity mapping process we have to declare a constraint:
```c#
/// <summary>
/// Entity profile that defines properties that
///  - have unique constraint
///  - have to be included into the returning clause
/// </summary>
public class UpsertEntityProfile: EntityProfile
{
    public UpsertEntityProfile(int maximumSentElements=0)
        :base(typeof(TestEntity))
    {
        this.MaximumSentElements = maximumSentElements;

        this.ToTable("entity_with_unique_columns", "unit_tests");
        
        this.HasProperty<TestEntity, int>(entity => entity.Id)
            .ThatIsAutoGenerated()
            .ThatIsPrivateKey();

        // The name of unique constraint.
        this.HasUniqueConstraint("business_identity");

        // declaration of a property as a part of a unique constraint
        this.HasPropertyAsPartOfUniqueConstraint<TestEntity, string>(entity => entity.RecordId);
        this.HasPropertyAsPartOfUniqueConstraint<TestEntity, string>(entity => entity.SensorId);

        this.HasProperty<TestEntity, int>(entity => entity.Value);
    }
}
```
It is importat to set the name of constraint by calling HasUniqueConstraint method, 
and then declare columns that constraint includes by calling either HasPropertyAsPartOfUniqueConstraint or ThatIsPartOfUniqueConstraint methods.

It is also possible to declare unique constraint as a property attribute by decarating entity's property with PartOfUniqueConstraintAttribute.
However, in this case anyway we need to set constraint name in EntityProfile class with calling method HasUniqueConstraint. 
```c#
    [Table("books", Schema = "unit_tests")]
    public class Book
    {
        [Key, AutoGenerated]
        public int Id { get; set; }
        
        [Column("author_id", TypeName = "text")]
        [PartOfUniqueConstraint]
        public int AuthorId { get; set; } 
        
        public Author WrittenBy { get; set; } 
        
        [DataType(DataType.Text)]
        [PartOfUniqueConstraint]
        public string Title { get; set; }
        
        [UpdateAfterInsert]
        public long ValueGeneratedOnInsert { get; set; }
        
        [UpdateAfterUpdate]
        public long ValueGeneratedOnUpdate { get; set; }
        
        [NotMapped]
        public long AbstractValue { get; set; }
    }
```
## Executing an upsert operation ##
```c#
public class BookService {

  private readonly IPostgreSqlBulkService _bulkService;
  
  public void BookService(IPostgreSqlBulkService bulkService){
    _bulkService = bulkService;
  }

  ///<param name="books">A collection of new books that have to be inserted, and modified books that have to be updated</param>
  public Task UpsertAsync(ICollection<Book> books, CancellationToken cancellationToken) {
    using (var connection = new NpgsqlConnection(_configuration.ConnectionString))
    {
      // those boks that have the same "author_id" and "title" will not be inserted, but updated
      await _bulkService.UpsertAsync(connection, books, cancellationToken);
    }
}
```
