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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPCogBingAutoSuggest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private static string AutoSuggestionEndPoint = "https://api.cognitive.microsoft.com/bing/v5.0/suggestions";
        private static HttpClient autoSuggestionClient { get; set; }
        public MainPage()
        {
            this.InitializeComponent();
            autoSuggestionClient = new HttpClient();
            autoSuggestionClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "565f0ae336d8467bb6d1769ec8460266");
    }

        private async void asbBing_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                try
                {
                    this.asbBing.ItemsSource = await GetAutoSuggestResults(this.asbBing.Text);
                }
                catch (HttpRequestException)
                {
                    // default to no suggestions
                    this.asbBing.ItemsSource = null;
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

    }
}
