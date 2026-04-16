using System.Text.RegularExpressions;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Domain.Common.Validation;

public static partial class ValidationHelpers
{
    private const string UrlPattern = @"^https?://[^\s/$.?#].[^\s]*$";
    private const string LowercaseKeyPattern = @"^[a-z0-9_]+$";

    /// <summary>
    /// Validates that a string is not null, empty, or whitespace
    /// </summary>
    public static Result ValidateRequired(string? value, Error requiredError)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return requiredError;
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates the maximum length of a trimmed string
    /// </summary>
    public static Result ValidateMaxLength(string? value, int maxLength, Error tooLongError)
    {
        var trimmedValue = value?.Trim();
        return trimmedValue?.Length > maxLength ? tooLongError : Result.Success();
    }

    /// <summary>
    /// Validates that a required string meets both required and max length constraints
    /// </summary>
    public static Result ValidateRequiredWithMaxLength(
        string? value, 
        int maxLength, 
        Error requiredError, 
        Error tooLongError)
    {
        var requiredResult = ValidateRequired(value, requiredError);
        if (!requiredResult.IsSuccess)
        {
            return requiredResult;
        }

        return ValidateMaxLength(value, maxLength, tooLongError);
    }

    /// <summary>
    /// Validates a URL format and length
    /// </summary>
    public static Result ValidateUrl(string? url, int maxLength, Error invalidUrlError, Error tooLongError)
    {
        var trimmedUrl = url?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedUrl))
        {
            return Result.Success(); // URL is optional
        }

        if (trimmedUrl.Length > maxLength)
        {
            return tooLongError;
        }

        if (!UrlRegex().IsMatch(trimmedUrl))
        {
            return invalidUrlError;
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates a numeric range (inclusive)
    /// </summary>
    public static Result ValidateRange(decimal value, decimal min, decimal max, Error rangeError)
    {
        if (value < min || value > max)
        {
            return rangeError;
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates a Guid is not empty
    /// </summary>
    public static Result ValidateGuidNotEmpty(Guid value, Error emptyGuidError)
    {
        if (value == Guid.Empty)
        {
            return emptyGuidError;
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates a key format (lowercase letters, numbers, and underscores only)
    /// </summary>
    public static Result ValidateKeyFormat(string? key, int maxLength, Error requiredError, Error tooLongError, Error invalidFormatError)
    {
        var requiredResult = ValidateRequired(key, requiredError);
        if (!requiredResult.IsSuccess)
        {
            return requiredResult;
        }

        var trimmedKey = key!.Trim().ToLowerInvariant();
        
        if (trimmedKey.Length > maxLength)
        {
            return tooLongError;
        }

        if (!LowercaseKeyRegex().IsMatch(trimmedKey))
        {
            return invalidFormatError;
        }

        return Result.Success();
    }

    /// <summary>
    /// Trims a string if not null or whitespace, otherwise returns null
    /// </summary>
    public static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Normalizes a key to lowercase and trims whitespace
    /// </summary>
    public static string NormalizeKey(string key)
    {
        return key.Trim().ToLowerInvariant();
    }

    public static Result IsPositiveNumber(int number, Error error) => number < 0
        ? error
        : Result.Success();

    /// <summary>
    /// Validates that an enum value is defined in the enum type
    /// </summary>
    public static Result ValidateEnum<TEnum>(TEnum value, Error invalidEnumError) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            return invalidEnumError;
        }

        return Result.Success();
    }

    public static Result ValidateAddress(Address address, Error addressError)
    {
        var requiredResult = ValidateRequired(address.Street, addressError);

        if (requiredResult.IsFailure)
        {
            return requiredResult.Error;
        }

        return Result.Success();
    }

    [GeneratedRegex(UrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(LowercaseKeyPattern, RegexOptions.Compiled)]
    private static partial Regex LowercaseKeyRegex();
}
