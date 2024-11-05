using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Blast.API.Search;
using Blast.API.Search.SearchOperations;
using Blast.Core;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using TextCopy;

namespace Translator.Fluent.Plugin
{
    public class TranslatorSearchApp : ISearchApplication
    {
        private const string SubscriptionKey = "";
        private const string Endpoint = "https://api.cognitive.microsofttranslator.com/";
        private const string SearchAppName = "Translator";
        private const string DictionaryIconGlyph = "\uF2B7";
        private const string TranslatorSearchTag = "translator";
        private readonly SearchApplicationInfo _applicationInfo;
        private readonly List<ISearchOperation> _translateOperations;
        private readonly List<ISearchOperation> _languageOperations;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() {PropertyNameCaseInsensitive = true};

        private readonly Dictionary<string, string> _supportedLanguages = new();
        private readonly Dictionary<string, string> _supportedLanguagesReversed = new();
        private readonly CopySearchOperation _copyLatinSearchOperation;


        public TranslatorSearchApp()
        {
            _httpClient = new HttpClient();
            // For icon glyphs look at https://docs.microsoft.com/en-us/windows/uwp/design/style/segoe-ui-symbol-font

            var copySearchOperation = new CopySearchOperation();
            _copyLatinSearchOperation = new CopySearchOperation("Copy Latin text");
            var translateSearchOperation = new TranslateSearchOperation();
            _translateOperations = new List<ISearchOperation>
            {
                copySearchOperation,
                _copyLatinSearchOperation
            };
            _languageOperations = new List<ISearchOperation>
            {
                translateSearchOperation
            };
            _applicationInfo = new SearchApplicationInfo(SearchAppName,
                "Translate search to selected language",
                new SearchOperationBase[] {copySearchOperation, translateSearchOperation})
            {
                IsProcessSearchEnabled = false,
                IsProcessSearchOffline = false,
                SearchTagOnly = true,
                ApplicationIconGlyph = DictionaryIconGlyph,
                SearchAllTime = ApplicationSearchTime.Fast,
                DefaultSearchTags = new List<SearchTag>()
            };
        }

        public async ValueTask LoadSearchApplicationAsync()
        {
            // This is used if you need to load anything asynchronously on Fluent Search startup
            Dictionary<string, Language> dictionaryResult = (await _httpClient.GetFromJsonAsync<LanguagesResult>(
                "https://api.cognitive.microsofttranslator.com/languages?api-version=3.0&scope=translation",
                _jsonSerializerOptions))?.Translation;
            if (dictionaryResult is null)
                return;

            foreach (var (key, value) in dictionaryResult)
            {
                string languageName = value.Name.ToLower();
                _supportedLanguages[languageName] = key;
                _supportedLanguagesReversed[key] = languageName;
            }


            _applicationInfo.DefaultSearchTags = _supportedLanguages.Keys.Select(l => new SearchTag
            {
                Name = l,
                IconGlyph = DictionaryIconGlyph
            }).ToList();
        }

        public SearchApplicationInfo GetApplicationInfo()
        {
            return _applicationInfo;
        }

        public async IAsyncEnumerable<ISearchResult> SearchAsync(SearchRequest searchRequest,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || searchRequest.SearchType == SearchType.SearchProcess)
                yield break;
            string searchedTag = searchRequest.SearchedTag;
            string searchedText = searchRequest.SearchedText;

            string toLanguage = null;
            if (!searchedTag.Equals(TranslatorSearchTag) &&
                !_supportedLanguages.TryGetValue(searchedTag, out toLanguage))
                yield break;

            // This means that the user searched for translator tag
            if (toLanguage == null)
            {
                // If the search is empty return all results
                bool searchAll = string.IsNullOrWhiteSpace(searchedText);
                foreach (string language in _supportedLanguages.Keys)
                {
                    if (searchAll || language.SearchBlind(searchedText))
                        yield return new LanguageSearchResult(searchedText, language, DictionaryIconGlyph,
                            _languageOperations);
                }

                yield break;
            }

            // We don't want to send 2 letter translations
            if (searchedText.Length < 2)
                yield break;

            // Output languages are defined as parameters, input language detected.
            string route = $"/translate?api-version=3.0&to={toLanguage}&toScript=Latn";
            object[] body = [new {Text = searchedText}];
            string requestBody = JsonSerializer.Serialize(body);

            using var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(Endpoint + route);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            // Build the request.
            request.Headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", "eastus2");

            // Send the request and get response.
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            TranslationResult[] translationResults = await JsonSerializer.DeserializeAsync<TranslationResult[]>(
                responseStream, _jsonSerializerOptions, cancellationToken);

            if (translationResults is null)
                yield break;

            foreach (TranslationResult translationResult in translationResults)
            {
                string detectedLanguage = translationResult.DetectedLanguage?.Language ?? string.Empty;
                foreach (Translation translation in translationResult.Translations)
                {
                    var tags = new List<SearchTag>
                    {
                        new()
                        {
                            Name = searchedTag,
                            Value = searchedTag,
                            IconGlyph = DictionaryIconGlyph
                        }
                    };

                    if (detectedLanguage != toLanguage &&
                        _supportedLanguagesReversed.TryGetValue(detectedLanguage, out var languageName))
                    {
                        tags.Add(new()
                        {
                            Name = languageName,
                            Value = languageName,
                            IconGlyph = DictionaryIconGlyph
                        });
                    }

                    yield return new TranslationSearchResult(searchedText, translation, DictionaryIconGlyph,
                        _translateOperations, tags);
                }
            }
        }

        public ValueTask<ISearchResult> GetSearchResultForId(string serializedSearchObjectId)
        {
            // This is used to calculate a search result after Fluent Search has been restarted
            // This is only used by the custom search tag feature
            return new();
        }

        public ValueTask<IHandleResult> HandleSearchResult(ISearchResult searchResult)
        {
            Type type = searchResult.GetType();

            if (type == typeof(LanguageSearchResult))
            {
                // This will cause Fluent Search to search again using the selected language
                SearchTag searchTag = searchResult.Tags.FirstOrDefault();
                return new ValueTask<IHandleResult>(new HandleResult(true, true)
                {
                    SearchRequest = new SearchRequest(string.Empty, searchTag?.Name, SearchType.SearchAll),
                    SearchTag = searchTag
                });
            }

            // Type is TranslateSearchResult
            string textToCopy = searchResult.ResultName;
            if (Equals(searchResult.SelectedOperation, _copyLatinSearchOperation))
                textToCopy = ((TranslationSearchResult) searchResult).LatinTranslationText;
            Clipboard.SetText(textToCopy);
            return new ValueTask<IHandleResult>(new HandleResult(true, false));
        }
    }
}