using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Exception raised during building an sql command  
    /// </summary>
    public class SqlGenerationException: Exception
    {
        /// <inheritdoc />
        public SqlGenerationException(string message) :base(message) { }

        /// <inheritdoc />
        public SqlGenerationException(string message, Exception innerException)
        :base(message, innerException) { }

        /// <inheritdoc />
        public SqlGenerationException(Exception innerException)
            :base(innerException.Message, innerException) { }
    }
}