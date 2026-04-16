using Newtonsoft.Json;

namespace ProjectName.Application.Common.Extensions;

public static class JsonExtensions
{
    public static string? ToJson(this object? obj)
    {
        return obj == null ? null : JsonConvert.SerializeObject(obj);
    }

    public static T? FromJsonTo<T>(this string? str)
    {
        return string.IsNullOrEmpty(str)
            ? default
            : JsonConvert.DeserializeObject<T>(str);
    }
}