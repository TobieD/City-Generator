using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Points;
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


        private RelayCommand _redrawCityCommand;
        public RelayCommand RedrawCityCommand
            => _redrawCityCommand ?? (_redrawCityCommand = new RelayCommand(RefreshCanvas));

        private RelayCommand<string> _AddSettingsCommand;

        public RelayCommand<string> AddSettingsCommand
            => _AddSettingsCommand ?? (_AddSettingsCommand = new RelayCommand<string>(AddNewCityDistrictType));

        private RelayCommand<string> _removeSettingsCommand;

        public RelayCommand<string> RemoveSettingsCommand
            => _removeSettingsCommand ?? (_removeSettingsCommand = new RelayCommand<string>(RemoveCityDistrictType));

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

        private int _radius = 350;

        public int Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                RaisePropertyChanged();
            }
        }

        public int Offset { get; set; } = 4;

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
        public int Width { get; set; } = 1500;

        public int Height { get; set; } = 800;

        public int StartX { get; set; } = 100;

        public int StartY { get; set; } = 100;

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
        public bool? DrawPoints { get; set; } = true;

        public bool? DrawDistricts { get; set; } = false;
        public bool? DrawRoads { get; set; } = true;
        

        /// <summary>
        /// algorithm used to generate Delaunay Triangulation
        /// </summary>
        public VoronoiAlgorithm VoronoiAlgorithm { get; set; } = VoronoiAlgorithm.Fortune;

        public IEnumerable<VoronoiAlgorithm> PossibleAlgorithms
            => Enum.GetValues(typeof (VoronoiAlgorithm)).Cast<VoronoiAlgorithm>();


        public PointGenerationAlgorithm PointAlgorithm { get; set; } = PointGenerationAlgorithm.Simple;

        public IEnumerable<PointGenerationAlgorithm> PossiblePointAlgorithms
            => Enum.GetValues(typeof(PointGenerationAlgorithm)).Cast<PointGenerationAlgorithm>();

        public string NewType { get; set; } = "newType";


        public bool? DebugMode { get; set; } = true;

        private bool _bGenerateInnerRoads = false;
        public bool GenerateInnerRoads
        {
            get { return _bGenerateInnerRoads; }
            set
            {
                _bGenerateInnerRoads = value;
                RaisePropertyChanged();
            }
        }

        private int _roadSubdivisions = 1;

        public int RoadSubdivisions
        {
            get { return _roadSubdivisions; }
            set
            {
                _roadSubdivisions = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<DistrictSettings> _districtSettings = new ObservableCollection<DistrictSettings>();
        public ObservableCollection<DistrictSettings> DistrictSettings
        {
            get { return _districtSettings; }
            set
            {
                _districtSettings = value;
                RaisePropertyChanged();
            }
        }

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

        private GenerationSettings _voronoiSettings;
        private CitySettings _citySettings;

        private Color _baseColor = Colors.OrangeRed;

        //district settings
        private List<Color> _districtColors = new List<Color>()
        {
            Colors.ForestGreen,
            Colors.SaddleBrown,
            Color.FromRgb(235,175,40),
            Colors.DarkSlateGray,
            Colors.BurlyWood,
            Colors.DeepSkyBlue
        };
        private List<string> _districtTypes = new List<string>()
        {
            "Grass", //simple empty space
            "Urban", //tight appartments + shops
            "Financial", //big buildings
            "Factory", //industrial buildings
            "Suburbs", //big houses and lotsof green
            "Water"

        };

        #endregion

        /// <summary>
        /// Initialize this viewModel
        /// </summary>
        public void Initialize(DrawService drawService)
        {
            //initialize draw service
            _drawService = drawService;
            //_drawService.OnClick += OnClick;

            //create empty voronoi Diagram
            _points = new List<Point>();
            _voronoiDiagram = new VoronoiDiagram();
            _cityData = new CityData();

            _citySettings = new CitySettings();

            //seed for random generation
            Seed = DateTime.Now.GetHashCode();

            //store default settings
            foreach (var districtType in _districtTypes)
            {
                DistrictSettings.Add(new DistrictSettings(districtType));
                break;
            }

            RaisePropertyChanged("DistrictSettings");

            //debug for math test and drawing
           // MathTesting();
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

            //fill settings
            _voronoiSettings = new GenerationSettings
            {
                VoronoiAlgorithm = VoronoiAlgorithm,
                PointAlgorithm = PointAlgorithm,
                Width = Width,
                Length = Height,
                StartX = StartX,
                StartY = StartY,
                UseSeed = UseSeed,
                Seed = Seed,
                Amount = PointsToGenerate,
                CircleRadius = Radius
               
            };

            //generate points
            var timer = Stopwatch.StartNew();
            _points = PointGenerator.Generate(_voronoiSettings);
            timer.Stop();
            
            //update generation timer
            var time = timer.ElapsedMilliseconds/1000.0;
            GenerationTimeText = $"Generated {_points.Count} points in {time} seconds.";

            //update randomly generated seed
            if (UseSeed == false)
                Seed = _voronoiSettings.Seed;

            //update canvas
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
                GeneratePoints(PointsToGenerate);
                //return;
            }

            _baseColor = Extensions.Extensions.RandomColor();

            _voronoiSettings.VoronoiAlgorithm = VoronoiAlgorithm;

            //create voronoi using specified algorithm
            var timer = Stopwatch.StartNew();
            _voronoiDiagram = VoronoiGenerator.CreateVoronoi(_points, _voronoiSettings);
            timer.Stop();

            if (_voronoiDiagram == null)
                return;

            //update info
            var time = timer.ElapsedMilliseconds/1000.0;
            GenerationTimeText = $"For {_points.Count} points using {VoronoiAlgorithm.ToString()}: {time} seconds.";

            //Update canvas
            RefreshCanvas();
        }

        /// <summary>
        /// Convert the Voronoi Diagram into a city with roads and building zones
        /// </summary>
        private void GenerateCity()
        {
            if (_voronoiDiagram == null)
            {
                //GenerateVoronoi();
                return;
            }

            //Settings for generation
            _citySettings.DistrictSettings = DistrictSettings.ToList();
            _citySettings.GenerateInnerRoads = GenerateInnerRoads;
            _citySettings.RoadSubdivision = RoadSubdivisions;
            _citySettings.DebugMode = DebugMode.Value;

            //generate city
            var timer = Stopwatch.StartNew();
            _cityData = CityBuilder.GenerateCity(_citySettings,_voronoiDiagram);
            timer.Stop();

            //update timer
            var time = timer.ElapsedMilliseconds/1000.0;
            GenerationTimeText = $"City generated in {time} seconds.";

            //update canvas
            RefreshCanvas();

        }

        /// <summary>
        /// Redraw all the elements
        /// </summary>
        private void RefreshCanvas()
        {
            //clear canvas
            _drawService.ClearCanvas();

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

                //Show Bounds
                if (ShowBounds == true)
                    _drawService.DrawRectangle(_voronoiDiagram.Bounds, Colors.Red);

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
                if (_voronoiDiagram.VoronoiCells != null && ColorVoronoi.Value == true || DrawVoronoi.Value == true)
                {
                    foreach (var cell in _voronoiDiagram.GetCellsInBounds())
                    {
                        var c = (ColorVoronoi == true) ? _baseColor.GetRandomColorOffset(0.2) : lineColor;
                        _drawService.DrawCell(cell,c , ColorVoronoi.Value);
                    }
                }
            }

            //Draw Points
            if (DrawPoints == true)
            {
                foreach (var p in _points)
                {
                    _drawService.DrawPoint(p, PointSize, pointColor);
                    if (ShowPointInfo == true)
                        _drawService.DrawText(p.ToString(), pointColor, p);
                }
            }
        }

        private void DrawCity()
        {
            if (_cityData == null)
                return;

            int i = 0;

            if (_cityData.Bounds != null)
            {
                //_drawService.DrawRectangle(_cityData.Bounds, Colors.Red);
            }

            //Draw districts
            foreach (var district in _cityData.Districts)
            {
                var c = _districtColors[i];
                _drawService.DrawDistrict(district, c,DrawRoads.Value, DrawDistricts.Value);
                i++;
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


        private void AddNewCityDistrictType(string settings)
        {

            foreach (var dis in DistrictSettings)
            {
                if(dis.Type == settings)
                    return;
            }

            _districtColors.Add(Extensions.Extensions.RandomColor());

            DistrictSettings.Add(new DistrictSettings(settings));
            //RaisePropertyChanged("DistrictSettings");
            
        }

        private void RemoveCityDistrictType(string settings)
        {
            DistrictSettings found = null;
            int i = 0;
            foreach (var dis in DistrictSettings)
            {
                if (dis.Type == settings)
                    found = dis;

                i++;
            }

            if (found == null)
                return;

            _districtColors.RemoveAt(i);

            DistrictSettings.Remove(found);
            //RaisePropertyChanged("DistrictSettings");

        }


        private void MathTesting()
        {

            //Setup

            var s = new Point(200,700);
            var e = new Point(500, 350);
            var l = new Line(s,e);

            var fp = new Point(50,550);

            _drawService.DrawLine(l,Colors.Black,3);
            _drawService.DrawPoint(e, 7, Colors.Black);
            _drawService.DrawPoint(s,7,Colors.Black);
            _drawService.DrawPoint(fp, 7, Colors.Black);

            var offset = l.GenerateOffsetParallelTowardsPoint(20, fp);
//            _drawService.DrawLine(offset, Colors.Black, 1);

            double minDistance = 2;
            var width = 10;
            double totalDistance = offset.Length();
            double currentDistance = minDistance + width; //Start with an offset
            
            while(currentDistance < totalDistance - (minDistance + width))
            {

                var percentage = currentDistance/totalDistance;

                var ps = offset.FindRandomPointOnLine(percentage,percentage);
                _drawService.DrawPoint(ps, 7, Colors.Black);

                //In unity the width should be the width of the prefab
                width = RandomHelper.RandomInt(10, 50);

                currentDistance += (minDistance + width);
            }

        }


    }
}