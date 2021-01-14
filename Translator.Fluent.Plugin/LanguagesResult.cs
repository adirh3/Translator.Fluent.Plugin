using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Translator.Fluent.Plugin
{
    public class LanguagesResult
    {
        [JsonPropertyName("translation")] public Dictionary<string, Language> Languages { get; set; }
    }

    public class Language
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("nativeName")] public string NativeName { get; set; }

        [JsonPropertyName("dir")] public string Dir { get; set; }
    }
}