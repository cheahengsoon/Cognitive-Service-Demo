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

using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.Storage;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPCogFace
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly IFaceServiceClient fsClient = new FaceServiceClient("7acc16dcd7bb407c8795d1d23deb5004");
        FaceRectangle[] fRectangles;
        public MainPage()
        {
            this.InitializeComponent();
            UploadAndDetectFaces("ms-appx:///Assets/02.png");
        }
        async void UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder assets = await appInstalledFolder.GetFolderAsync("Assets");
                var storageFile = await assets.GetFileAsync("01.JPG");
                var randomAccessStream = await storageFile.OpenReadAsync();

                using (Stream stream = randomAccessStream.AsStreamForRead())
                {
                    //this is the fragment where face is recognized:
                    var faces = await fsClient.DetectAsync(stream);
                    var faceRects = faces.Select(face => face.FaceRectangle);
                    fRectangles = faceRects.ToArray();
                    CanFaceRect.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

    private void CanFaceRect_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if (fRectangles != null)
                if (fRectangles.Length > 0)
                {
                    foreach (var faceRect in fRectangles)
                    {
                        args.DrawingSession.DrawRectangle(faceRect.Left, faceRect.Top, faceRect.Width, faceRect.Height, Colors.Red);
                    }
                }
        }

    }
}

