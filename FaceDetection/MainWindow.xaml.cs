using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FaceDetection
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        [System.Runtime.InteropServices.DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop
                  .Imaging.CreateBitmapSourceFromHBitmap(
                  ptr,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        private static string subscriptionKey = "ef796b256674419e9c17e2707f28cc50";
        private static EmotionServiceClient emotionServiceClient = new EmotionServiceClient(subscriptionKey);

        public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }

        private CascadeClassifier _faceclassifier = new CascadeClassifier(@"haarcascade_frontalface_default.xml");

        private async void ProcessFrame()
        {
            Image<Bgr, Byte> ImageFrame = _capture.QueryFrame().ToImage<Bgr, Byte>();  //Capture the frame.
            Task<Emotion[]> emotionTask = UploadAndDetectEmotions(ImageFrame);
            ImageFrame = DetectFaces(ImageFrame);
            image.Source = ToBitmapSource(ImageFrame);  //line 2
            Emotion[] myEmotions = await emotionTask;
            if (myEmotions != null)
            {
                foreach (Emotion emotion in myEmotions)
                {
                    Log("Emotion[" + 'd' + "]");
                    Log("  .FaceRectangle = left: " + emotion.FaceRectangle.Left
                             + ", top: " + emotion.FaceRectangle.Top
                             + ", width: " + emotion.FaceRectangle.Width
                             + ", height: " + emotion.FaceRectangle.Height);

                    Log("  Anger    : " + emotion.Scores.Anger.ToString());
                    Log("  Contempt : " + emotion.Scores.Contempt.ToString());
                    Log("  Disgust  : " + emotion.Scores.Disgust.ToString());
                    Log("  Fear     : " + emotion.Scores.Fear.ToString());
                    Log("  Happiness: " + emotion.Scores.Happiness.ToString());
                    Log("  Neutral  : " + emotion.Scores.Neutral.ToString());
                    Log("  Sadness  : " + emotion.Scores.Sadness.ToString());
                    Log("  Surprise  : " + emotion.Scores.Surprise.ToString());
                    Log("");
                }
            }
        }

        public void Log(string message)
        {
            textBox.Text += message + "\n";
        }

        private Image<Bgr, Byte> DetectFaces(Image<Bgr, Byte> myImage)
        {
            var grayFrame = myImage.Convert<Gray, byte>();
            var myFaces = _faceclassifier.DetectMultiScale(grayFrame);
            var noFaces = myFaces.Count();
            label.Content = noFaces;
            foreach (var face in myFaces)
                myImage.Draw(face, new Bgr(100, 100, 100), 3);
            return myImage;
        }

        private async Task<Emotion[]> UploadAndDetectEmotions(Image<Bgr, Byte> imageSource)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            Log("EmotionServiceClient is created");

            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE STARTS HERE
            // -----------------------------------------------------------------------

            //
            // Create Project Oxford Emotion API Service client
            //

            Log("Calling EmotionServiceClient.RecognizeAsync()...");
            try
            {
                Emotion[] emotionResult;
                MemoryStream imageStream = new MemoryStream();
                imageSource.ToBitmap().Save(imageStream, ImageFormat.Bmp);
                using (imageStream)
                {
                    //
                    // Detect the emotions in the URL
                    //
                    emotionResult = await emotionServiceClient.RecognizeAsync(imageStream);
                    return emotionResult;
                }
            }
            catch (Exception exception)
            {
                Log(exception.ToString());
                return null;
            }
            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE ENDS HERE
            // -----------------------------------------------------------------------

        }

        private Capture _capture = new Capture();
        DispatcherTimer myTimer;

        void timer_Tick(object sender, EventArgs e)
        {
            ProcessFrame();

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            myTimer = new DispatcherTimer();
            myTimer.Tick += new EventHandler(timer_Tick);
            myTimer.Interval = new TimeSpan(0, 0, 0, 5, 000);
            myTimer.Start();
        }
    }
}
