namespace Hrimsoft.SqlBulk.PostgreSql
{
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
        /// Service will ignore any failure and will try to operate all the elements.
        /// However, those not operated elements will be returned in the result in <see cref="SqlBulkExecutionException{TEntity}.ProblemElements"/> collection
        /// </summary>
        IgnoreFailure
    }
}