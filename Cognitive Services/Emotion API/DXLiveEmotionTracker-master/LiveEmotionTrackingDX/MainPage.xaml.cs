using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LiveEmotionTrackingDX
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {


        private const string FaceApiKey = "2698749f4e5340a3a6e2531ac29d4960";
        private const string EmotionApiKey = "35e54071335b4f0c8ee654205aa2de5c";

        private static readonly FaceServiceClient faceServiceClient = new FaceServiceClient(FaceApiKey);
        private static readonly EmotionServiceClient emotionServiceClient = new EmotionServiceClient(EmotionApiKey);

        /// <summary>
        /// Brush for drawing the bounding box around each identified face.
        /// </summary>
        private readonly SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);

        /// <summary>
        /// Thickness of the face bounding box lines.
        /// </summary>
        private readonly double lineThickness = 2.0;

        /// <summary>
        /// Transparent fill for the bounding box.
        /// </summary>
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

        /// <summary>
        /// References a MediaCapture instance; is null when not in Streaming state.
        /// </summary>
        private MediaCapture mediaCapture;

        /// <summary>
        /// Cache of properties from the current MediaCapture device which is used for capturing the preview frame.
        /// </summary>
        private VideoEncodingProperties videoProperties;

        /// <summary>
        /// References a FaceTracker instance.
        /// </summary>
        private FaceTracker faceTracker;

        /// <summary>
        /// A periodic timer to execute FaceTracker on preview frames
        /// </summary>
        private ThreadPoolTimer frameProcessingTimer;

        /// <summary>
        /// Semaphore to ensure FaceTracking logic only executes one at a time
        /// </summary>
        private SemaphoreSlim frameProcessingSemaphore = new SemaphoreSlim(1);

        private Color color;


        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (this.faceTracker == null)
            {
                this.faceTracker = await FaceTracker.CreateAsync();
            }

            await StartWebcamStreaming();
        }


        private async Task<bool> StartWebcamStreaming()
        {
            bool successful = true;

            try
            {
                color = GetColor();

                DeviceInformation cameraDevice;

                var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                cameraDevice = desiredDevice ?? allVideoDevices.FirstOrDefault();
                if (cameraDevice == null) throw new Exception("No camera found on device");


                this.mediaCapture = new MediaCapture();

                // For this scenario, we only need Video (not microphone) so specify this in the initializer.
                // NOTE: the appxmanifest only declares "webcam" under capabilities and if this is changed to include
                // microphone (default constructor) you must add "microphone" to the manifest or initialization will fail.
                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id, StreamingCaptureMode = StreamingCaptureMode.Video };

                await this.mediaCapture.InitializeAsync(settings);

                // Cache the media properties as we'll need them later.
                var deviceController = this.mediaCapture.VideoDeviceController;
                this.videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                // Immediately start streaming to our CaptureElement UI.
                // NOTE: CaptureElement's Source must be set before streaming is started.
                this.CamPreview.Source = this.mediaCapture;
                await this.mediaCapture.StartPreviewAsync();
                
                TimeSpan timerInterval = TimeSpan.FromMilliseconds(250);
                this.frameProcessingTimer = Windows.System.Threading.ThreadPoolTimer.CreatePeriodicTimer(new Windows.System.Threading.TimerElapsedHandler(ProcessCurrentVideoFrame), timerInterval);
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
            
            return successful;
        }



        private async void ProcessCurrentVideoFrame(ThreadPoolTimer timer)
        {

            // If a lock is being held it means we're still waiting for processing work on the previous frame to complete.
            // In this situation, don't wait on the semaphore but exit immediately.
            if (!frameProcessingSemaphore.Wait(0))
            {
                return;
            }

            try
            {
                IList<DetectedFace> faces = null;

                // Create a VideoFrame object specifying the pixel format we want our capture image to be (NV12 bitmap in this case).
                // GetPreviewFrame will convert the native webcam frame into this format.
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;
                using (VideoFrame previewFrame = new VideoFrame(InputPixelFormat, (int)this.videoProperties.Width, (int)this.videoProperties.Height))
                {
                    await this.mediaCapture.GetPreviewFrameAsync(previewFrame);

                    //Setting the format of the picture to send to cognitive services
                    var imageEncodingProp = ImageEncodingProperties.CreateJpeg();

                    var stream = new InMemoryRandomAccessStream();

                    //Capturing the picture and stoing into the Main memory
                    await mediaCapture.CapturePhotoToStreamAsync(imageEncodingProp, stream);

                    stream.Seek(0);

                    //Making a copy of the bite stream to send it to the cognitive services
                    var age_stream = stream.CloneStream();

                    //Getting the list of the emotions of the faces
                    var emotions = await GetEmotions(stream.AsStreamForRead());

                    //Getting the list of the gender and age of the faces
                    var ageandgender = await GetFaces(age_stream.AsStreamForRead());


                    // The returned VideoFrame should be in the supported NV12 format but we need to verify this.
                    if (FaceDetector.IsBitmapPixelFormatSupported(previewFrame.SoftwareBitmap.BitmapPixelFormat))
                    {
                        //Returning the dected faces using the Media analysis library in .Net no need for internet here
                        faces = await this.faceTracker.ProcessNextFrameAsync(previewFrame);
                    }
                    else
                    {
                        throw new System.NotSupportedException("PixelFormat '" + InputPixelFormat.ToString() + "' is not supported by FaceDetector");
                    }

                    // Create our visualization using the frame dimensions and face results but run it on the UI thread.
                    var previewFrameSize = new Windows.Foundation.Size(previewFrame.SoftwareBitmap.PixelWidth, previewFrame.SoftwareBitmap.PixelHeight);
                    var ignored = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.SetupVisualization(previewFrameSize, faces, emotions, ageandgender);
                    });
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                frameProcessingSemaphore.Release();
            }

        }

        /// <summary>
        /// Takes the webcam image and FaceTracker results and assembles the visualization onto the Canvas.
        /// </summary>
        /// <param name="framePizelSize">Width and height (in pixels) of the video capture frame</param>
        /// <param name="foundFaces">List of detected faces; output from FaceTracker</param>
        /// <param name="emotions">List of the emotions of the faces</param>
        /// <param name="age_gender">List the age and gender for each face</param>
        private void SetupVisualization(Windows.Foundation.Size framePizelSize, IList<DetectedFace> foundFaces, Emotion[] emotions, Face[] age_gender)
        {
            this.VisualizationCanvas.Children.Clear();

            double actualWidth = this.VisualizationCanvas.ActualWidth;
            double actualHeight = this.VisualizationCanvas.ActualHeight;

            if (foundFaces != null && actualWidth != 0 && actualHeight != 0)
            {
                double widthScale = framePizelSize.Width / actualWidth;
                double heightScale = framePizelSize.Height / actualHeight;

                var facesAndemotions = foundFaces.Zip(emotions, (a, e) => new { face = a, emotion = e });

                int i = 0;

                foreach (var faceAndemotion in facesAndemotions)
                {
                    // Create a square element for displaying the face box and scaling it

                    this.lineBrush.Color = color;
                    Rectangle square = new Rectangle();
                    square.Width = (uint)(faceAndemotion.face.FaceBox.Width / widthScale);
                    square.Height = (uint)(faceAndemotion.face.FaceBox.Height / heightScale);
                    square.Fill = this.fillBrush;
                    square.Stroke = this.lineBrush;
                    square.StrokeThickness = this.lineThickness;
                    square.Margin = new Thickness((uint)(faceAndemotion.face.FaceBox.X / widthScale), (uint)(faceAndemotion.face.FaceBox.Y / heightScale), 0, 0);

                    this.VisualizationCanvas.Children.Add(square);

                    TextBlock emotionsListTextBlock = new TextBlock();
                    emotionsListTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    emotionsListTextBlock.FontSize = 10;
                    emotionsListTextBlock.Text = BuildEmotionList(faceAndemotion.emotion.Scores.ToRankedList());
                    Border recommendationInfoBorder = new Border();
                    recommendationInfoBorder.Background = new SolidColorBrush(color);
                    recommendationInfoBorder.Padding = new Thickness(1);
                    recommendationInfoBorder.Child = emotionsListTextBlock;
                    recommendationInfoBorder.HorizontalAlignment = HorizontalAlignment.Left;
                    recommendationInfoBorder.VerticalAlignment = VerticalAlignment.Top;
                    recommendationInfoBorder.Margin = new Thickness(square.Margin.Left, square.Margin.Top + square.Height, 0, 0);
                    this.VisualizationCanvas.Children.Add(recommendationInfoBorder);


                    TextBlock faceInfoTextBlock = new TextBlock();
                    faceInfoTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    faceInfoTextBlock.FontSize = 14;
                    faceInfoTextBlock.Text = $"{GetGenderString(age_gender.ElementAt(i).FaceAttributes.Gender)}, {Math.Floor(age_gender.ElementAt(i).FaceAttributes.Age)}" + " Ans";
                    Border faceInfoBorder = new Border();
                    faceInfoBorder.Background = new SolidColorBrush(color);
                    faceInfoBorder.Padding = new Thickness(5);
                    faceInfoBorder.Child = faceInfoTextBlock;
                    faceInfoBorder.HorizontalAlignment = HorizontalAlignment.Left;
                    faceInfoBorder.VerticalAlignment = VerticalAlignment.Top;
                    faceInfoBorder.Margin = new Thickness(square.Margin.Left, square.Margin.Top - 29, 0, 0);
                    this.VisualizationCanvas.Children.Add(faceInfoBorder);

                    i++;
                }
            }
        }


        private static async Task<Emotion[]> GetEmotions(Stream stream)
        {

            var result = await emotionServiceClient.RecognizeAsync(stream);
            return result;
        }


        private string BuildEmotionList(IEnumerable<KeyValuePair<string, float>> scores)
        {

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine(" Vos emotions");

            for (int i = 0; i < 3 && Math.Round(scores.ElementAt(i).Value, 3) > 0.0099; i++)
            {
                sb.AppendLine("   " + scores.ElementAt(i).Key + " : " + Math.Round(scores.ElementAt(i).Value, 3) + "   ");
            }

            return sb.ToString();
        }

        private Color GetColor()
        {
            Random rnd = new Random();

            return Color.FromArgb(250, Convert.ToByte(rnd.Next(0, 255)), Convert.ToByte(rnd.Next(0, 150)), 1);
        }

        private static async Task<Face[]> GetFaces(Stream stream)
        {
            //implemting local variable to get face attributes
            var requiredFaceAttributes = new FaceAttributeType[] {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.Glasses
            };

            var result = await faceServiceClient.DetectAsync(stream, true, true, returnFaceAttributes: requiredFaceAttributes);

            return result;
        }

        private string GetGenderString(string originalValue)
        {
            if (originalValue == "male")
                return "Homme";
            else
                return "Femme";
        }


    }
}
