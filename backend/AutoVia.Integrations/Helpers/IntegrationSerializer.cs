using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoVia.Integrations.Helpers
{
    /// <summary>
    /// Opcoes unicas de serializacao JSON das chamadas externas. Todo provedor
    /// usa estas opcoes, para que nenhum tenha seu proprio jeito de serializar.
    /// </summary>
    public static class IntegrationSerializer
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, Options);
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
    }
}
