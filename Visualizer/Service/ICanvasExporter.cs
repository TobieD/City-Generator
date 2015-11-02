using System.Windows.Controls;

namespace Visualizer.Service
{
    public interface ICanvasExporter
    {
        void Export(string filename, Canvas canvas,int width, int height);
    }
}
