using Blast.Core.Interfaces;
using Blast.Core.Results;

namespace Translator.Fluent.Plugin
{
    public class LanguageSearchResult : SearchResultBase
    {
        public LanguageSearchResult(string searchedText, string language, string iconGlyph,
            IList<ISearchOperation> supportedOperations) : base($"Translate to {language}", searchedText,
            language, 1, supportedOperations, new List<SearchTag> {new() {Name = language, IconGlyph = iconGlyph}})
        {
            UseIconGlyph = true;
            IconGlyph = iconGlyph;
        }

        protected override void OnSelectedSearchResultChanged()
        {
        }
    }
}