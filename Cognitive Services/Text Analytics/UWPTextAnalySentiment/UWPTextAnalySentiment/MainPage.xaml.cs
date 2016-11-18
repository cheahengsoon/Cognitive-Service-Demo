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
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPTextAnalySentiment
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public class sentimentc
    {
        public string Scores { get; set; }
        public string Id { get; set; }
    }

    public class Kpcol
    {
        public sentimentc stco { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        static string requestString, text;

        public ObservableCollection<Kpcol> SentimentResults { get; set; } = new ObservableCollection<Kpcol>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnSentiment_Click(object sender, RoutedEventArgs e)
        {
            text = txtInput.Text;
            var ss = await getSentimentScore();
            for (int i = 0; i < ss.Count(); i++)
            {
                sentimentc sc = ss.ElementAt(i);
                SentimentResults.Add(new Kpcol { stco = sc });

            }
        }
        static async Task<IEnumerable<sentimentc>> getSentimentScore()
        {

            List<sentimentc> sentli = new List<sentimentc>();
            var client = new HttpClient();

            string language = "en";
            string[] input = new string[] { text };
            if (input != null)
            {
                // Request body. 
                requestString = "{\"documents\":[";
                for (int i = 0; i < input.Length; i++)
                {
                    requestString += string.Format("{{\"id\":\"{0}\",\"text\":\"{1}\", \"language\":\"{2}\"}}", i, input[i].Replace("\"", "'"), language);
                    if (i != input.Length - 1)
                    {
                        requestString += ",";
                    }
                }

                requestString += "]}";
            }
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<Your Key>");

            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment?";

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(requestString);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }

            string content1 = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Text Analytics failed. " + content1);
            }
            dynamic data = JObject.Parse(content1);

            if (data.documents != null)
            {
                for (int i = 0; i < data.documents.Count; i++)
                {
                    sentli.Add(new sentimentc
                    {
                        Scores = text+"   Sentiment Score is : " + data.documents[i].score,
                        Id = data.documents[i].id
                    });

                }
            }

            return sentli;

        }

    }
}
