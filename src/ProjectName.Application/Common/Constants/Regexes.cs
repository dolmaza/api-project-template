using System.Text.RegularExpressions;

namespace ProjectName.Application.Common.Constants;

public static partial class Regexes
{
    public static readonly Regex PasswordRegex = Password();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")]
    private static partial Regex Password();
}