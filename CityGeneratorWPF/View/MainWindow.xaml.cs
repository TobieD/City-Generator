using System.Windows;
using CityGeneratorWPF.Service;
using Visualizer.ViewModel;

namespace Visualizer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //not normally done , but it is the easiest way to access the drawing Canvas
            var locator = (ViewModelLocator)DataContext;
            locator.Main.Initialize(new DrawService(cDrawCanvas));
        }
    }
}
