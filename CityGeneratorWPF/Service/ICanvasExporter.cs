using System.Windows.Controls;

namespace CityGeneratorWPF.Service
{
    public interface ICanvasExporter
    {
        void Export(string filename, Canvas canvas,int width, int height);
    }
}
