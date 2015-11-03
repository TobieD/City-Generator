using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using CityGenerator;
using CityGeneratorWPF.Extensions;
using CityGeneratorWPF.Service;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Helpers;
using Microsoft.Win32;
using Voronoi;

namespace CityGeneratorWPF.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        //Commands
        #region Commands

        private RelayCommand<int> _generatePointsCommand;

        public RelayCommand<int> GeneratePointsCommand
            => _generatePointsCommand ?? (_generatePointsCommand = new RelayCommand<int>(GeneratePoints));

        private RelayCommand _connectPointsCommand;

        public RelayCommand ConnectPointsCommand
            => _connectPointsCommand ?? (_connectPointsCommand = new RelayCommand(GenerateVoronoi));

        private RelayCommand _clearCommand;

        public RelayCommand ClearCommand => _clearCommand ?? (_clearCommand = new RelayCommand(() =>
        {
            _drawService.ClearCanvas();
            Initialize(_drawService);
        }));

        private RelayCommand _exportPngCommand;

        public RelayCommand ExportPngCommand
            => _exportPngCommand ?? (_exportPngCommand = new RelayCommand(() => Export(new CanvasToPngService())));

       private RelayCommand _generateCityCommand;

        public RelayCommand GenerateCityCommand
            => _generateCityCommand ?? (_generateCityCommand = new RelayCommand(GenerateCity));

        private RelayCommand _generateRoadsCommand;

        public RelayCommand GenerateRoadsCommand
            => _generateRoadsCommand ?? (_generateRoadsCommand = new RelayCommand(GenerateRoads));

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

        public bool CityLike { get; set; } = false;

        /// <summary>
        /// Amount of points to generate
        /// </summary>
        private int _pointsToGenerate = 500;

        public int PointsToGenerate
        {
            get { return _pointsToGenerate; }
            set
            {
                _pointsToGenerate = value;
                RaisePropertyChanged();
            }
        }

        public int Offset { get; set; } = 4;

        /// <summary>
        /// Size the point
        /// </summary>
        private int _pointSize = 3;

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
        public int Width { get; set; } = 1700;

        public int Height { get; set; } = 1000;

        public int StartX { get; set; } = 25;

        public int StartY { get; set; } = 25;

        /// <summary>
        /// Draw Settings
        /// </summary>
        public bool? ShowBounds { get; set; } = false;

        public bool? ShowCircumCircles { get; set; } = false;
        public bool? ShowMiddlePoints { get; set; } = false;
        public bool? DrawTriangles { get; set; } = false;
        public bool ColorTriangles { get; set; } = false;
        public bool? DrawVoronoi { get; set; } = true;
        public bool? ColorVoronoi { get; set; } = false;
        public bool? ShowPointInfo { get; set; } = false;

        /// <summary>
        /// algorithm used to generate Delaunay Triangulation
        /// </summary>
        public VoronoiAlgorithm VoronoiAlgorithm { get; set; } = VoronoiAlgorithm.Fortune;

        public IEnumerable<VoronoiAlgorithm> PossibleAlgorithms
            => Enum.GetValues(typeof (VoronoiAlgorithm)).Cast<VoronoiAlgorithm>();

        /// <summary>
        /// Generation Info
        /// </summary>
        private string _generationText;

        public string GenerationTimeText
        {
            get { return _generationText; }
            set
            {
                _generationText = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        //Fields
        #region Fields

        /// <summary>
        /// Point from where to start drawing the rectangle that visualizes the bounds
        /// </summary>
        private readonly Point _boundsStartPoint = new Point(5, 5);

        /// <summary>
        /// Handles drawing to the screen
        /// </summary>
        private DrawService _drawService;

        /// <summary>
        /// Store all items to draw
        /// </summary>
        private List<Point> _points;

        private VoronoiDiagram _voronoiDiagram;
        private CityData _cityData;


        private Color _baseColor = Colors.OrangeRed;

        #endregion

        //Debug Bindings
        private int _maxRoadProgress = 0;

        public int MaxRoadProgress
        {
            get { return _maxRoadProgress; }
            set
            {
                _maxRoadProgress = value;
                RaisePropertyChanged();
            }
        }

        private int _roadProgress = 0;

        public int RoadProgress
        {
            get { return _roadProgress; }
            set
            {
                _roadProgress = value;
                RaisePropertyChanged();
                RefreshCanvas();
            }
        }

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
            _cityData = new CityData();

            //seed for random generation
            Seed = DateTime.Now.GetHashCode();

            #if DEBUG
            Seed = 0;
            UseSeed = true;
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
            _cityData.Clear();

            //user seed or random
            var seed = UseSeed ? Seed : DateTime.Now.GetHashCode();
            var actualAmount = amount;
            var timer = Stopwatch.StartNew();

            _boundsStartPoint.X = StartX;
            _boundsStartPoint.X = StartY;

            //Generate Seed
            _points = VoronoiGenerator.GenerateRandomPoints(actualAmount, _boundsStartPoint, Width, Height, Seed);

            //generate additional points on the center
            if (CityLike)
            {
                int offset = Offset;

                var startPoint = new Point(_boundsStartPoint.X + (int) (Width/(offset*2)),
                    _boundsStartPoint.Y + (int) (Height/(offset*2)));
                var newWidth = Width - (Width/offset);
                var newHeight = Height - (Height/offset);

                _points.AddRange(VoronoiGenerator.GenerateRandomPoints(actualAmount, startPoint, newWidth, newHeight,
                    Seed));
            }

            //Add Bounds as points
            _points.Add(_boundsStartPoint);
            _points.Add(new Point(_boundsStartPoint.X + Width, _boundsStartPoint.Y));
            _points.Add(new Point(_boundsStartPoint.X, _boundsStartPoint.Y + Height));
            _points.Add(new Point(_boundsStartPoint.X + Width, _boundsStartPoint.Y + Height));

            timer.Stop();
            var time = timer.ElapsedMilliseconds/1000.0;
            GenerationTimeText = $"Generated {PointsToGenerate} points in {time} seconds.";

            //update randomly generated seed
            if (UseSeed == false)
                Seed = seed;


            _points.FilterDoubleValues();

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

            _baseColor = Extensions.Extensions.RandomColor();

            //create voronoi using specified algorithm
            var timer = Stopwatch.StartNew();
            _voronoiDiagram = VoronoiGenerator.CreateVoronoi(_points, VoronoiAlgorithm);
            timer.Stop();

            //set bounds
            _voronoiDiagram.Bounds = new Point(Width, Height);

            if (_voronoiDiagram == null)
                return;
            _voronoiDiagram.Sites = _points;

            //update info
            var time = timer.ElapsedMilliseconds/1000.0;
            GenerationTimeText = $"For {PointsToGenerate} points using {VoronoiAlgorithm.ToString()}: {time} seconds.";

            //Update canvas
            RefreshCanvas();
        }

        /// <summary>
        /// Convert the Voronoi Diagram into a city with roads and building zones
        /// </summary>
        private void GenerateCity()
        {
            //start timer for speed check
            var timer = Stopwatch.StartNew();

            _cityData = CityBuilder.GenerateCity(_voronoiDiagram);

            timer.Stop();

            //update timer
            var time = timer.ElapsedMilliseconds/1000.0;
            GenerationTimeText = $"City generated in {time} seconds.";

            //update canvas
            RefreshCanvas();

        }

        private void GenerateRoads()
        {
            
            if(_cityData == null)
                _cityData = new CityData();
            
            //start timer for speed check
            var timer = Stopwatch.StartNew();



            _cityData.MainRoad = CityBuilder.GenerateMainRoad(_voronoiDiagram);

            timer.Stop();
            var time = timer.ElapsedMilliseconds / 1000.0;
            GenerationTimeText = $"City generated in {time} seconds.";

            MaxRoadProgress = _cityData.MainRoad.RoadLines.Count;
            RoadProgress = MaxRoadProgress;
        }

        /// <summary>
        /// Redraw all the elements
        /// </summary>
        private void RefreshCanvas()
        {
            //clear canvas
            _drawService.ClearCanvas();

            //Show Bounds
            if (ShowBounds == true)
                _drawService.DrawRectangle(_boundsStartPoint, Width, Height, Colors.Red);

            DrawVoronoiDiagram();
            DrawCity();
        }

        private void DrawVoronoiDiagram()
        {
            var pointColor = Color.FromRgb(25, 25, 25);
            var triangleColor = Color.FromRgb(50, 50, 50);
            var lineColor = Color.FromRgb(22, 22, 22);


            if (_voronoiDiagram != null)
            {
                //Draw Triangulation
                if (_voronoiDiagram.Triangulation != null)
                {
                    foreach (var t in _voronoiDiagram.Triangulation)
                    {
                        //Fill triangles in a random color

                        if (ColorTriangles)
                            _drawService.DrawPolygon(t, Extensions.Extensions.RandomColor());

                        if (DrawTriangles == true)
                            _drawService.DrawTriangle(t, triangleColor);
                    }
                }


                //Voronoi Colors
                if (_voronoiDiagram.VoronoiCells != null && ColorVoronoi == true)
                {
                    foreach (var cell in _voronoiDiagram.VoronoiCells)
                        _drawService.DrawPolygon(cell.Points, _baseColor.GetRandomColorOffset(0.2));
                }


                //Voronoi
                if (_voronoiDiagram.HalfEdges != null && DrawVoronoi == true)
                {
                    foreach (var l in _voronoiDiagram.HalfEdges)
                    {
                        if (DrawVoronoi == true)
                        {

                            var c = DrawTriangles == true ? Colors.Red : lineColor;
                            _drawService.DrawLine(l, c);
                        }

                        if (ShowPointInfo == true)
                        {
                            _drawService.DrawText(l.Point1.ToString(), lineColor, l.Point1);
                            _drawService.DrawText(l.Point2.ToString(), lineColor, l.Point2);
                        }
                    }
                }
            }

            //Draw Points
            foreach (var p in _points)
            {
                _drawService.DrawPoint(p, PointSize, pointColor);
                if (ShowPointInfo == true)
                    _drawService.DrawText(p.ToString(), pointColor, p);
            }
        }

        private void DrawCity()
        {
            if (_cityData == null)
                return;


            if (_cityData.Zones.Count > 0)
            {

                Color[] colors = new[]
                {
                    Colors.SaddleBrown,
                    Colors.Gray,
                    Colors.DarkGreen,
                    Colors.DodgerBlue
                };

                foreach (var zone in _cityData.Zones)
                {

                    Color c = colors[(int)zone.Type];
                    c = c.GetRandomColorOffset(0.04f);
                    _drawService.DrawCell(zone.ZoneBounds, c, true);
                }
            }

            if (_cityData.MainRoad.RoadLines.Count > 0)
            {
                foreach (var roadLine in _cityData.MainRoad.RoadLines)
                {
                    var c = Colors.ForestGreen;
                    _drawService.DrawLine(roadLine, c, 5);
                }

            }
            //Draw start and endpoint of main road
            _drawService.DrawPoint(_cityData.MainRoad.StartPoint, 10, Colors.OrangeRed);
            _drawService.DrawPoint(_cityData.MainRoad.EndPoint, 10, Colors.DodgerBlue);



            if (_cityData.RoadBranches.Count > 0)
            {

                foreach (var roadBranch in _cityData.RoadBranches)
                {
                    var c = Colors.DarkRed;
                    foreach (var roadLine in roadBranch.RoadLines)
                    {
                        _drawService.DrawLine(roadLine, c, 3);

                    }

                    _drawService.DrawPoint(roadBranch.StartPoint, 5, Colors.YellowGreen);
                    _drawService.DrawPoint(roadBranch.EndPoint, 5, Colors.DeepPink);

                }
            }
        }

        /// <summary>
        /// Add a point on the clicked position of the canvas
        /// </summary>
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
                Filter = "png Image|*.png",
                Title = "Save the diagram to an Image File"
            };

            if (saveDlg.ShowDialog() == true)
                exporter.Export(saveDlg.FileName, _drawService.Canvas,Width,Height);

        }
    }
}