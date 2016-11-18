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

namespace UWPCogBingImage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public class ImgSer
    {
        public string Name { get; set; }
        public string ContentUrl { get; set; }
    }

    public class ImgCol
    {
        public ImgSer imco { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        public string  query;
        public ObservableCollection<ImgCol> SearchResults { get; set; } = new ObservableCollection<ImgCol>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnImgSearch_Click(object sender, RoutedEventArgs e)
        {
            query = asbImage.Text.Trim();
            var res = await imgSearch();
            for (int i = 0; i < res.Count(); i++)
            {
                ImgSer imser = res.ElementAt(i);
                SearchResults.Add(new ImgCol { imco = imser });

            }

        }
        async Task<IEnumerable<ImgSer>> imgSearch()
        {

            List<ImgSer> sres = new List<ImgSer>();
            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "8e99d7e95ddb4bc7b19b097f429bd97d");

            // Request parameters

            string count = "10";
            string offset = "0";
            string mkt = "en-us";

            var ImgSearchEndPoint = "https://api.cognitive.microsoft.com/bing/v5.0/images/search?";
            var result = await client.GetAsync(string.Format("{0}q={1}&count={2}&offset={3}&mkt={4}", ImgSearchEndPoint, WebUtility.UrlEncode(query), count, offset, mkt));

            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);

            for (int i = 0; i < 10; i++)
            {
                sres.Add(new ImgSer
                {
                    Name = data.value[i].name,
                    ContentUrl = data.value[i].contentUrl,
                });

            }
            return sres;
        }
             
    }
}
