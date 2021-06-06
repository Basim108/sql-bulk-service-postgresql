using System;
using Hrimsoft.Core.ValueTypes;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.TestModels
{
    public class EntityWithDateTime
    {
        public Date Date { get; set; }
        public DateTime DateAndTime { get; set; }
        public DateTimeOffset DateTimeAndOffset { get; set; }
    }
}