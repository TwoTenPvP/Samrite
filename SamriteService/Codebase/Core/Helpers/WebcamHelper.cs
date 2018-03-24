using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SamriteService.Codebase.Core.Helpers
{
    internal static class WebcamHelper
    {
        internal static void GetWebcamImage(int deviceIndex, int width, int height, Action<Bitmap> callback)
        {
            FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (deviceIndex >= devices.Count)
            {
                callback(new Bitmap(width, height)); //Out of bounds
                return;
            }

            VideoCaptureDevice device = new VideoCaptureDevice(devices[deviceIndex].MonikerString);
            device.NewFrame += new AForge.Video.NewFrameEventHandler((sender, e) => 
            {
                device.SignalToStop();
                callback(e.Frame);
            });
            device.Start();
        }

        internal static byte[] GetImagePNGBytes(Bitmap source)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                source.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        internal static string[] GetDeviceNames()
        {
            FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            string[] names = new string[devices.Count];
            for (int i = 0; i < devices.Count; i++)
            {
                names[i] = devices[i].Name;
            }
            return names;
        }

        internal static VideoCapabilities[] GetDeviceCapabilities(int deviceIndex)
        {
            FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (deviceIndex >= devices.Count)
                return new VideoCapabilities[0];

            if (new VideoCaptureDevice(devices[deviceIndex].MonikerString).VideoCapabilities == null)
                return new VideoCapabilities[0];

            return new VideoCaptureDevice(devices[deviceIndex].MonikerString).VideoCapabilities;
        }

        internal static Bitmap GetDesktopScreenshot()
        {
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }
    }
}
