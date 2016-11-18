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

namespace UWPCogBingWeb
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public class WebSer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Snippet { get; set; }
        public string DisplayUrl { get; set; }
    }

    public class ws
    {
        public WebSer WSer { get; set; }
    }


    public sealed partial class MainPage : Page
    {
        public string q;
        public ObservableCollection<ws> SearchResults { get; set; } = new ObservableCollection<ws>();

        public MainPage()
        {
            this.InitializeComponent();
        }
        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            q = asbWebSearch.Text.Trim();
            var res = await MakeRequest();
            for (int i = 0; i < res.Count(); i++)
            {
                WebSer webser = res.ElementAt(i);
                SearchResults.Add(new ws { WSer = webser });

            }
        }
        async Task<IEnumerable<WebSer>> MakeRequest()
        {

            List<WebSer> sres = new List<WebSer>();
            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "8e99d7e95ddb4bc7b19b097f429bd97d");

            // Request parameters

            string count = "10";
            string offset = "0";
            string mkt = "en-us";

            var WebSearchEndPoint = "https://api.cognitive.microsoft.com/bing/v5.0/search?";
            var result = await client.GetAsync(string.Format("{0}q={1}&count={2}&offset={3}&mkt={4}", WebSearchEndPoint, WebUtility.UrlEncode(q), count, offset, mkt));

            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);

            for (int i = 0; i < 10; i++)
            {
                sres.Add(new WebSer
                {
                    Id = data.webPages.value[i].id,
                    Name = data.webPages.value[i].name,
                    Url = data.webPages.value[i].url,
                    Snippet = data.webPages.value[i].snippet,
                    DisplayUrl = data.webPages.value[i].displayUrl

                });

            }
            return sres;
        }

    }
}
