using System.Text.Json;
using System.Text.Json.Serialization;

namespace KurisuCore.Models
{
    public sealed class KSettings
    {
        public int CrushRewardMin { get; set; } = 3;
        public int CrushRewardMax { get; set; } = 5;
        public int MaxBinderSize { get; set; } = 50;

        public int MaxQuizTokens { get; set; } = 10;
        public int QuizTokenRefreshInterval { get; set; } = 15;
        public int QuizTimeToAnswer { get; set; } = 30;
        public int QuizRewardMin { get; set; } = 20;
        public int QuizRewardMax { get; set; } = 30;
    }

    internal static class KSettingsSerializer
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string Serialize(KSettings settings)
        {
            return JsonSerializer.Serialize(settings, _options);
        }

        public static KSettings Deserialize(string json)
        {
            return JsonSerializer.Deserialize<KSettings>(json, _options) ?? new KSettings();
        }
    }
}
