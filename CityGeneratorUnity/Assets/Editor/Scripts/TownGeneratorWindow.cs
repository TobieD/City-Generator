using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using UnityEngine;
using UnityEditor;
using Voronoi;


public class TownGeneratorWindow : EditorWindow
{
    private TownGenerator _townGenerator;
    private TownGenerationSettings _settings;

    //zone prefab selector
    private Dictionary<string, ZoneTypePrefabSelector> _prefabSelectors = new Dictionary<string, ZoneTypePrefabSelector>();
    private string _zoneInput = "";

    //Foldouts
    private bool _foldoutSettings = true;
    private bool _foldoutZoneEdit = false;

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
            _settings = new TownGenerationSettings();

            //base types
            CreatePrefabSelection(ZoneType.Factory.ToString());
            CreatePrefabSelection(ZoneType.Farm.ToString());
            CreatePrefabSelection(ZoneType.Urban.ToString());

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
        GUILayout.Label("Settings that adjust the generation of the city", EditorStyles.boldLabel);
        _foldoutSettings = EditorGUILayout.Foldout(_foldoutSettings, "Generation Settings");

        if (!_foldoutSettings)
        {
            return;
        }


        //Seed
        GUILayout.Label("Use a seed for generation", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        _settings.UseSeed = EditorGUILayout.Toggle("Use Seed", _settings.UseSeed);
        if (_settings.UseSeed)
        {
            _settings.Seed = EditorGUILayout.IntField("Seed", _settings.Seed);
            
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Adjust generation bounds", EditorStyles.boldLabel);
        //Width and Height
        _settings.Width = EditorGUILayout.IntField("Width",_settings.Width);
        _settings.Height = EditorGUILayout.IntField("Height",_settings.Height);

        //parent object to spawn the town on
        _settings.Parent = (GameObject)EditorGUILayout.ObjectField("Parent", _settings.Parent, typeof (GameObject), true);

        //Algorithm used
        _settings.Algorithm = (VoronoiAlgorithm)EditorGUILayout.EnumPopup("Algorithm", _settings.Algorithm);

        GUILayout.Space(25);

    }

    private void GenerationButtons()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate"))
        {
            _townGenerator.Generate(_settings);
            _townGenerator.PrefabsPerZone = MakePrefabList();
        }

        if (GUILayout.Button("Build"))
        {
            _townGenerator.Build();
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
        GUILayout.Label("Settings that adjust the buildings spawned during generation", EditorStyles.boldLabel);
        _foldoutZoneEdit = EditorGUILayout.Foldout(_foldoutZoneEdit, "Zone Settings");

        if (!_foldoutZoneEdit)
        {
            return;
        }
        
        GUILayout.Label("Add or remove zone types", EditorStyles.boldLabel);
        //allow user to add custom zone types
        AddOrRemoveZoneType();

        GUILayout.Label("Select prefabs for zone types",EditorStyles.boldLabel);
        //manage selecting of the prefab game objects the user wants to spawn in a specific zone type
        foreach (var prefabSelect in _prefabSelectors.Values)
        {
            prefabSelect.DrawGUI();
            GUILayout.Space(10);
        }

        GUILayout.Space(25);
    }

    private void AddOrRemoveZoneType()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Add new zone");
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

        _prefabSelectors.Add(type,new ZoneTypePrefabSelector(type));
        
    }

    private Dictionary<string, List<GameObject>> MakePrefabList()
    {
        return _prefabSelectors.ToDictionary(pfS => pfS.Key, pfS => pfS.Value.GetActualPrefabs());
    }
}
