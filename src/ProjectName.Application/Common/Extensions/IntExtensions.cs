namespace ProjectName.Application.Common.Extensions;

public static class IntExtensions
{
    public static T? ToEnum<T>(this int? value) where T : Enum
    {
        if (value != null && Enum.IsDefined(typeof(T), value))
        {
            return (T)Enum.ToObject(typeof(T), value);
        }

        return default;
    }

    public static T? ToEnum<T>(this int value) where T : Enum
    {
        if (Enum.IsDefined(typeof(T), value))
        {
            return (T)Enum.ToObject(typeof(T), value);
        }

        return default;
    }
}