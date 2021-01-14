using System.Collections.Generic;
using Blast.Core.Interfaces;
using Blast.Core.Results;

namespace Translator.Fluent.Plugin
{
    public class TranslationSearchResult : SearchResultBase
    {
        public TranslationSearchResult(string searchedText, Translation translation, string iconGlyph,
            IList<ISearchOperation> supportedOperations) : base(translation.Text, searchedText, translation.To, 1, supportedOperations,
            new List<SearchTag> {new() {Name = translation.To}})
        {
            UseIconGlyph = true;
            IconGlyph = iconGlyph;
        }

        protected override void OnSelectedSearchResultChanged()
        {
        }
    }
}