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
public class ViedoDet
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string ContentUrl { get; set; }
    public string Publisher { get; set; }


}

public class Videocollection
{
    public ViedoDet videocol { get; set; }
}

namespace UWPBingVideoSearch
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public string query;
        public ObservableCollection<Videocollection> SearchResults { get; set; } = new ObservableCollection<Videocollection>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnVideoSearch_Click(object sender, RoutedEventArgs e)
        {
            query = asbVideo.Text.Trim();
            var res = await videoSearch();
            for (int i = 0; i < res.Count(); i++)
            {
                ViedoDet vser = res.ElementAt(i);
                SearchResults.Add(new Videocollection { videocol = vser });
               }
        }
        async Task<IEnumerable<ViedoDet>> videoSearch()
        {

            List<ViedoDet> sres = new List<ViedoDet>();
            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<Your Key>");

            // Request parameters

            string count = "10";
            string offset = "0";
            string mkt = "en-us";

            var VideoSearchEndPoint = "https://api.cognitive.microsoft.com/bing/v5.0/videos/search?";
            var result = await client.GetAsync(string.Format("{0}q={1}&count={2}&offset={3}&mkt={4}", VideoSearchEndPoint, WebUtility.UrlEncode(query), count, offset, mkt));

            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);

            for (int i = 0; i < 10; i++)
            {
                sres.Add(new ViedoDet
                {
                    Title = data.value[i].name,
                    Url = data.value[i].thumbnailUrl,
                    Description = data.value[i].description,
                    ContentUrl = data.value[i].contentUrl,
                    Publisher = data.value[i].publisher?[0].name
                });

            }
            return sres;
        }

    }
}
