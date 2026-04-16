using Dapper;

namespace ProjectName.Application.Common.Extensions;

public static class SqlBuilderJoinExtensions
{
    public static SqlBuilder InnerJoinIf(this SqlBuilder builder, bool condition, string joinClause)
    {
        if (condition)
        {
            builder.InnerJoin(joinClause);
        }
        return builder;
    }
}