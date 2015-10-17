using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Visualizer.Service
{
    public class CanvasToPngService:ICanvasExporter

    {
        /// <summary>
        /// export canvas to .png
        /// http://stackoverflow.com/questions/21411878/saving-a-canvas-to-png-c-sharp-wpf
        /// </summary>
        public void Export(string filename,Canvas canvas)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(canvas);
            var dpi = 96;

            var rtb = new RenderTargetBitmap((int)bounds.Width,(int)bounds.Height,dpi,dpi,PixelFormats.Default);

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                var vb = new VisualBrush(canvas);
                context.DrawRectangle(vb,null,new Rect(new Point(),bounds.Size));
            }

            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            //add png extension
            filename += ".png";

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);

                File.WriteAllBytes(filename,ms.ToArray());
            }

        }
    }
}
