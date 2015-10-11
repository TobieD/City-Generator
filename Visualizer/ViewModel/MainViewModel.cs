using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
        #endregion

        //Bindings
        #region Bindings    
            
        /// <summary>
        /// Seed for random Generation of points
        /// </summary>
        public  bool UseSeed { get; set; }
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
        private int _pointsToGenerate = 150;
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
        private int _pointSize = 5;
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
        public int Width { get; set; } = 1500;
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
            _points = VoronoiCreator.GenerateRandomPoints(amount, _boundsStartPoint, Width, Height,Seed);

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
            var timer = Stopwatch.StartNew();

            //Create Voronoi
            _voronoiDiagram = VoronoiCreator.CreateVoronoi(_points, VoronoiAlgorithm);
            timer.Stop();

            if (_voronoiDiagram == null)
                return;
            _voronoiDiagram.Points = _points;

            GenerationTime = timer.ElapsedMilliseconds / 1000.0;

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

            

            var rng = new Random(DateTime.Now.GetHashCode());
            var c = Color.FromRgb(50, 50, 50);

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
                    {
                        var plist = new List<Point>()
                        {
                            t.Point1,
                            t.Point2,
                            t.Point3
                        };
                        _drawService.DrawPolygon(plist, RandomColor(rng));
                    }

                    if(DrawTriangles == true)
                        _drawService.DrawTriangle(t, c);
                }
            }


            //Voronoi Colors
            if (_voronoiDiagram.VoronoiCells != null && ColorVoronoi == true)
            {
                foreach (var cell in _voronoiDiagram.VoronoiCells)
                    _drawService.DrawPolygon(cell.Points, Colors.GreenYellow);
            }

            //Voronoi
            if (_voronoiDiagram.Lines != null && DrawVoronoi == true)
            {
                foreach (var l in _voronoiDiagram.Lines)
                {
                    _drawService.DrawLine(l, Colors.DarkRed);
                    _drawService.DrawPoint(l.Point1, PointSize, Colors.DarkRed);
                    _drawService.DrawPoint(l.Point2, PointSize, Colors.DarkRed);

                    if (ShowPointInfo == true)
                    {
                        _drawService.DrawText(l.Point1.ToString(), Colors.DarkRed, l.Point1);
                        _drawService.DrawText(l.Point2.ToString(), Colors.DarkRed, l.Point2);
                    }
                }
            }

            //Draw Points
            foreach (var p in _points)
            {
                _drawService.DrawPoint(p, PointSize, Colors.Black);
                if(ShowPointInfo == true)
                    _drawService.DrawText(p.ToString(),Colors.Black,p);
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

        private Color RandomColor(Random rng, bool grayscale = false)
        {
            var c = new Color
            {
                R = (byte) (rng.Next()%255),
                G = (byte) (rng.Next()%255),
                B = (byte) (rng.Next()%255),
                A = 255
            };

            if (grayscale)
                c.R = c.G = c.B;

            return c;
        }
    }
}