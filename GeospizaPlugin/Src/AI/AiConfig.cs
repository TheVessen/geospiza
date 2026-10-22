#nullable enable
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeospizaPlugin.AI;

public enum TextProviderKind
{
    Ollama,
    OpenAiCompatible
}

/// <summary>
///     Per-user AI settings, edited through <see cref="AiSettingsDialog" />. API keys are stored in plain
///     text in this file; the environment variables are used when a key is left empty.
/// </summary>
public class AiConfig
{
    /// <summary>API key for the OpenAI-compatible endpoint when none is saved.</summary>
    public const string LlmApiKeyEnvironmentVariable = "GEOSPIZA_LLM_API_KEY";

    public const string DefaultOllamaModel = "gemma4:e4b";

    [JsonConverter(typeof(StringEnumConverter))]
    public TextProviderKind TextProvider { get; set; } = TextProviderKind.Ollama;

    // The JSON name predates the other providers; kept so existing config files keep their model.
    [JsonProperty("SelectedModel")]
    public string OllamaModel { get; set; } = DefaultOllamaModel;

    public string OpenAiBaseUrl { get; set; } = "http://localhost:1234/v1";
    public string OpenAiModel { get; set; } = string.Empty;
    public string? OpenAiApiKey { get; set; }

    public string JevBaseUrl { get; set; } = JevDecisionModel.DefaultBaseUrl;
    public string JevModel { get; set; } = JevDecisionModel.DefaultModel;
    public string? JevApiKey { get; set; }

    [JsonIgnore]
    public string? ResolvedOpenAiApiKey => KeyOrEnvironment(OpenAiApiKey, LlmApiKeyEnvironmentVariable);

    [JsonIgnore]
    public string? ResolvedJevApiKey => KeyOrEnvironment(JevApiKey, JevDecisionModel.ApiKeyEnvironmentVariable);

    public static string ConfigPath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "Geospiza");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ai-config.json");
        }
    }

    public static AiConfig Load()
    {
        try
        {
            var path = ConfigPath;
            if (File.Exists(path))
                return JsonConvert.DeserializeObject<AiConfig>(File.ReadAllText(path)) ?? new AiConfig();
        }
        catch
        {
            // An unreadable config means defaults; the settings dialog overwrites it on save.
        }

        return new AiConfig();
    }

    /// <summary>Throws when the file can't be written, so the settings dialog can say so.</summary>
    public void Save()
    {
        File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    private static string? KeyOrEnvironment(string? saved, string variable)
    {
        if (!string.IsNullOrWhiteSpace(saved)) return saved!.Trim();
        var fromEnvironment = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(fromEnvironment) ? null : fromEnvironment!.Trim();
    }
}

public static class AiProviders
{
    public static ITextModel CreateTextModel(AiConfig config) => config.TextProvider switch
    {
        TextProviderKind.OpenAiCompatible =>
            new OpenAiCompatibleTextModel(config.OpenAiBaseUrl, config.OpenAiModel, config.ResolvedOpenAiApiKey),
        _ => new OllamaTextModel(string.IsNullOrWhiteSpace(config.OllamaModel)
            ? AiConfig.DefaultOllamaModel
            : config.OllamaModel)
    };

    public static IDecisionModel CreateJev(AiConfig config)
        => new JevDecisionModel(config.JevBaseUrl, config.JevModel, config.ResolvedJevApiKey);
}
