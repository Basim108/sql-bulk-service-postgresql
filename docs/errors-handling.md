# Errors Handling #

As the number of items that have to be operated in one sql command could be limited by MaximumSentElements option.
```c#
public class Startup
{
    ...
    public void ConfigureServices(IServiceCollection services)
    {
        ...
        services.AddPostgreSqlBulkService(options =>
        {
            options.MaximumSentElements = 500; 

            options.AddEntityProfile<TestEntity>(new TestEntityProfile());
        });
    }
}
```
```c#
  // inputElements is a list with more than 500 items
  await _bulkService.InsertAsync(connection, inputElements, cancellationToken);
```

Where 500 - is less than length of the list of input element. 
User might want to operated several thousands of items, and in order to make their life easier, bulk service introduces 
MaximumSentElements option. This option, as most of others, could be set for all entity types in BulkServiceOptions, or 
could be set in the specific EntityProfile, so it will be applicable only for bulk operations on instances of this type of entity.
It also could set one number for all entites and a specific number for a spesific entity type in EntityProfile class.
```c#
public class Startup
{
    ...
    public void ConfigureServices(IServiceCollection services)
    {
        ...
        services.AddPostgreSqlBulkService(options =>
        {
            options.MaximumSentElements = 500; 

            // bulk operation on books will be split on subsets of 1000 items
            options.AddEntityProfile<TestEntity>(new BookEntityProfile{
                MaximumSentElements = 1000; 
            });
            
            // bulk operation on authors will be split on subsets of 500 items
            options.AddEntityProfile<TestEntity>(new AuthorEntityProfile());
        });
    }
}
```
As we have several subsets of input elements, there are several ways how errors could be handled. 
These ways of handling are defined by setting FaultStrategy. As usual, on BulkServiceOption - 
this will work for all types of entities, or in EntityProfile and this will spesify a strategy 
for a spesific entity.

## Fault Strategies ##

There three strategies that allows to affect error handling process.
```c#
/// <summary>
/// Strategies of processing sql command failures
/// </summary>
public enum FailureStrategies
{
    /// <summary>
    /// If service has several sets of elements to operate,
    /// and while operating an intermediate set an exception was thrown,
    /// this strategy will tell service to stop all further operations and raise the exception higher.
    /// However, the elements that were previously operated will be still saved in the storage 
    /// </summary>
    StopEverything,
    /// <summary>
    /// The same strategy as <see cref="StopEverything"/>, but, in addition to it,
    /// it will rollback all the elements that were already operated. 
    /// </summary>
    StopEverythingAndRollback,
    /// <summary>
    /// Service will ignore any failure and will try to operate all the remaining subsets of elements.
    /// Those elements that were operated, an not operated elements will be returned in the result in 
    /// <see cref="BulkOperationResult"/> 
    /// </summary>
    IgnoreFailure
}
```

If strategy will be set not to IgnoreFailure, then any bulk operation will return null. 
This is because, BulkOperationResult makes sense only when error occurs, otherwise all elements will be operated. 
If the error occurs all collections of operated, not operated, and problem elements will be set in 
the SqlBulkExecutionException<TEntity> and BulkOperationResult becomes redundant.
In IgnoreFailure all exceptions will be catched and collections will be filled with corresponding elements.

## Exceptions ##

Bulk service might generate these exceptions: 
* [SqlGenerationException](../src/Hrimsoft.SqlBulk.PostgreSql/Exceptions/SqlGenerationException.cs) occurs during an sql command generation process. 
It has Operation property that indicates during which sql operation the exception was thrown.
* [TypeMappingException](../src/Hrimsoft.SqlBulk.PostgreSql/Exceptions/TypeMappingException.cs) occurs during type mapping.
* [SqlBulkExecutionException<TEntity>](../src/Hrimsoft.SqlBulk.PostgreSql/Exceptions/SqlBulkExecutionExceptionT.cs) occurs during an bulk execution process. 
It has propertes that indicates elements that were operated, were not operated, and subset of elements that provoked an exception.

These collections of elements might be empty, and they are empty by default. 
However, if in BulkServiceOptions user sets properties to true then these collections will be filled with elements correspondingly 
to the enabled options. If these options are set to false, as by default, then server time and memory will be saved.

```c#
public class Startup
{
    ...
    public void ConfigureServices(IServiceCollection services)
    {
        ...
        services.AddPostgreSqlBulkService(options =>
        {
            // this option garantees that if the exception occurs OperatedElements collection 
            // will be filled with those elements that were successfully operated.
            options.IsOperatedElementsEnabled = true;
            
            // this option garantees that if the exception occurs NotOperatedElements collection            
            // will be filled with those elements that were not operated.
            options.IsNotOperatedElementsEnabled = true;
            
            // this option garantees that if the exception occurs ProblemElements collection  
            // will be filled with those elements that provoked an exception.
            options.IsProblemElementsEnabled = true;
            
            options.AddEntityProfile<TestEntity>(new TestEntityProfile());
        });
    }
}
```
These three collections Operated, NotOperated, and ProblemElements make sense only 
if the set of input elements was split into several subsets of elements by setting 
```c#
options.MaximumSentElements = n; // where n - is less than length of the list of input element.
```
Therefore, with having several subsets of elements some them could be oprated, for example, the first subset.
Some of them not, for example, the second and the third subsets, because while operating the second subset 
the inner exception occured (database constraint violation, or connection lost, etc).
And some of them, in this example, the second subset will be the problem one; 
therefore, it will be put into the ProblemElements collection.

These options IsOperatedElementsEnabled, IsNotOperatedElementsEnabled, and IsProblemElementsEnabled could be set for all 
entity types in BulkServiceOptions, or could be set in the specific EntityProfile, so it will be applicable only 
for bulk operations on instances of this type of entity.
