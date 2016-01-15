using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.ProjectOxford.Emotion;
using System.Net.Http;
using System.IO;
using System.Drawing.Imaging;

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

        private CascadeClassifier _faceclassifier = new CascadeClassifier(@"haarcascade_frontalface_default.xml");

        private void ProcessFrame()
        {
            Image<Bgr, Byte> ImageFrame = _capture.QueryFrame().ToImage<Bgr, Byte>();  //Capture the frame.
            ImageFrame = DetectFaces(ImageFrame);
            image.Source = ToBitmapSource(ImageFrame);  //line 2
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

        async Task<int> GetEmotion(Image<Bgr, Byte> myImage)
        {
            // You need to add a reference to System.Net.Http to declare client.
            HttpClient client = new HttpClient();
            MemoryStream imageStream = new MemoryStream();
            System.Drawing.Image img = myImage.ToBitmap();
            img.Save(imageStream, ImageFormat.Jpeg);
            string mySubscriptionKey = "xxx";
            EmotionServiceClient myEmotion = new EmotionServiceClient(mySubscriptionKey);
            //await myEmotion.RecognizeAsync();

            // GetStringAsync returns a Task<string>. That means that when you await the
            // task you'll get a string (urlContents).
            Task<string> getStringTask = client.GetStringAsync("http://msdn.microsoft.com");

            // You can do work here that doesn't rely on the string from GetStringAsync.
            //DoIndependentWork();

            // The await operator suspends AccessTheWebAsync.
            //  - AccessTheWebAsync can't continue until getStringTask is complete.
            //  - Meanwhile, control returns to the caller of AccessTheWebAsync.
            //  - Control resumes here when getStringTask is complete. 
            //  - The await operator then retrieves the string result from getStringTask.
            string urlContents = await getStringTask;

            // The return statement specifies an integer result.
            // Any methods that are awaiting AccessTheWebAsync retrieve the length value.
            return urlContents.Length;
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
            myTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            myTimer.Start();
        }
    }
}
