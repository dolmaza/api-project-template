using Dapper;
using ProjectName.Application.Common.Constants;
using ProjectName.Domain.Common.Abstractions;

namespace ProjectName.Application.Common.Extensions;

public static class SqlBuilderFilterExtensions
{
    extension(SqlBuilder sqlBuilder)
    {
        public SqlBuilder ApplySoftDeleteFilter(string alias = "")
        {
            sqlBuilder.Where($"""
                              {GetAlias(alias)}"DeletedAt" is null
                              """);

            return sqlBuilder;
        }

        public SqlBuilder ApplySearchFilter(DatabaseColumn[] columns, DynamicParameters parameters, string? searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return sqlBuilder;
            
            parameters.Add(nameof(searchValue), $"%{searchValue}%");
            
            var searchConditions =
                string.Join(" OR ", columns.Select(column => $"{column.GetFullColumnName()} ILIKE @{nameof(searchValue)}"));
            
            sqlBuilder.Where($"({searchConditions})");

            return sqlBuilder;
        }

        public SqlBuilder ApplyUserFilter(ICurrentStateService currentStateService, DynamicParameters parameters, DatabaseColumn column)
        {
            var isUserAdministrator = currentStateService.IsInRole(RoleNames.Administrator);

            if (isUserAdministrator) 
                return sqlBuilder;
            
            var userId = currentStateService.GetAuthorizedId();
            parameters.Add(nameof(userId), userId);
            sqlBuilder.Where($"{column.GetFullColumnName()} = @{nameof(userId)}");

            return sqlBuilder;
        }

        public SqlBuilder ApplyCustomFilter<T>(T value, DynamicParameters parameters, DatabaseColumn column)
        {
            parameters.Add(nameof(value), value);
            sqlBuilder.Where($"{column.GetFullColumnName()} = @{nameof(value)}");

            return sqlBuilder;
        }
    }

    private static string GetAlias(string alias)
    {
        return string.IsNullOrEmpty(alias) ? "" : $"{alias}.";
    }
}

public record DatabaseColumn(string Name, string Alias = "")
{
    public string GetFullColumnName() => string.IsNullOrEmpty(Alias) ? $""" "{Name}" """.Trim() : $"""{Alias}."{Name}" """.Trim();
}