using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Visualizer.Extensions;

namespace Visualizer.Service
{
    public class CanvasToPngService:ICanvasExporter

    {
        /// <summary>
        /// export canvas to .png
        /// http://stackoverflow.com/questions/21411878/saving-a-canvas-to-png-c-sharp-wpf
        /// </summary>
        public void Export(string filename,Canvas canvas,int width, int height)
        {
            var dpi = 96;
            width = (int) canvas.RenderSize.Width;
            height = (int)canvas.RenderSize.Height;
            Console.WriteLine("Exporting image of {0}x{1}",width, height);

            var rtb = new RenderTargetBitmap(width, height, dpi,dpi,PixelFormats.Default);

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                var vb = new VisualBrush(canvas);
                context.DrawRectangle(vb,null,new Rect(new Point(),new Size(width,height)));
            }

            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);

                File.WriteAllBytes(filename,ms.ToArray());
            }

        }
    }
}
