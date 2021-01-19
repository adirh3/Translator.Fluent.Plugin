using Blast.Core.Results;

namespace Translator.Fluent.Plugin
{
    public class TranslateSearchOperation : SearchOperationBase
    {
        protected internal TranslateSearchOperation() : base(
            "Translate", "Translates to the specified language", "\uF2B7")
        {
            HideMainWindow = false;
        }
    }
}