namespace Hrimsoft.SqlBulk.PostgreSql.Tests.UpsertSqlCommandBuilderService
{
    public class UpsertConsts
    {
        public const string UPSERT_PATTERN =
"insert\\s+into\\s+(\"\\w+\".)?\"\\w+\"\\s*\\(\\s*\"\\w+\"(,\\s*\"\\w+\")*\\s*\\)\\s*values\\s*\\(\\s*@param\\d+(,\\s*@param\\d+)*\\s*\\)\\s*(,\\s*\\(\\s*@param\\d+(,\\s*@param\\d+)*\\s*\\))*\\s*on\\s+conflict\\s+(\\(\\s*\"\\w+\"\\s*\\)|on\\s+constraint\\s+\"\\w+\")\\s+do\\s+update\\s+set\\s+\"\\w+\"\\s*=\\s*(\"\\w+\".)?\"\\w+\".\"\\w+\"\\s*(,\\s*\"\\w+\"\\s*=\\s*(\"\\w+\".)?\"\\w+\".\"\\w+\")*\\s*(returning\\s+\"\\w+\"\\s*(,\\s*\"\\w+\")*)?;";

    }
}