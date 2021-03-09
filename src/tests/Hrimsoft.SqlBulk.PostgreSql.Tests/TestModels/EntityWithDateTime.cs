using System;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels
{
    public class EntityWithDateTime
    {
        public DateTime DateAndTime { get; set; }
        public DateTimeOffset DateTimeAndOffset { get; set; }
    }
}