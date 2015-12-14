using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Assets.Editor.Scripts;
using Points;
using UnityEngine;
using UnityEditor;

using Voronoi;
using CityGenerator;

/// <summary>
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

    //Road
    private RoadEditor _roadEditor;

    //Foldouts
    private bool _foldoutSettings = true;
    private bool _foldoutZoneEdit = false;
    private bool _foldoutRiverEdit = false;
    private GUIStyle _foldoutStyle;

    //Saving
    private ExportSettingType _exportSetting = ExportSettingType.XML;
    private string _saveFile = "Settings";

    //Add menu to the window menu
    [MenuItem("TownGenerator Tool/TownGenerator")]
    private static void Init()
    {
        //get exisiting open window or create one if none is available
        var window = (TownGeneratorWindow) EditorWindow.GetWindow(typeof (TownGeneratorWindow));
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

        //Create a base district type
        CreatePrefabSelection("Grass");

        //Terrain settings editor
        _terrainEditor = new TerrainEditor(new TerrainSettings());

        //Road settings editor
        _roadEditor = new RoadEditor(_citySettings.RoadSettings);

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

        //CitySettings.DebugMode = true;

        //Show import export UI
        ImportExportSettings();

        //Enable/Disable debug mode
        _citySettings.DebugMode = EditorGUILayout.Toggle("Debug Mode", _citySettings.DebugMode);//Draw Generation settings

        //generation settings
        GenerationSettings();

        //Terrain settings
        _terrainEditor.DrawGUI(_foldoutStyle);

        //District Settings
        ZonePrefabSelection();

        //River Settings
        //RoadRiverSettings();

        //Draw Buttons
        GenerationButtons();

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
        GUILayout.BeginHorizontal();
        _generationSettings.UseSeed = EditorGUILayout.Toggle("Use Seed", _generationSettings.UseSeed);
        if (_generationSettings.UseSeed)
        {
            _generationSettings.Seed = EditorGUILayout.IntField("Seed", _generationSettings.Seed);
            
        }
        GUILayout.EndHorizontal();

        //PARENT
        _townGenerator.Parent = (GameObject)EditorGUILayout.ObjectField("Parent", _townGenerator.Parent, typeof(GameObject), true);

        //PRESETS
        _cityType = (CityType)EditorGUILayout.EnumPopup("Preset", _cityType);

        //ADVANCED SETTINGS
        _showAdvancedSettings = EditorGUILayout.Toggle("Advanced Settings", _showAdvancedSettings);
        if (_showAdvancedSettings)
        {
            //GUILayout.Label("Generation bounds", EditorStyles.boldLabel);
            _roadEditor.Settings.GenerateInnerRoads = EditorGUILayout.Toggle("Generate Inner Roads",
                _roadEditor.Settings.GenerateInnerRoads);
            //Width and Height
            _generationSettings.Width = EditorGUILayout.IntField("Width", (int) _generationSettings.Width);
            _generationSettings.Length = EditorGUILayout.IntField("Length", (int) _generationSettings.Length);

            _generationSettings.Amount = EditorGUILayout.IntField("Amount of Points", (int) _generationSettings.Amount);

            //Algorithm used
            _generationSettings.PointAlgorithm =
                (PointGenerationAlgorithm) EditorGUILayout.EnumPopup("Point Method", _generationSettings.PointAlgorithm);
            _generationSettings.VoronoiAlgorithm =
                (VoronoiAlgorithm) EditorGUILayout.EnumPopup("Voronoi Method", _generationSettings.VoronoiAlgorithm);

        }
        //GUILayout.Space(25);
        EditorGUI.indentLevel--;

    }
    
    /// <summary>
    /// select prefabs for a specific zone
    /// </summary>
    private void ZonePrefabSelection()
    {
        //GUILayout.Label("City Settings", EditorStyles.boldLabel);
        _foldoutZoneEdit = EditorGUILayout.Foldout(_foldoutZoneEdit, "District Settings", _foldoutStyle);

        if (!_foldoutZoneEdit)
        {
            
            return;
        }


        
        //allow user to add custom zone types
        AddOrRemoveZoneType();

       

        if (_prefabSelectors.Count > 0)
        {
            //manage selecting of the prefab game objects the user wants to spawn in a specific zone type
            foreach (var prefabSelect in _prefabSelectors.Values)
            {
                EditorGUI.indentLevel++;
                //draw districtEditor
                prefabSelect.DrawGUI(_foldoutStyle);
                //GUILayout.Space(10);
                EditorGUI.indentLevel--;
            }
        }

    }

    /// <summary>
    /// Add or delete a new district type
    /// </summary>
    private void AddOrRemoveZoneType()
    {
        EditorGUI.indentLevel++;
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

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Edit road settings
    /// </summary>
    private void RoadRiverSettings()
    {
        _foldoutRiverEdit = EditorGUILayout.Foldout(_foldoutRiverEdit, "Road Settings", _foldoutStyle);

        if (!_foldoutRiverEdit)
        {
            return;
        }

        EditorGUI.indentLevel++;
        _roadEditor.DrawGUI(_foldoutStyle);
        EditorGUI.indentLevel--;

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
            _citySettings.RoadSettings = _roadEditor.Settings;

            _townGenerator.Generate(_generationSettings, _citySettings, _terrainEditor.GetSettings());
            _townGenerator.PrefabsPerZone = MakePrefabList();
            _townGenerator.RoadPrefabs = _roadEditor.RoadPrefabs;
        }

        //Using the voronoi data, create a city and build it
        if (GUILayout.Button("Build"))
        {
            _townGenerator.Build();
        }

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

        var editor = new DistrictEditor(type);

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
                break;
            }

                //medium
            case CityType.Town:
            {
                genSettings.Width = 10000;
                genSettings.Length = 10000;
                genSettings.PointAlgorithm = PointGenerationAlgorithm.CityLike;
                genSettings.Amount = 8000;
                break;
            }

            //large
            case CityType.Metropolis:
            {
                genSettings.Width = 20000;
                genSettings.Length = 20000;
                genSettings.PointAlgorithm = PointGenerationAlgorithm.CityLike;
                genSettings.Amount = 16000;
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
        genElem.Add(new XElement("Point", (int) _generationSettings.PointAlgorithm));
        genElem.Add(new XElement("Voronoi", (int) _generationSettings.VoronoiAlgorithm));
        genElem.Add(new XElement("Parent", (_townGenerator.Parent != null) ? _townGenerator.Parent.name : string.Empty));

        //Terrain
        var terrainElem = new XElement("Terrain");
        var splatmapsElem = new XElement("Splatmaps");

        terrainElem.Add(new XElement("RoadTexture",AssetDatabase.GetAssetPath(_terrainEditor._settings.RoadTexture.Texture),new XAttribute("Tiling",_terrainEditor.GetSettings().RoadTexture.TileSize)));

        int a = 0; 
        foreach (var splatmap in _terrainEditor.GetSettings().SplatMaps)
        {
            var path = AssetDatabase.GetAssetPath(splatmap.Texture);
            splatmapsElem.Add(new XElement("Splatmap", path, new XAttribute("ID", a), new XAttribute("Tiling", splatmap.TileSize)));
            a++;
            
        }

        var treeElem = new XElement("Trees");
        foreach (var treePrefab in _terrainEditor.GetSettings().Trees)
        {
            var path = AssetDatabase.GetAssetPath(treePrefab);
            treeElem.Add(new XElement("Tree",path));

        }

        terrainElem.Add(splatmapsElem);
        terrainElem.Add(treeElem);

        //Road Settings
        var roadElem = new XElement("Road");
        roadElem.Add(new XElement("Amount", _roadEditor.Settings.Amount));
        roadElem.Add(new XElement("Max", _roadEditor.Settings.Max));
        roadElem.Add(new XElement("Branches", _roadEditor.Settings.Branches));

        var straightPath = AssetDatabase.GetAssetPath(_roadEditor.RoadPrefabs.Straight);

        roadElem.Add(new XElement("Road_Straight", straightPath));

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
        rootElem.Add(roadElem);

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
        var road = root.Element("Road");
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

        _generationSettings.PointAlgorithm = (PointGenerationAlgorithm) Enum.Parse(typeof (PointGenerationAlgorithm), generation.Element("Point").Value);
        _generationSettings.VoronoiAlgorithm = (VoronoiAlgorithm) Enum.Parse(typeof (VoronoiAlgorithm), generation.Element("Voronoi").Value);

        var parentname = generation.Element("Parent").Value;
        _townGenerator.Parent = null;
        if (parentname != string.Empty)
        {
            _townGenerator.Parent = GameObject.Find(parentname);
        }


        //Load in terrain settings
        var splatmaps= new List<SplatTexture>();
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

        var roadSplat = new SplatTexture();
        roadSplat.Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(terrain.Element("RoadTexture").Value);
        roadSplat.TileSize = float.Parse(terrain.Element("RoadTexture").Attribute("Tiling").Value);


        _terrainEditor.GetSettings().RoadTexture = roadSplat;
        _terrainEditor.GetSettings().SplatMaps = splatmaps;
        _terrainEditor.GetSettings().Trees = trees;
        
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


        _roadEditor.Settings.Amount = int.Parse(road.Element("Amount").Value);
        _roadEditor.Settings.Branches = int.Parse(road.Element("Branches").Value);
        _roadEditor.Settings.Max = int.Parse(road.Element("Max").Value);
        if (_roadEditor.RoadPrefabs == null)
        {
            _roadEditor.RoadPrefabs = new RoadPrefabs();
        }
        
        _roadEditor.RoadPrefabs.Straight = AssetDatabase.LoadAssetAtPath<GameObject>(road.Element("Road_Straight").Value);

    }

    //JSON
    private void SaveAsJson(string filename)
    {
    }

    private void LoadFromJson(string filename)
    {
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
