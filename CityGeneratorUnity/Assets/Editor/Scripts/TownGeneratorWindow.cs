using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using Points;
using UnityEngine;
using UnityEditor;
using Voronoi;


public class TownGeneratorWindow : EditorWindow
{
    private TownGenerator _townGenerator;
    private GenerationSettings _generationSettings;
    private CitySettings _citySettings;

    //zone prefab selector
    private Dictionary<string, DistrictEditor> _prefabSelectors = new Dictionary<string, DistrictEditor>();
    private string _zoneInput = "";

    //Foldouts
    private bool _foldoutSettings = true;
    private bool _foldoutZoneEdit = false;
    private GUIStyle _foldoutStyle;

    //Add menu to the window menu
    [MenuItem("TownGenerator Tool/TownGenerator")]
    private static void Init()
    {
        //get exisiting open window or create one if none is available
        TownGeneratorWindow window = (TownGeneratorWindow) EditorWindow.GetWindow(typeof (TownGeneratorWindow));
        window.Initialize();
        window.Show();
    }

    public void Initialize()
    { 
        //create a town generator object when it doesn't exist yet
        if (_townGenerator == null)
        {
            _townGenerator = TownGenerator.GetInstance();
            _generationSettings = new GenerationSettings();
            _citySettings = new CitySettings();
            
            _generationSettings.VoronoiAlgorithm = VoronoiAlgorithm.Fortune;
            _generationSettings.PointAlgorithm = PointGenerationAlgorithm.CityLike;;

            //base types
            CreatePrefabSelection("Grass");
            CreatePrefabSelection("Urban");
            CreatePrefabSelection("Factory");

            //foldout style
            _foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                //fontSize = 10,
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

    }

    /// Drawn to the window
    void OnGUI()
    {
        //Create TownGenerator if there isn't one
        Initialize();

        //Draw Generation settings
        GenerationSettings();

        ZonePrefabSelection();

         //Draw Buttons
         GenerationButtons();

    }

    private void GenerationSettings()
    {
        //GUILayout.Label("Generation Settings", EditorStyles.boldLabel);
        _foldoutSettings = EditorGUILayout.Foldout(_foldoutSettings, "Generation Settings", _foldoutStyle);

        if (!_foldoutSettings)
        {
            return;
        }


        //Seed
        GUILayout.Label("Seed", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        _generationSettings.UseSeed = EditorGUILayout.Toggle("Use Seed", _generationSettings.UseSeed);
        if (_generationSettings.UseSeed)
        {
            _generationSettings.Seed = EditorGUILayout.IntField("Seed", _generationSettings.Seed);
            
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Generation bounds", EditorStyles.boldLabel);
        //Width and Height
        _generationSettings.Width = EditorGUILayout.IntField("Width",(int)_generationSettings.Width);
        _generationSettings.Length = EditorGUILayout.IntField("Length",(int)_generationSettings.Length);

        //parent object to spawn the town on
        _townGenerator.Parent = (GameObject)EditorGUILayout.ObjectField("Parent", _townGenerator.Parent, typeof (GameObject), true);

        //Algorithm used
        _generationSettings.PointAlgorithm = (PointGenerationAlgorithm)EditorGUILayout.EnumPopup("Point Method", _generationSettings.PointAlgorithm);
        _generationSettings.VoronoiAlgorithm = (VoronoiAlgorithm)EditorGUILayout.EnumPopup("Voronoi Method", _generationSettings.VoronoiAlgorithm);

        GUILayout.Space(25);

    }

    private void GenerationButtons()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate"))
        {
            _townGenerator.Generate(_generationSettings);
            _townGenerator.PrefabsPerZone = MakePrefabList();
        }

        if (GUILayout.Button("Build"))
        {
            //Store district information
            _citySettings.DistrictSettings = MakeDistrictSettings();


            _townGenerator.Build(_citySettings);
        }

        if (GUILayout.Button("Clear"))
        {
            _townGenerator.Clear();
        }

        GUILayout.EndHorizontal();
        
    }

    //select prefabs for a specific zone
    private void ZonePrefabSelection()
    {
        //GUILayout.Label("City Settings", EditorStyles.boldLabel);
        _foldoutZoneEdit = EditorGUILayout.Foldout(_foldoutZoneEdit, "District Settings", _foldoutStyle);

        if (!_foldoutZoneEdit)
        {
            return;
        }


        
        GUILayout.Label("Add District", EditorStyles.boldLabel);
        //allow user to add custom zone types
        AddOrRemoveZoneType();

        if (_prefabSelectors.Count > 0)
        {
            GUILayout.Label("Edit Districts", EditorStyles.boldLabel);
            //manage selecting of the prefab game objects the user wants to spawn in a specific zone type
            foreach (var prefabSelect in _prefabSelectors.Values)
            {
                //draw districtEditor
                prefabSelect.DrawGUI(_foldoutStyle);
                GUILayout.Space(10);
            }
        }


        GUILayout.Space(25);
    }

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

    private void CreatePrefabSelection(string type)
    {
        if (_prefabSelectors.ContainsKey(type))
        {
            Debug.LogWarningFormat("TownGenerator: Zonetype {0} already exists. \nFailed to add type.", type);
            return;
        }

        _prefabSelectors.Add(type,new DistrictEditor(type));
        
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
}
