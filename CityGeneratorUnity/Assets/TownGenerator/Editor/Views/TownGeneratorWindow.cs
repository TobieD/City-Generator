using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Points;
using UnityEngine;
using UnityEditor;

using Voronoi;
using CityGenerator;


namespace Tools.TownGenerator
{


    /// Export as file specifier
    /// </summary>
    public enum ExportSettingType
    {
        //JSON,	
        XML
    }

    /// <summary>
    /// Preset size and point amounts
    /// </summary>
    public enum CityType
    {
        Village,
        Town,
        Metropolis
    }

    /// <summary>
    /// UI wrapper around the TownGenerator
    /// </summary>
    public class TownGeneratorWindow : EditorWindow
    {
        //Generator
        private TownGenerator _townGenerator;

        //Settings
        private GenerationSettings _generationSettings;
        private CitySettings _citySettings;

        private CityType _cityType = CityType.Village;
        private bool _showAdvancedSettings = false;

        //terrain editor
        private TerrainEditor _terrainEditor;

        //District Editor for every district
        private Dictionary<string, DistrictEditor> _prefabSelectors = new Dictionary<string, DistrictEditor>();
        private string _zoneInput = "";

        //Foldouts
        private bool _foldoutSettings = true;
        private bool _foldoutZoneEdit = false;
        private GUIStyle _foldoutStyle;

        Vector2 _scrollPosition;

        //Saving
        private ExportSettingType _exportSetting = ExportSettingType.XML;
        private string _saveFile = "Settings";

        //Add menu to the window menu
        [MenuItem("Tools/Town Generator")]
        private static void Init()
        {
            //get exisiting open window or create one if none is available
            var window =
                (TownGeneratorWindow)EditorWindow.GetWindow(typeof(TownGeneratorWindow), false, "Town Generator");
            window.maxSize = new Vector2(400, 1000);
            window.minSize = window.maxSize;

            window.Initialize();
            window.Show();
        }

        /// <summary>
        /// Initialize base settings
        /// </summary>
        public void Initialize()
        {
            //create a town generator object when it doesn't exist yet
            if (_townGenerator != null) return;


            //Access towngenerator
            _townGenerator = TownGenerator.GetInstance();

            //Set default settings
            _generationSettings = new GenerationSettings
            {
                VoronoiAlgorithm = VoronoiAlgorithm.Fortune,
                PointAlgorithm = PointGenerationAlgorithm.CityLike
            };

            //City settings
            _citySettings = new CitySettings();
            _citySettings.DebugMode = false;

            //Create a base district type
            CreatePrefabSelection("Grass");

            //Terrain settings editor
            _terrainEditor = new TerrainEditor(new TerrainSettings(), this);

            //Define a style for the foldout
            _foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
            };

            var c = Color.black;
            _foldoutStyle.onActive.textColor = c;
            _foldoutStyle.normal.textColor = c;
            _foldoutStyle.onNormal.textColor = c;
            _foldoutStyle.hover.textColor = c;
            _foldoutStyle.onHover.textColor = c;
            _foldoutStyle.focused.textColor = c;
            _foldoutStyle.onFocused.textColor = c;
            _foldoutStyle.active.textColor = c;
            _foldoutStyle.onActive.textColor = c;
        }

        /// <summary>
        /// Drawing
        /// </summary>
        void OnGUI()
        {
            //Create TownGenerator if there isn't one
            Initialize();

            //Show import export UI
            ImportExportSettings();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            //Enable/Disable debug mode
            //_citySettings.DebugMode = EditorGUILayout.Toggle("Debug Mode", _citySettings.DebugMode);//Draw Generation settings

            //Terrain settings
            _terrainEditor.DrawGUI(_foldoutStyle);

            //generation settings
            GenerationSettings();

            //District Settings
            CitySettings();

            //buttons
            GenerationButtons();

            EditorGUILayout.EndScrollView();

        }

        #region GUI

        /// <summary>
        /// Save /Load Settings from a file
        /// </summary>
        private void ImportExportSettings()
        {
            GUILayout.Label("Import / Export", EditorStyles.boldLabel);
            //Load/Saving
            GUILayout.BeginHorizontal();
            _saveFile = EditorGUILayout.TextField(_saveFile);
            _exportSetting = (ExportSettingType)EditorGUILayout.EnumPopup(_exportSetting);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Import"))
            {
                LoadSettings();
            }

            if (GUILayout.Button("Export"))
            {
                SaveSettings();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Adjust settings that generate the voronoi diagram
        /// </summary>
        private void GenerationSettings()
        {
            //foldout settings
            _foldoutSettings = EditorGUILayout.Foldout(_foldoutSettings, "Generation Settings", _foldoutStyle);
            if (!_foldoutSettings)
            {
                return;
            }

            EditorGUI.indentLevel++;

            //SEED
            _generationSettings.UseSeed = EditorGUILayout.Toggle("Use Seed", _generationSettings.UseSeed);
            if (_generationSettings.UseSeed)
            {
                _generationSettings.Seed = EditorGUILayout.IntField("Seed", _generationSettings.Seed);
            }

            //PARENT
            //_townGenerator.Parent = (GameObject)EditorGUILayout.ObjectField("Parent", _townGenerator.Parent, typeof(GameObject), true);

            //PRESETS
            _cityType = (CityType)EditorGUILayout.EnumPopup("Preset", _cityType);

            //ADVANCED SETTINGS
            _showAdvancedSettings = EditorGUILayout.Toggle("Advanced Settings", _showAdvancedSettings);
            if (_showAdvancedSettings)
            {
                //Width and Height
                _generationSettings.Width = EditorGUILayout.IntField("Size", (int)_generationSettings.Width);
                _generationSettings.Length = _generationSettings.Width;
                _generationSettings.Amount = EditorGUILayout.IntField("Amount of Points",
                    (int)_generationSettings.Amount);
            }
            EditorGUI.indentLevel--;

        }

        /// <summary>
        /// select prefabs for a specific zone
        /// </summary>
        private void CitySettings()
        {
            //GUILayout.Label("City Settings", EditorStyles.boldLabel);
            _foldoutZoneEdit = EditorGUILayout.Foldout(_foldoutZoneEdit, "City Settings", _foldoutStyle);

            if (!_foldoutZoneEdit)
            {
                return;
            }


            //General Settings
            _citySettings.GenerateInnerRoads = EditorGUILayout.Toggle("Generate inner roads",
                _citySettings.GenerateInnerRoads);

            if (_citySettings.GenerateInnerRoads)
            {
                _citySettings.RoadSubdivision = EditorGUILayout.IntSlider("Road subdivision",
                    _citySettings.RoadSubdivision,
                    1, 2);
            }

            //allow user to add custom zone types
            AddOrRemoveZoneType();

            //Per District Editor
            if (_prefabSelectors.Count > 0)
            {
                //manage selecting of the prefab game objects the user wants to spawn in a specific zone type
                foreach (var prefabSelect in _prefabSelectors.Values)
                {
                    //draw districtEditor
                    prefabSelect.DrawGUI(_foldoutStyle);
                    //GUILayout.Space(10);
                }
            }

        }

        /// <summary>
        /// Add or delete a new district type
        /// </summary>
        private void AddOrRemoveZoneType()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add new district");
            _zoneInput = GUILayout.TextField(_zoneInput);

            //add type
            if (GUILayout.Button("+") && _zoneInput != String.Empty)
            {
                CreatePrefabSelection(_zoneInput);
                _zoneInput = "";
            }

            //Remove type
            if (GUILayout.Button("-") && _zoneInput != String.Empty)
            {
                _prefabSelectors.Remove(_zoneInput);
                _zoneInput = "";
            }


            EditorGUILayout.EndHorizontal();

        }

        /// <summary>
        /// The buttons for generating, building and resetting
        /// </summary>
        private void GenerationButtons()
        {
            GUILayout.BeginHorizontal();

            //Generate a terrain and a voronoi diagram on the terrain
            if (GUILayout.Button("Generate"))
            {
                //Use custom settings or a preset
                _generationSettings = _showAdvancedSettings ? _generationSettings : GetSettingsFromPreset(_cityType);

                //Store district information
                _citySettings.DistrictSettings = MakeDistrictSettings();
                _townGenerator.PrefabsPerZone = MakePrefabList();

                _townGenerator.Generate(_generationSettings, _citySettings, _terrainEditor.GetSettings());

            }

            ////Using the voronoi data, create a city and build it
            //if (GUILayout.Button("Build"))
            //{
            //    _townGenerator.Build();
            //}

            //Clear all generated data
            if (GUILayout.Button("Clear"))
            {
                _townGenerator.Clear();
            }

            GUILayout.EndHorizontal();

        }

        #endregion

        #region Helpers

        /// <summary>
        /// Create a new district zone
        /// </summary>
        private DistrictEditor CreatePrefabSelection(string type)
        {
            if (_prefabSelectors.ContainsKey(type))
            {
                Debug.LogWarningFormat("TownGenerator: Zonetype {0} already exists. \nFailed to add type.", type);
                return null;
            }

            var editor = new DistrictEditor(type, this);

            _prefabSelectors.Add(type, editor);

            return editor;


        }

        /// <summary>
        /// Define preset settings based on a type
        /// </summary>
        private GenerationSettings GetSettingsFromPreset(CityType cityType)
        {
            var genSettings = new GenerationSettings();

            switch (cityType)
            {
                //Small
                case CityType.Village:
                    {
                        genSettings.Width = 150;
                        genSettings.Length = 150;
                        genSettings.PointAlgorithm = PointGenerationAlgorithm.Simple;
                        genSettings.Amount = 20;
                        _citySettings.GenerateInnerRoads = false;
                        break;
                    }

                //medium
                case CityType.Town:
                    {
                        genSettings.Width = 200;
                        genSettings.Length = 200;
                        genSettings.PointAlgorithm = PointGenerationAlgorithm.Simple;
                        genSettings.Amount = 50;
                        _citySettings.GenerateInnerRoads = true;
                        break;
                    }

                //large
                case CityType.Metropolis:
                    {
                        genSettings.Width = 350;
                        genSettings.Length = 350;
                        genSettings.PointAlgorithm = PointGenerationAlgorithm.CityLike;
                        genSettings.Amount = 100;
                        break;
                    }
                default:
                    break;
            }

            //still allow seed to be used with presets
            genSettings.UseSeed = _generationSettings.UseSeed;
            genSettings.Seed = _generationSettings.Seed;

            //prefer fortune algorithm for speed
            genSettings.VoronoiAlgorithm = VoronoiAlgorithm.Fortune;

            return genSettings;
        }

        /// <summary>
        /// returns a look up map that contains all the prefabs per district
        /// </summary>
        private Dictionary<string, List<GameObject>> MakePrefabList()
        {
            return _prefabSelectors.ToDictionary(pfS => pfS.Key, pfS => pfS.Value.GetActualPrefabs());
        }

        private List<DistrictSettings> MakeDistrictSettings()
        {
            var settings = new List<DistrictSettings>();
            foreach (var districtEditor in _prefabSelectors)
            {
                settings.Add(districtEditor.Value.GetSettings());
            }

            return settings;
        }

        #endregion

        //SHOULD PROBABLY MOVE THIS TO A SEPERATE CLASS/SERVICE

        #region Saving/Loading

        /// <summary>
        /// Save the current settings as the specified file format
        /// </summary>
        public void SaveSettings()
        {
            //Create full filename path
            var filename = Application.dataPath + "/GenerationSettings/" + _saveFile + ExtensionByType(_exportSetting);

            //check if the file exists, if it does delete and recreate it
            var file = new FileInfo(filename);
            if (file.Exists)
            {
                file.Delete();
            }

            //Save file based on selected type
            switch (_exportSetting)
            {
                //case ExportSettingType.JSON:
                //SaveAsJson(filename);
                //break;
                case ExportSettingType.XML:
                    SaveAsXml(filename);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //let user know where the file is saved
            Debug.LogFormat("Saved file at {0}!", filename);
        }

        public void LoadSettings()
        {
            //Create full filename
            var filename = Application.dataPath + "/GenerationSettings/" + _saveFile + ExtensionByType(_exportSetting);

            //check if the file exists, if it does delete and recreate it
            var file = new FileInfo(filename);
            if (!file.Exists)
            {
                Debug.LogFormat("File not found: {0}!", filename);
                return;
            }

            //choose file type to load
            switch (_exportSetting)
            {
                //case ExportSettingType.JSON:
                //    LoadFromJson(filename);
                //    break;
                case ExportSettingType.XML:
                    LoadFromXml(filename);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //XML
        private void SaveAsXml(string filename)
        {
            //create doc file
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

            var rootElem = new XElement("Settings");

            //save all relevant data
            //Generation Settings
            var genElem = new XElement("Generation");

            genElem.Add(new XElement("UseSeed", _generationSettings.UseSeed));
            genElem.Add(new XElement("Seed", _generationSettings.Seed));
            genElem.Add(new XElement("Width", _generationSettings.Width));
            genElem.Add(new XElement("Length", _generationSettings.Length));
            genElem.Add(new XElement("StartX", _generationSettings.StartX));
            genElem.Add(new XElement("StartY", _generationSettings.StartY));
            genElem.Add(new XElement("Amount", _generationSettings.Amount));
            genElem.Add(new XElement("Point", (int)_generationSettings.PointAlgorithm));
            genElem.Add(new XElement("Voronoi", (int)_generationSettings.VoronoiAlgorithm));
            genElem.Add(new XElement("Parent",
                (_townGenerator.Parent != null) ? _townGenerator.Parent.name : string.Empty));

            //Terrain
            var terrainElem = new XElement("Terrain");
            var splatmapsElem = new XElement("Splatmaps");

            int a = 0;
            foreach (var splatmap in _terrainEditor.GetSettings().SplatMaps)
            {
                var path = AssetDatabase.GetAssetPath(splatmap.Texture);
                splatmapsElem.Add(new XElement("Splatmap", path, new XAttribute("ID", a),
                    new XAttribute("Tiling", splatmap.TileSize)));
                a++;

            }

            var treeElem = new XElement("Trees");
            foreach (var treePrefab in _terrainEditor.GetSettings().Trees)
            {
                var path = AssetDatabase.GetAssetPath(treePrefab);
                treeElem.Add(new XElement("Tree", path));
            }

            var propElem = new XElement("Props");
            foreach (var propPrefab in _terrainEditor.GetSettings().Props)
            {
                var path = AssetDatabase.GetAssetPath(propPrefab);
                propElem.Add(new XElement("Prop", path));

            }

            var grassElem = new XElement("Details");
            foreach (var detail in _terrainEditor.GetSettings().Details)
            {
                var path = AssetDatabase.GetAssetPath(detail.Detail);
                grassElem.Add(new XElement("detail", path, new XAttribute("Type", (int)detail.Type)));

            }

            var lakeElem = new XElement("Lakes", new XAttribute("generate", _terrainEditor.Settings.GenerateLake));
            var p = AssetDatabase.GetAssetPath(_terrainEditor.Settings.WaterPrefab);
            lakeElem.Add(new XElement("lake", p));


            terrainElem.Add(splatmapsElem);
            terrainElem.Add(treeElem);
            terrainElem.Add(propElem);
            terrainElem.Add(grassElem);
            terrainElem.Add(lakeElem);

            //Prefab settings
            var prefabElem = new XElement("Districts");
            var prefabList = MakePrefabList();

            foreach (var districtSetting in MakeDistrictSettings())
            {
                var districtElement = new XElement(districtSetting.Type);

                districtElement.Add(new XElement("Frequency", districtSetting.Frequency));
                districtElement.Add(new XElement("Size", districtSetting.Size));
                districtElement.Add(new XElement("Offset", districtSetting.Offset));
                districtElement.Add(new XElement("Interval", districtSetting.Percentage));

                //prefabs
                var buildings = new XElement("Buildings");

                int i = 0;
                foreach (var prefab in prefabList[districtSetting.Type])
                {
                    var path = AssetDatabase.GetAssetPath(prefab);
                    buildings.Add(new XElement("Building", path, new XAttribute("ID", i)));

                    i++;
                }


                districtElement.Add(buildings);
                prefabElem.Add(districtElement);
            }


            //Add all elements to the root
            rootElem.Add(genElem);
            rootElem.Add(terrainElem);
            rootElem.Add(prefabElem);

            doc.Add(rootElem);

            doc.Save(filename);
        }

        private void LoadFromXml(string filename)
        {
            //Generation settings
            _generationSettings = new GenerationSettings();
            var root = XElement.Load(filename);

            //locate generation node
            var generation = root.Element("Generation");
            var district = root.Element("Districts");
            var terrain = root.Element("Terrain");

            if (generation == null)
                return;

            _generationSettings.UseSeed = bool.Parse(generation.Element("UseSeed").Value);
            _generationSettings.Seed = int.Parse(generation.Element("Seed").Value);

            _generationSettings.Width = double.Parse(generation.Element("Width").Value);
            _generationSettings.Length = double.Parse(generation.Element("Length").Value);

            _generationSettings.StartX = double.Parse(generation.Element("StartX").Value);
            _generationSettings.StartY = double.Parse(generation.Element("StartY").Value);

            _generationSettings.Amount = int.Parse(generation.Element("Amount").Value);

            _generationSettings.PointAlgorithm =
                (PointGenerationAlgorithm)
                    Enum.Parse(typeof(PointGenerationAlgorithm), generation.Element("Point").Value);
            _generationSettings.VoronoiAlgorithm =
                (VoronoiAlgorithm)Enum.Parse(typeof(VoronoiAlgorithm), generation.Element("Voronoi").Value);

            var parentname = generation.Element("Parent").Value;
            _townGenerator.Parent = null;
            if (parentname != string.Empty)
            {
                _townGenerator.Parent = GameObject.Find(parentname);
            }


            //Load in terrain settings
            var splatmaps = new List<SplatTexture>();
            foreach (var e in terrain.Element("Splatmaps").Elements())
            {
                var newSplat = new SplatTexture
                {
                    Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(e.Value),
                    TileSize = float.Parse(e.Attribute("Tiling").Value)
                };
                splatmaps.Add(newSplat);
            }

            var trees = new List<GameObject>();
            foreach (var e in terrain.Elements("Trees").Elements())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(e.Value);
                trees.Add(prefab);
            }

            var props = new List<GameObject>();
            foreach (var e in terrain.Elements("Props").Elements())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(e.Value);
                props.Add(prefab);
            }

            var details = new List<DetailObject>();
            foreach (var e in terrain.Elements("Details").Elements())
            {
                var d = new DetailObject();

                var type = (DetailType)int.Parse(e.Attribute("Type").Value);

                switch (type)
                {
                    case DetailType.Texture:
                        d.Detail = AssetDatabase.LoadAssetAtPath<Texture2D>(e.Value);
                        d.Type = DetailType.Texture;
                        break;
                    case DetailType.GameObject:
                        d.Detail = AssetDatabase.LoadAssetAtPath<GameObject>(e.Value);
                        d.Type = DetailType.GameObject;
                        break;
                }

                details.Add(d);
            }

            //lakes
            _terrainEditor.Settings.GenerateLake = bool.Parse(terrain.Element("Lakes").Attribute("generate").Value);
            _terrainEditor.Settings.WaterPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(terrain.Element("Lakes").Element("lake").Value);

            _terrainEditor.GetSettings().SplatMaps = splatmaps;
            _terrainEditor.GetSettings().Trees = trees;
            _terrainEditor.GetSettings().Props = props;
            _terrainEditor.GetSettings().Details = details;


            //Clear previous loaded districts
            _prefabSelectors.Clear();
            foreach (var districtElem in district.Elements())
            {
                //Create district
                var distictEditor = CreatePrefabSelection(districtElem.Name.ToString());

                if (distictEditor != null)
                {
                    distictEditor.GetSettings().Frequency = int.Parse(districtElem.Element("Frequency").Value);
                    distictEditor.GetSettings().Size = double.Parse(districtElem.Element("Size").Value);
                    distictEditor.GetSettings().Offset = int.Parse(districtElem.Element("Offset").Value);
                    distictEditor.GetSettings().Percentage = double.Parse(districtElem.Element("Interval").Value);

                    //Load in all buildings
                    distictEditor.ResetPrefabs();
                    foreach (var bElem in districtElem.Element("Buildings").Elements())
                    {
                        distictEditor.AddPrefab(bElem.Value);
                    }
                }
            }
        }

        //Helpers
        private string ExtensionByType(ExportSettingType type)
        {
            string ext = "";
            switch (_exportSetting)
            {
                //case ExportSettingType.JSON:
                //    ext = ".json";
                //    break;
                case ExportSettingType.XML:
                    ext = ".xml";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return ext;
        }

        #endregion
    }
}
