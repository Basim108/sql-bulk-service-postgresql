using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Exception raised during type mapping process  
    /// </summary>
    public class TypeMappingException: Exception
    {
        /// <inheritdoc />
        public TypeMappingException(string message) :base(message) { }

        /// <inheritdoc />
        public TypeMappingException(string message, Exception innerException)
        :base(message, innerException) { }

        /// <inheritdoc />
        public TypeMappingException(Exception innerException)
            :base(innerException.Message, innerException) { }

        public TypeMappingException(Type entityType, string propertyName, string message) : base(message)
        {
            EntityType = entityType;
            PropertyName = propertyName;
        }

        /// <summary>
        /// Type of entity which mapping provoked an exception
        /// </summary>
        public Type EntityType { get; }
        
        /// <summary>
        /// Name of property which mapping provoked an exception
        /// </summary>
        public string PropertyName { get; }
    }
}