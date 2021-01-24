using System.Collections.Generic;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;

namespace Translator.Fluent.Plugin
{
    public class TranslationSearchResult : SearchResultBase
    {
        public TranslationSearchResult(string searchedText, Translation translation, string iconGlyph,
            IList<ISearchOperation> supportedOperations, List<SearchTag> searchTags) : base(translation.Text,
            searchedText, translation.To, 1,
            supportedOperations, searchTags)
        {
            UseIconGlyph = true;
            IconGlyph = iconGlyph;
            if (!string.IsNullOrWhiteSpace(translation.Transliteration?.Text))
            {
                LatinTranslationText = translation.Transliteration.Text;
                InformationElements = new List<InformationElement> {new("Latin", translation.Transliteration.Text)};
            }
        }

        internal string LatinTranslationText { get; set; }

        protected override void OnSelectedSearchResultChanged()
        {
        }
    }
}