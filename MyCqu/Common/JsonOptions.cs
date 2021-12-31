using System.Text.Json;

namespace MyCqu.Common;

public static class JsonOptions
{
    private static JsonSerializerOptions _options;

    public static JsonSerializerOptions Options => _options?? new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
