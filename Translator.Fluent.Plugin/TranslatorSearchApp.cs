using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
        private const string DictionaryIconGlyph = "\uE82D";
        private readonly SearchApplicationInfo _applicationInfo;
        private readonly List<ISearchOperation> _supportedOperations;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() {PropertyNameCaseInsensitive = true};

        private Dictionary<string, string> _supportedLanguages;


        public TranslatorSearchApp()
        {
            _httpClient = new HttpClient();
            // For icon glyphs look at https://docs.microsoft.com/en-us/windows/uwp/design/style/segoe-ui-symbol-font

            _supportedOperations = new List<ISearchOperation>
            {
                new CopySearchOperation()
            };
            _applicationInfo = new SearchApplicationInfo(SearchAppName,
                "This apps translates detected languages to target languages", _supportedOperations)
            {
                MinimumSearchLength = 3,
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
            _supportedLanguages = (await _httpClient.GetFromJsonAsync<LanguagesResult>(
                                      "https://api.cognitive.microsofttranslator.com/languages?api-version=3.0&scope=translation",
                                      _jsonSerializerOptions))
                                  ?.Translation.ToDictionary(pair => pair.Value.Name.ToLower(), pair => pair.Key) ??
                                  new Dictionary<string, string>();
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

            if (!_supportedLanguages.TryGetValue(searchedTag, out var toLanguage))
                yield break;

            // Output languages are defined as parameters, input language detected.
            string route = $"/translate?api-version=3.0&to={toLanguage}";
            object[] body = {new {Text = searchedText}};
            string requestBody = JsonSerializer.Serialize(body);

            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(Endpoint + route),
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };
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
                foreach (Translation translation in translationResult.Translations)
                {
                    yield return new TranslationSearchResult(searchedText, translation, DictionaryIconGlyph,
                        _supportedOperations);
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
            Clipboard.SetText(searchResult.ResultName);
            return new ValueTask<IHandleResult>(new HandleResult(true, false));
        }
    }
}