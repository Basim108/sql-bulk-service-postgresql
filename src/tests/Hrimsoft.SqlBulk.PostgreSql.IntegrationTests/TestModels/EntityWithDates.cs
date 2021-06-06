using System;
using Hrimsoft.Core.ValueObjects;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels
{
    public class EntityWithDates
    {
        public int            Id                { get; set; }
        public Date           Date              { get; set; }
        public DateTime       DateAndTime       { get; set; }
        public DateTimeOffset DateTimeAndOffset { get; set; }
    }
}