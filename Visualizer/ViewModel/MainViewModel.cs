using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Visualizer.Extensions;
using Visualizer.Service;
using Voronoi;

namespace Visualizer.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        //Commands
        #region Commands
        private RelayCommand<int> _generatePointsCommand;
        public RelayCommand<int> GeneratePointsCommand => _generatePointsCommand ?? (_generatePointsCommand = new RelayCommand<int>(GeneratePoints));

        private RelayCommand _connectPointsCommand;
        public RelayCommand ConnectPointsCommand => _connectPointsCommand ?? (_connectPointsCommand = new RelayCommand(GenerateVoronoi));

        private RelayCommand _clearCommand;

        public RelayCommand ClearCommand => _clearCommand ?? (_clearCommand = new RelayCommand(() =>
        {
            _drawService.ClearCanvas();
            Initialize(_drawService);
        }));

        private RelayCommand _exportPngCommand;
        public RelayCommand ExportPngCommand => _exportPngCommand ?? (_exportPngCommand = new RelayCommand(() => Export(new CanvasToPngService())));

        #endregion

        //Bindings
        #region Bindings    

        /// <summary>
        /// Seed for random Generation of points
        /// </summary>
        public bool UseSeed { get; set; }
        private int _seed = 0;
        public int Seed
        {
            get { return _seed; }
            set
            {
                _seed = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Amount of points to generate
        /// </summary>
        private int _pointsToGenerate = 200;
        public int PointsToGenerate
        {
            get { return _pointsToGenerate; }
            set
            {
                _pointsToGenerate = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Size the point
        /// </summary>
        private int _pointSize = 2;
        public int PointSize
        {
            get { return _pointSize; }
            set
            {
                _pointSize = value;
                RaisePropertyChanged();
                RefreshCanvas();
            }
        }
        public int MinPointSize { get; set; } = 1;
        public int MaxPointSize { get; set; } = 15;

        /// <summary>
        /// Settings of the bounds in which the points will be spawned
        /// </summary>
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 950;

        /// <summary>
        /// Draw Settings
        /// </summary>
        public bool? ShowBounds { get; set; } = false;
        public bool? ShowCircumCircles { get; set; } = false;
        public bool? ShowMiddlePoints { get; set; } = false;
        public bool? DrawTriangles { get; set; } = false;
        public bool ColorTriangles { get; set; } = false;
        public bool? DrawVoronoi { get; set; } = true;
        public bool? ColorVoronoi { get; set; } = true;
        public bool? ShowPointInfo { get; set; } = false;

        /// <summary>
        /// algorithm used to generate Delaunay Triangulation
        /// </summary>
        public VoronoiAlgorithm VoronoiAlgorithm { get; set; }
        public IEnumerable<VoronoiAlgorithm> PossibleAlgorithms => Enum.GetValues(typeof (VoronoiAlgorithm)).Cast<VoronoiAlgorithm>();

        /// <summary>
        /// Generation Info
        /// </summary>
        public string GenerationTimeText => $"For {PointsToGenerate} points using {VoronoiAlgorithm.ToString()}: {GenerationTime} seconds.";
        private double _generationTime = 0.0;
        public double GenerationTime
        {
            get { return _generationTime; }
            set
            {
                _generationTime = value;
                RaisePropertyChanged("GenerationTimeText");
            }
        }
        #endregion

        //Fields
        #region Fields
        /// <summary>
        /// Point from where to start drawing the rectangle that visualizes the bounds
        /// </summary>
        private readonly Point _boundsStartPoint = new Point(50, 50);
        
        /// <summary>
        /// Handles drawing to the screen
        /// </summary>
        private DrawService _drawService;

        /// <summary>
        /// Store all items to draw
        /// </summary>
        private List<Point> _points;
        private VoronoiDiagram _voronoiDiagram;

        #endregion

        /// <summary>
        /// Initialize this viewModel
        /// </summary>
        public void Initialize(DrawService drawService)
        {
            //initialize draw service
            _drawService = drawService;
            _drawService.OnClick += OnClick;

            //create empty voronoi Diagram
            _points = new List<Point>();
            _voronoiDiagram = new VoronoiDiagram();

            //seed for random generation
            Seed = DateTime.Now.GetHashCode();

            #if DEBUG
            //Seed = 0;
            #endif

        }

        /// <summary>
        /// Generate points and store them into a list
        /// </summary>
        private void GeneratePoints(int amount)
        {
            //clear previous data
            _points.Clear();
            _voronoiDiagram.Clear();

            //user seed or random
            var seed = UseSeed ? Seed : DateTime.Now.GetHashCode();

            //Generate Seed
            _points = VoronoiGenerator.GenerateRandomPoints((amount), _boundsStartPoint, Width, Height,Seed);

            //generate additional points on the center
            //_points.AddRange(VoronoiGenerator.GenerateRandomPoints((amount/2) - 4, new Point(_boundsStartPoint.X + Width/4, _boundsStartPoint.Y + Height/4), Width/2, Height/2, Seed));

            //update randomly generated seed
            if (UseSeed == false)
                Seed = seed;

            //Add Bounds as points
            _points.Add(_boundsStartPoint);
            _points.Add(new Point(_boundsStartPoint.X + Width, _boundsStartPoint.Y));
            _points.Add(new Point(_boundsStartPoint.X, _boundsStartPoint.Y + Height));
            _points.Add(new Point(_boundsStartPoint.X + Width, _boundsStartPoint.Y + Height));
        
            RefreshCanvas();
        }

        /// <summary>
        /// Generate a Voronoi Diagram from the generated points
        /// </summary>
        private void GenerateVoronoi()
        {
            //check if there are enough points
            if (_points == null || _points.Count <= 2 || _voronoiDiagram.Triangulation == null)
            {
                Console.WriteLine("No points available!");
                return;
            }

            //create voronoi using specified algorithm
            var timer = Stopwatch.StartNew();
            _voronoiDiagram = VoronoiGenerator.CreateVoronoi(_points, VoronoiAlgorithm);
            timer.Stop();

            GenerationTime = timer.ElapsedMilliseconds / 1000.0;

            if (_voronoiDiagram == null)
                return;

            _voronoiDiagram.Sites = _points;
         
            //Update canvas
            RefreshCanvas();
        }

        /// <summary>
        /// Redraw all the elements
        /// </summary>
        private void RefreshCanvas()
        { 
            //clear canvas
            _drawService.ClearCanvas();

            var pointColor = Color.FromRgb(25, 25, 25);
            var triangleColor = Color.FromRgb(50, 50, 50);
            var lineColor = Colors.DarkRed;

            //Show Bounds
            if (ShowBounds == true)
                _drawService.DrawRectangle(_boundsStartPoint, Width, Height, Colors.Red);
            
            //Draw Triangulation
            if (_voronoiDiagram.Triangulation != null )
            {
                foreach (var t in _voronoiDiagram.Triangulation)
                {
                    //Fill triangles in a random color

                    if (ColorTriangles)
                        _drawService.DrawPolygon(t, Helpers.RandomColor());

                    if(DrawTriangles == true)
                        _drawService.DrawTriangle(t, triangleColor);
                }
            }


            //Voronoi Colors
            if (_voronoiDiagram.VoronoiCells != null && ColorVoronoi == true)
            {
                foreach (var cell in _voronoiDiagram.VoronoiCells)
                    _drawService.DrawPolygon(cell.Points, Helpers.RandomColor());
            }

            
            //Voronoi
            if (_voronoiDiagram.HalfEdges != null && DrawVoronoi == true)
            {
                foreach (var l in _voronoiDiagram.HalfEdges)
                {
                    _drawService.DrawLine(l, lineColor);

                    if (ShowPointInfo == true)
                    {
                        _drawService.DrawText(l.Point1.ToString(), lineColor, l.Point1);
                        _drawService.DrawText(l.Point2.ToString(), lineColor, l.Point2);
                    }
                }
            }

            //Draw Points
            foreach (var p in _points)
            {
                _drawService.DrawPoint(p, PointSize, pointColor);
                if(ShowPointInfo == true)
                    _drawService.DrawText(p.ToString(), pointColor, p);
            }

            //Voronoi Cell Points
            foreach (var voronoiCellPoint in _voronoiDiagram.VoronoiCellPoints)
            {
                _drawService.DrawPoint(voronoiCellPoint, PointSize, Colors.OrangeRed);
            }

        }

        /// <summary>
        /// Add a point on the clicked position of the canvas
        /// </summary>
        /// <param name="point"></param>
        private void OnClick(Point point)
        {
            _points.Add(point);
            //retriangulate with the new point added
            GenerateVoronoi();

        }
        
       /// <summary>
       /// Export the currently draw canvas to a file
       /// </summary>
        private void Export(ICanvasExporter exporter)
        {
            if (exporter == null)
                return;

           //select save location
           var saveDlg = new SaveFileDialog()
           {
               FileName = "",
           };

           if (saveDlg.ShowDialog() == true)
                exporter.Export(saveDlg.FileName,_drawService.Canvas);

        }
    }
}