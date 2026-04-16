using ProjectName.Domain.Common;

namespace ProjectName.Application.Common.Extensions;

public static class MoneyExtensions
{
    public static decimal ToAmount(this Money? money)
    {
        return money == null ? throw new ArgumentNullException(nameof(money)) : Convert.ToDecimal(money.Minor) / 100;
    }
}