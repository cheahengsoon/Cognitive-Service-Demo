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
using System.Net;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
namespace UWPCogAcadCalchistogram
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public class Interpret
    {
        public string Title { get; set; }
        public string output { get; set; }
    }
    public class Histogramc
    {
        public string TotalCount { get; set; }
        public string Value { get; set; }
        public string Count { get; set; }
    }
    public class histocol
    {
        public Histogramc hisco { get; set; }
    }
    public sealed partial class MainPage : Page
    {
        public static string expr, query;
        public ObservableCollection<histocol> SearchResults { get; set; } = new ObservableCollection<histocol>();
        public MainPage()
        {
            this.InitializeComponent();
        }
        private async void btnAcKnHist_Click(object sender, RoutedEventArgs e)
        {
            query = asbHisto.Text.Trim();
            var res = await interpret();

            for (int i = 0; i < res.Count(); i++)
            {
                Interpret wres = res.ElementAt(i);
                expr = wres.output;
             }
            var hist = await histogram();
            for (int i = 0; i < hist.Count(); i++)
            {
                Histogramc hh = hist.ElementAt(i);
                SearchResults.Add(new histocol { hisco = hh });
                tblTotal.Text = hh.TotalCount;
            }
            (Column.Series[0] as ColumnSeries).ItemsSource = hist; //Column Chart display
        }
       async Task<IEnumerable<Histogramc>> histogram()
        {
            List<Histogramc> hiscc = new List<Histogramc>();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<Your Acadamic Knowledge API Key>");
            string count = "4";
            string offset = "0";
            string model = "latest";
            string attributes = "Y,F.FN";
            var histoEndPoint = "https://api.projectoxford.ai/academic/v1.0/calchistogram?";
            var result = await client.GetAsync(string.Format("{0}expr={1}&model={2}&count={3}&offset={4}&attributes={5}", histoEndPoint, expr, model, count, offset, attributes));
            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);
            for (int i = 0; i < 4; i++)
            {
                hiscc.Add(new Histogramc
                {
                    TotalCount = "Total Count : " + data.histograms[0].total_count,
                    Value = data.histograms[0].histogram[i].value,
                    Count = data.histograms[0].histogram[i].count
                });

            }
            return hiscc;
        }
        
        static async Task<IEnumerable<Interpret>> interpret()
        {
            var iclient = new HttpClient();
            List<Interpret> ire = new List<Interpret>();
            iclient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<Your Acadamic Knowledge API Key>");
            string count = "2";
            string Complete = "1";
            var inteSearchEndPoint = "https://api.projectoxford.ai/academic/v1.0/interpret?";
            var result = await iclient.GetAsync(string.Format("{0}query=papers by +{1}&complete={2}&count={3}", inteSearchEndPoint, WebUtility.UrlEncode(query), Complete, count));
            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);
            for (int i = 0; i < 1; i++)
            {
                ire.Add(new Interpret
                {
                    Title = data.interpretations[i].rules[i].name,
                    output = data.interpretations[i].rules[i].output.value

                });
            }
            return ire;
        }
    }
}
