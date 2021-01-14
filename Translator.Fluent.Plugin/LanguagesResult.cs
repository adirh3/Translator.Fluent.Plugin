using System.Collections.Generic;

namespace Translator.Fluent.Plugin
{
    public class LanguagesResult
    {
        public Dictionary<string, Language> Translation { get; set; }
    }

    public class Language
    {
        public string Name { get; set; }

        public string NativeName { get; set; }

        public string Dir { get; set; }
    }
}