using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Collections.ObjectModel;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPCogBingSearch
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public class NewsArticle
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Provider { get; set; }
    }
    public class News
    {
        public NewsArticle Article { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        private static string AutoSuggestionEndPoint = "https://api.cognitive.microsoft.com/bing/v5.0/suggestions";
        private static string NewsSearchEndPoint = "https://api.cognitive.microsoft.com/bing/v5.0/news/search";

        private static HttpClient autoSuggestionClient { get; set; }
        private static HttpClient searchClient { get; set; }

        private List<News> latestSearchResult = new List<News>();
        public ObservableCollection<News> SearchResults { get; set; } = new ObservableCollection<News>();

        public MainPage()
        {
            this.InitializeComponent();
            InitializeBingClients();
        }

        private static void InitializeBingClients()
        {
            autoSuggestionClient = new HttpClient();
            autoSuggestionClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<Your Key>");

            searchClient = new HttpClient();
            searchClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<Your Key>");

        }
      //Auto Suggest
        private async void asbBingsearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                try
                {
                    asbBingsearch.ItemsSource = await GetAutoSuggestResults(asbBingsearch.Text);
                }
                catch (HttpRequestException)
                {
                    // default to no suggestions
                    asbBingsearch.ItemsSource = null;
                }
            }

        }
        public static async Task<IEnumerable<string>> GetAutoSuggestResults(string query)
        {
            List<string> suggestions = new List<string>();
            string market = "en-US";

            var result = await autoSuggestionClient.GetAsync(string.Format("{0}/?q={1}&mkt={2}", AutoSuggestionEndPoint, WebUtility.UrlEncode(query), market));
            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);
            if (data.suggestionGroups != null && data.suggestionGroups.Count > 0 &&
                data.suggestionGroups[0].searchSuggestions != null)
            {
                for (int i = 0; i < data.suggestionGroups[0].searchSuggestions.Count; i++)
                {
                    suggestions.Add(data.suggestionGroups[0].searchSuggestions[i].displayText.Value);
                }
            }

            return suggestions;
        }

        //News Search
        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            var news = await GetNewsSearchResults(this.asbBingsearch.Text, count: 50, offset: 0, market: "en-US");
            for (int i = 0; i < news.Count(); i++)
            {
                NewsArticle article = news.ElementAt(i);
                SearchResults.Add(new News { Article = article });
            }

        }
        public static async Task<IEnumerable<NewsArticle>> GetNewsSearchResults(string query, int count = 20, int offset = 0, string market = "en-US")
        {

            List<NewsArticle> articles = new List<NewsArticle>();
            var result = await searchClient.GetAsync(string.Format("{0}/?q={1}&count={2}&offset={3}&mkt={4}", NewsSearchEndPoint, WebUtility.UrlEncode(query), count, offset, market));
            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);

            if (data.value != null && data.value.Count > 0)
            {
                for (int i = 0; i < data.value.Count; i++)
                {
                    articles.Add(new NewsArticle
                    {
                        Title = data.value[i].name,
                        Url = data.value[i].url,
                        Description = data.value[i].description,
                        ThumbnailUrl = data.value[i].image?.thumbnail?.contentUrl,
                        Provider = data.value[i].provider?[0].name
                    });

                }
            }
            return articles;
        }


    }
}
