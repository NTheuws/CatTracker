﻿using System;
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

namespace DistRS
{
    public partial class MainWindow : Window
    {
        int count = 0;
        private Pipeline pipe;
        private Colorizer colorizer;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        int height = 240;
        int width = 320;
        float[] distArray = new float[((240 / 10) * (320 / 10))];
        float[] callibrationArray = new float[((240 / 10) * (320 / 10))];


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
                    cfg.EnableStream(Stream.Depth, depthProfile.Width, depthProfile.Height, depthProfile.Format, depthProfile.Framerate);
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
                            count++;
                            Console.WriteLine(count);
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
            CheckPixels(callibrationArray);
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

        private void CheckPixels(float[] array)
        {
            int num = -1;
            using (var frames = pipe.WaitForFrames())
            using (var depth = frames.DepthFrame)
            {
                for (int i = 0; i < width / 10; i++)
                {
                    for (int j = 0; j < height / 10; j++)
                    {
                        num++;
                        array[num] = depth.GetDistance(i*10, j*10);
                    }
                }
                depth.Dispose();
                frames.Dispose();
            }
        }

        private void ComparePixels()
        {
            int count = 0;
            for (int i = 0; i < callibrationArray.Length; i++)
            {
                if (distArray[i] + 0.05 < callibrationArray[i] )
                {
                    count++;
                }
            }
            tbDotCount.Text = count.ToString() + " / " + callibrationArray.Length;
        }
    }
}