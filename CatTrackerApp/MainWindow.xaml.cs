using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using Intel.RealSense;
using Stream = Intel.RealSense.Stream;
using System.Windows.Threading;
using System.Diagnostics;

namespace DistRS
{
    public partial class MainWindow : Window
    {
        private Pipeline pipe;
        private Colorizer colorizer;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        int height = 240;
        int width = 320;
        float[] distArray = new float[((320 / 10) * (240 / 10))];
        float[] callibrationArray = new float[((320 / 10) * (240 / 10))];

        private SerialCommunication com;
        int currentCorner = 0;
        int pixelCount = 0;
        bool playing = false;

        static Action<VideoFrame> UpdateImage(System.Windows.Controls.Image img)
        {
            var bmap = img.Source as WriteableBitmap;
            return new Action<VideoFrame>(frame =>
            {
                var rect = new Int32Rect(0, 0, frame.Width, frame.Height);
                bmap.WritePixels(rect, frame.Data, frame.Stride * frame.Height, frame.Stride);
            });
        }
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                Action<VideoFrame> updateDepth;
                Action<VideoFrame> updateColor;

                // The colorizer processing block will be used to visualize the depth frames.
                colorizer = new Colorizer();

                // Create and config the pipeline to strem color and depth frames.
                pipe = new Pipeline();

                using (var ctx = new Context())
                {
                    var devices = ctx.QueryDevices();
                    var dev = devices[0];

                    Console.WriteLine("\nUsing device 0, an {0}", dev.Info[CameraInfo.Name]);
                    Console.WriteLine("    Serial number: {0}", dev.Info[CameraInfo.SerialNumber]);
                    Console.WriteLine("    Firmware version: {0}", dev.Info[CameraInfo.FirmwareVersion]);

                    var sensors = dev.QuerySensors();
                    var depthSensor = sensors[0];
                    var colorSensor = sensors[1];

                    var depthProfile = depthSensor.StreamProfiles
                                        .Where(p => p.Stream == Stream.Depth)
                                        .OrderBy(p => p.Framerate)
                                        .Select(p => p.As<VideoStreamProfile>()).First();

                    var colorProfile = colorSensor.StreamProfiles
                                        .Where(p => p.Stream == Stream.Color)
                                        .OrderBy(p => p.Framerate)
                                        .Select(p => p.As<VideoStreamProfile>()).First();

                    var cfg = new Config();
                    cfg.EnableStream(Stream.Depth, 320, 240, depthProfile.Format, depthProfile.Framerate);
                    cfg.EnableStream(Stream.Color, colorProfile.Width, colorProfile.Height, colorProfile.Format, colorProfile.Framerate);


                    var pp = pipe.Start(cfg);

                    SetupWindow(pp, out updateDepth, out updateColor);
                }
                Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipe.WaitForFrames())
                        {
                            var colorFrame = frames.ColorFrame.DisposeWith(frames);
                            var depthFrame = frames.DepthFrame.DisposeWith(frames);

                            var colorizedDepth = colorizer.Process<VideoFrame>(depthFrame).DisposeWith(frames);

                            Dispatcher.Invoke(DispatcherPriority.Render, updateDepth, colorizedDepth);
                            Dispatcher.Invoke(DispatcherPriority.Render, updateColor, colorFrame);

                            Dispatcher.Invoke(new Action(() =>
                            {
                                String depth_dev_sn = depthFrame.Sensor.Info[CameraInfo.SerialNumber];
                            }));
                        }
                    }
                }, tokenSource.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Shutdown();
            }
        }

        private void Control_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tokenSource.Cancel();
        }

        private void SetupWindow(PipelineProfile pipelineProfile, out Action<VideoFrame> depth, out Action<VideoFrame> color)
        {
            using (var vsp = pipelineProfile.GetStream(Stream.Depth).As<VideoStreamProfile>())
                imgDepth.Source = new WriteableBitmap(vsp.Width, vsp.Height, 96d, 96d, PixelFormats.Rgb24, null);
            depth = UpdateImage(imgDepth);

            using (var vsp = pipelineProfile.GetStream(Stream.Color).As<VideoStreamProfile>())
                imgColor.Source = new WriteableBitmap(vsp.Width, vsp.Height, 96d, 96d, PixelFormats.Rgb24, null);
            color = UpdateImage(imgColor);
        }

        private void ButtonReadDist_Click(object sender, RoutedEventArgs e)
        {
            using (var frames = pipe.WaitForFrames())
            using (var depth = frames.DepthFrame)
            {
                int x = int.Parse(Xcoordinate.Text) - 1;
                int y = int.Parse(Ycoordinate.Text) - 1;
                Console.WriteLine("The camera is pointing at an object " +
                    depth.GetDistance(x, y) + " meters away\t");
                tbResult.Text = "Distance is " + depth.GetDistance(x, y) + " m";
                depth.Dispose();
                frames.Dispose();
            }
        }

        // Create a callibration distance array
        private void ButtonCallibrate_Click(object sender, RoutedEventArgs e)
        {
            tbDotCount.Text = "0";
            bool isObstructed = CheckPixels(callibrationArray);
            if (isObstructed)
            {
                MessageBox.Show("There's something in the way, either a cat or the sensor isnt placed correctly! Please try again after moving it!");
            }
            else
            {
                MessageBox.Show("Callibration succesfull!");
            }
        }
        // Compare current to callibration
        private void ButtonCompare_Click(object sender, RoutedEventArgs e)
        {
            CheckPixels(distArray);
            ComparePixels();
        }

        private float GetDistance(int x, int y)
        {
            float num = 0;
            using (var frames = pipe.WaitForFrames())
            using (var depth = frames.DepthFrame)
            {
                num = depth.GetDistance(x, y);
                depth.Dispose();
                frames.Dispose();
            }
            return num;
        }

        private bool CheckPixels(float[] array)
        {
            bool heightDif = false;
            int num = -1;

            using (var frames = pipe.WaitForFrames())
            using (var depth = frames.DepthFrame)
            {
                for (int i = 0; i < width; i++) // Check Width 1020/10 pixels.
                {
                    for (int j = 0; j < height; j++) // Check Height 760/10 pixels.
                    {
                        if (i % 10 == 0 && j % 10 == 0)
                        {
                            num++;
                            array[num] = depth.GetDistance(i, j);
                        }
                        
                    }
                }
                depth.Dispose();
                frames.Dispose();
            }
            // Check if there's something in front of the camera.
            // By comparing the closest dots in distance, the difference can't be too much.
            if (array == callibrationArray)
            {
                int counter = 0;
                float lastVal = callibrationArray[0];
                float lastTopVal = callibrationArray[0];

                foreach (float dist in array)
                {
                    counter++;
                    if (counter % 24 == 1)
                    {
                        // Difference over 10cm.
                        if (dist < lastTopVal && lastTopVal - dist >= 0.1)
                        {
                            heightDif = true;
                        }
                        else if (dist > lastTopVal && dist - lastTopVal >= 0.1)
                        {
                            heightDif = true;
                        }

                        lastTopVal = dist;
                    }
                    // Compare to last
                    else
                    {
                        // Difference over 10cm.
                        if (dist < lastVal && lastVal - dist >= 0.1)
                        {
                            heightDif = true;
                        }
                        else if (dist > lastVal && dist - lastVal >= 0.1)
                        {
                            heightDif = true;
                        }
                    }
                    lastVal = dist;
                }
            }
            return heightDif;
        }

        private void ComparePixels()
        {
            // 768 pixels 32x24.
            // Y-axis first followed by the X-axis.
            pixelCount = 0;
            int x = 0;
            int y = -1;

            for (int i = 0; i < callibrationArray.Length; i++)
            {
                // Sort the squares. 3x3 grid.
                // Counting the position.
                y++;
                if ((i + 1) % 24 == 0)
                {
                    y = 0;
                    x++;
                }

                switch (currentCorner)
                {
                    case 1: // Top left.

                        if (x < 10 && y < 8)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                    case 2: // Top Middle.

                        if (x > 9 && x < 22 && y < 8)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                    case 3: // Top right.
                        if (x > 21 && y < 8)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                    case 4:// Middle Left.
                        if (x < 10 && y > 7 && y < 16)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                    case 5: // Middle Middle.
                        if (x > 9 && x < 22 && y > 7 && y < 16)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                    case 6: // Middle Right.
                        if (x > 21 && y > 7 && y < 16)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                    case 7: // Bottom Left.
                        if (x < 10 && y > 15)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                    case 8: // Bottom Middle.
                        if (x > 9 && x < 22 && y > 15)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                    case 9:  // Bottom right.
                        if (x > 21 && y > 15)
                        {
                            if (distArray[i] + 0.1 < callibrationArray[i])
                            {
                                pixelCount++;
                            }
                        }
                        break;
                }
            }

            tbDotCount.Text = pixelCount.ToString() + " / " + callibrationArray.Length + " (" + currentCorner + ")";
        }


        // Connect to the arduino and start playing.
        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            com = new SerialCommunication();
            currentCorner = 1;
            SquareHitSendMessage(currentCorner);

            playing = true;
            while (playing)
            {
                CheckPixels(distArray);
                ComparePixels();
                if (pixelCount > 20) // Max if all pixels count = 80.
                {
                    nextSquare();
                }
            }
        }

        private void nextSquare()
        {
            Random ran = new Random();
            int num = currentCorner + ran.Next(1,9);
            if (num > 9)
            {
                num %= 9;
            }
            currentCorner = num;
            SquareHitSendMessage(num);
        }

        private void SquareHitSendMessage(int val)
        {
            com.Connect();
            com.SendMessage("#" + val + "%");
            com.Disconnect();
        }
    }
}